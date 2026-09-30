using BrowserScrapingBridge.Server.Models;
using BrowserScrapingBridge.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace BrowserScrapingBridge.Server.Controllers;

[ApiController]
[Route("api")]
public sealed class BrowserController(BrowserRequestCoordinator coordinator) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType(typeof(BrowserConnectionStatus), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
        => Ok(new BrowserConnectionStatus(coordinator.IsBrowserConnected));

    [HttpPost("fetch")]
    [ProducesResponseType(typeof(BrowserFetchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> Fetch([FromBody] FetchPageRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.Url?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new ApiErrorResponse("HTTP 또는 HTTPS URL을 입력해주세요."));
        }

        if (request.RenderWaitMilliseconds is < 0 or > 10000)
            return BadRequest(new ApiErrorResponse("추가 대기는 0~10000ms 범위로 입력해주세요."));

        var optionsError = FetchPageRequestValidator.ValidateOptions(request);
        if (optionsError is not null)
            return BadRequest(new ApiErrorResponse(optionsError));

        try
        {
            var result = await coordinator.FetchAsync(uri, request, cancellationToken);
            return Ok(result);
        }
        catch (BrowserUnavailableException exception)
        {
            return Problem(detail: exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (TimeoutException exception)
        {
            return Problem(detail: exception.Message, statusCode: StatusCodes.Status504GatewayTimeout);
        }
    }
}

