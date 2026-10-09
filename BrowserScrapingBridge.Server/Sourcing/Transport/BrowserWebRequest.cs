using System.Net;
using System.Text;

namespace BrowserScrapingBridge.Server.Sourcing.Transport;

// 옮겨온 WebRequest 호출도 서버에서 대상 사이트로 직접 접속하지 않습니다.
public sealed class BrowserWebRequest
{
    private readonly Uri _uri;
    private readonly MemoryStream _body = new();
    public string Method { get; set; } = "GET";
    public WebHeaderCollection Headers { get; } = new();
    public string? Accept { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
    public string? ContentType { get; set; }
    public int Timeout { get; set; } = 60000;
    public IWebProxy? Proxy { get; set; }
    public CookieContainer? CookieContainer { get; set; }
    public bool KeepAlive { get; set; } = true;

    private BrowserWebRequest(Uri uri) => _uri = uri;
    public static BrowserWebRequest CreateHttp(string url) => new(new Uri(url));
    public Stream GetRequestStream() => new BodyWriter(_body);
    public WebResponse GetResponse() => GetResponseAsync().GetAwaiter().GetResult();

    public async Task<WebResponse> GetResponseAsync()
    {
        using var client = CookieContainer is null ? new BrowserHttpClient() : new BrowserHttpClient(new HttpClientHandler { CookieContainer = CookieContainer });
        client.Timeout = TimeSpan.FromMilliseconds(Timeout);
        using var request = new HttpRequestMessage(new HttpMethod(Method), _uri);
        if (_body.Length > 0) request.Content = new ByteArrayContent(_body.ToArray());
        var headers = Headers.AllKeys.ToDictionary(key => key, key => Headers[key]!, StringComparer.OrdinalIgnoreCase);
        if (Accept is not null) headers["Accept"] = Accept;
        if (UserAgent is not null) headers["User-Agent"] = UserAgent;
        if (Referer is not null) headers["Referer"] = Referer;
        if (ContentType is not null)
        {
            request.Content ??= new ByteArrayContent([]);
            headers["Content-Type"] = ContentType;
        }
        foreach (var header in headers)
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value)) request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
        using var response = await client.SendAsync(request, SourcingExecution.Current.CancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var result = new BrowserWebResponse(bytes, response.StatusCode, _uri);
        if (!response.IsSuccessStatusCode) throw new WebException($"대상 사이트 응답: HTTP {(int)response.StatusCode}", null, WebExceptionStatus.ProtocolError, result);
        return result;
    }

    private sealed class BodyWriter(MemoryStream stream) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => stream.Length;
        public override long Position { get => stream.Position; set => throw new NotSupportedException(); }
        public override void Flush() => stream.Flush();
        public override void Write(byte[] buffer, int offset, int count) => stream.Write(buffer, offset, count);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => stream.SetLength(value);
    }
}

internal sealed class BrowserWebResponse(byte[] body, HttpStatusCode status, Uri uri) : WebResponse
{
    public HttpStatusCode StatusCode { get; } = status;
    public override Uri ResponseUri => uri;
    public override long ContentLength { get => body.Length; set => throw new NotSupportedException(); }
    public override Stream GetResponseStream() => new MemoryStream(body, writable: false);
}
