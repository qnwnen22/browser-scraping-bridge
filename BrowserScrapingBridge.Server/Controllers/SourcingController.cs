using System.Text.Json;
using BrowserScrapingBridge.Server.Models;
using BrowserScrapingBridge.Server.Services;
using BrowserScrapingBridge.Server.Sourcing;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BrowserScrapingBridge.Server.Controllers;

[ApiController]
[Route("api/sourcing")]
public sealed class SourcingController(SiteCollectorCatalog catalog, BrowserChatHubService browser) : ControllerBase
{
    [HttpGet("sites")]
    public IActionResult Sites() => Ok(catalog.Sites);

    [HttpPost("product")]
    public Task<IActionResult> Product([FromBody] SourcingRequest request, CancellationToken cancellationToken)
        => Execute(request, "product", cancellationToken);

    [HttpPost("page")]
    public Task<IActionResult> Page([FromBody] SourcingRequest request, CancellationToken cancellationToken)
        => Execute(request, "page", cancellationToken);

    [HttpPost("raw")]
    public Task<IActionResult> Raw([FromBody] SourcingRequest request, CancellationToken cancellationToken)
        => Execute(request, "raw", cancellationToken);

    [HttpPost("parse")]
    public Task<IActionResult> Parse([FromBody] SourcingRequest request, CancellationToken cancellationToken)
        => Execute(request, "parse", cancellationToken);

    [HttpPost("login")]
    public Task<IActionResult> Login([FromBody] SourcingRequest request, CancellationToken cancellationToken)
        => Execute(request, "login", cancellationToken);

    private async Task<IActionResult> Execute(SourcingRequest request, string operation, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Site)) return BadRequest(new ApiErrorResponse("site를 지정해주세요."));
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return BadRequest(new ApiErrorResponse("HTTP/HTTPS URL을 지정해주세요."));
        if (request.Page < 1) return BadRequest(new ApiErrorResponse("page는 1 이상이어야 합니다."));
        if (request.RenderWaitMilliseconds is < 0 or > 10000) return BadRequest(new ApiErrorResponse("추가 대기는 0~10000ms 범위입니다."));
        if (request.TimeoutSeconds is < 10 or > 600) return BadRequest(new ApiErrorResponse("timeoutSeconds는 10~600 범위입니다."));
        var validation = FetchPageRequestValidator.ValidateOptions(new FetchPageRequest(uri.AbsoluteUri,
            request.RenderWaitMilliseconds, request.Headers, request.Cookies));
        if (validation is not null) return BadRequest(new ApiErrorResponse(validation));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));
        try
        {
            browser.Configure(uri, request, timeout.Token);
            var collector = catalog.Create(request.Site.Trim());
            collector.ChatHubService = browser;
            if (operation is "product" or "page" or "raw" or "login")
                browser.BindBrowser();
            if (operation == "parse" && !collector.SupportsCaching)
                return UnprocessableEntity(new ApiErrorResponse("이 파서는 저장된 원문으로 상품을 파싱하는 기능을 지원하지 않습니다."));
            if (operation == "parse" && string.IsNullOrWhiteSpace(request.Html) && request.Data is null)
                return BadRequest(new ApiErrorResponse("parse에는 html 또는 data가 필요합니다."));
            var options = request.CollectOptions is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) } json
                ? JsonConvert.DeserializeObject<CollectOptionSetting>(json.GetRawText()) : null;
            // 일부 기존 파서는 동기 함수이므로 요청 실행 범위와 취소 토큰을 함께 전달합니다.
            var result = await Task.Run(async () =>
            {
                using var context = new SourcingExecution(browser, request.ApiSettings, timeout.Token);
                var connectionId = operation == "parse" ? "" : browser.ConnectionId;
                try
                {
                    object result = operation switch
                    {
                        "product" => (object)await collector.GetProductAsync(connectionId, uri.AbsoluteUri, options),
                        "page" => await collector.GetCollectPageInfosAsync(connectionId, uri.AbsoluteUri, request.Page),
                        "raw" => await collector.GetCollectRawDataAsync(connectionId, uri.AbsoluteUri),
                        "parse" => collector.GetProduct(new CollectRawData
                        {
                            Url = uri.AbsoluteUri,
                            Html = request.Html ?? "",
                            Data = request.Data is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) } data
                                ? Newtonsoft.Json.Linq.JToken.Parse(data.GetRawText()) : null
                        }),
                        "login" => new { loginUrl = await collector.GetLoginUrlAsync(connectionId) },
                        _ => throw new InvalidOperationException()
                    };
                    context.ThrowIfFailed();
                    return result;
                }
                catch
                {
                    context.ThrowIfFailed();
                    throw;
                }
            }, timeout.Token).WaitAsync(timeout.Token);
            // 기존 JToken과 JsonProperty를 올바른 JSON으로 반환합니다.
            var serialized = JsonConvert.SerializeObject(result, new StringEnumConverter());
            using var document = JsonDocument.Parse(serialized);
            return Ok(document.RootElement.Clone());
        }
        catch (BrowserUnavailableException exception) { return Problem(detail: exception.Message, statusCode: 503); }
        catch (BrowserOperationException exception) { return Problem(detail: exception.Message, statusCode: 502); }
        catch (TimeoutException exception) { return Problem(detail: exception.Message, statusCode: 504); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return Problem(detail: "수집 제한 시간을 초과했습니다.", statusCode: 504); }
        catch (OperationCanceledException) { throw; }
        catch (CaptchaException exception)
        { return UnprocessableEntity(new { error = exception.CaptchaInfo?.Message ?? exception.Message, captcha = exception.CaptchaInfo }); }
        catch (NotImplementedException) { return UnprocessableEntity(new ApiErrorResponse("이 파서는 요청한 기능을 지원하지 않습니다.")); }
        catch (CollectException exception) { return UnprocessableEntity(new ApiErrorResponse(exception.Message)); }
        catch (ArgumentException exception) { return BadRequest(new ApiErrorResponse(exception.Message)); }
        catch (Newtonsoft.Json.JsonException) { return BadRequest(new ApiErrorResponse("수집 옵션 또는 원문 JSON 형식이 올바르지 않습니다.")); }
        catch (Exception)
        { return UnprocessableEntity(new ApiErrorResponse("사이트 수집에 실패했습니다. 로그인 상태와 페이지 구조를 확인해주세요.")); }
    }
}
