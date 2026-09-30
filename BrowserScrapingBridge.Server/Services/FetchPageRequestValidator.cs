using BrowserScrapingBridge.Server.Models;

namespace BrowserScrapingBridge.Server.Services;

public static class FetchPageRequestValidator
{
    public static string? ValidateOptions(FetchPageRequest request)
    {
        var headerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers ?? [])
        {
            if (!IsToken(header.Key))
                return "헤더 이름은 공백 없는 HTTP 헤더 이름 형식이어야 합니다.";
            if (!headerNames.Add(header.Key))
                return "같은 이름의 헤더를 대소문자만 다르게 중복 지정할 수 없습니다.";
            if (header.Value is null || header.Value.Any(c => c != '\t' && (c < ' ' || c > '~')))
                return "헤더 값은 줄바꿈 없는 ASCII 문자열이어야 합니다.";
        }

        if (request.Cookies is not null && headerNames.Contains("Cookie"))
            return "Headers의 Cookie 헤더와 Cookies를 동시에 지정할 수 없습니다.";

        foreach (var cookie in request.Cookies ?? [])
        {
            if (!IsToken(cookie.Key))
                return "쿠키 이름은 공백과 구분자가 없는 HTTP 토큰 형식이어야 합니다.";
            if (cookie.Value is null || !cookie.Value.All(IsCookieOctet))
                return "쿠키 값에는 공백, 따옴표, 쉼표, 세미콜론, 역슬래시 또는 비 ASCII 문자를 사용할 수 없습니다.";
        }

        return null;
    }

    private static bool IsToken(string name)
        => name.Length > 0 && name.All(c =>
            char.IsAsciiLetterOrDigit(c) || "!#$%&'*+-.^_`|~".Contains(c));

    // RFC 6265 cookie-octet: values are sent as supplied, without URL encoding.
    private static bool IsCookieOctet(char c)
        => c == '!' || c is >= '#' and <= '+' or >= '-' and <= ':' or >= '<' and <= '[' or >= ']' and <= '~';
}
