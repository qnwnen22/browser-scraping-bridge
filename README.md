# Browser HTML Bridge

사이트에 HTML을 요청할 때 Chrome/Edge 브라우저의 세션을 사용하는 테스팅 도구입니다. 로컬 백엔드가 SignalR로 브라우저 확장에 URL을 전달하고, 확장이 페이지를 열어 가져온 HTML을 반환합니다. 요청 대상 사이트와의 통신은 브라우저가 수행합니다.

HTTP/HTTPS URL을 지원하며 특정 사이트로 제한하지 않습니다.

## 구성

- `BrowserScrapingBridge.Server`: ASP.NET Core 10 로컬 서버, SignalR 허브, HTML 요청 API 및 테스트 화면
- `extension`: Chrome/Edge 확장 프론트. 현재 브라우저 프로필에서 비활성 탭을 열고 HTML을 반환합니다.

```text
테스트 화면 / API 호출자 → 백엔드 → SignalR → 브라우저 확장
테스트 화면 / API 호출자 ← 백엔드 ← SignalR ← 브라우저가 반환한 HTML
```

## 백엔드 코드 구조

Controller 기반 ASP.NET Core API로 구성합니다.

- `Controllers/BrowserController.cs`: `GET /api/status`, `POST /api/fetch` 엔드포인트. `IActionResult` 및 `Task<IActionResult>`로 HTTP 응답을 반환합니다.
- `Models/FetchPageRequest.cs`: URL, 추가 대기 시간, 선택적인 요청 헤더와 쿠키
- `Services/FetchPageRequestValidator.cs`: 헤더·쿠키 형식 및 중복 지정 검사
- `Models/BrowserFetchResult.cs`: 브라우저가 반환한 HTML 및 응답 정보
- `Services/BrowserRequestCoordinator.cs`: 브라우저 연결, SignalR 요청 전달, 결과 대기와 제한 시간 관리
- `Hubs/BrowserHub.cs`: 확장 프로그램의 연결 등록과 수집 결과 수신
- `Program.cs`: 서비스 등록, Controller 및 SignalR Hub 연결, 서버 실행

Controller 전환 후에도 요청 URL과 JSON 응답 형식은 같습니다. 잘못된 요청은 400, 브라우저 연결 오류는 503, 응답 제한 시간 초과는 504로 반환합니다. `[ApiController]`가 JSON 요청 바인딩 오류도 400으로 처리합니다.

## 실행

프로젝트 폴더 `C:\Users\User\source\repos\browser-scraping-bridge`에서 서버를 실행합니다.

```powershell
dotnet run --project .\BrowserScrapingBridge.Server
```

Chrome의 `chrome://extensions` 또는 Edge의 `edge://extensions`에서 개발자 모드를 켜고 `extension` 폴더를 압축 해제된 확장 프로그램으로 로드합니다. 확장 수정 후에는 다시 로드해야 합니다. CRX 파일로 설치했다면 수정된 소스로 다시 패키징하거나 해당 폴더를 로드해야 합니다.

`http://127.0.0.1:5180`에 접속해 브라우저 연결 상태를 확인하고 URL을 입력합니다. 필요하면 '헤더·쿠키 지정'을 펼쳐 JSON 객체로 입력합니다. 결과에서 제목, 최종 URL, HTTP 상태, HTML 크기와 HTML 내용을 확인하거나 HTML 파일로 내려받을 수 있습니다.

## 반환하는 HTML

브라우저는 요청마다 새 비활성 탭을 열고 페이지 로딩과 지정한 추가 대기 후 탭의 DOM HTML을 읽습니다. 완료 후 해당 탭을 닫습니다. 추가 대기는 기본 900ms이며 0~10000ms로 조정할 수 있습니다.

JavaScript가 실행된 이후의 DOM이므로 서버가 처음 보낸 HTML 원문과 다를 수 있습니다. 쿠키를 별도로 지정하지 않으면 확장을 설치한 브라우저 프로필의 쿠키를 사용합니다. 로그인 화면, CAPTCHA, HTTP 오류 페이지가 표시되면 그 페이지가 결과로 반환될 수 있습니다.

브라우저 내부 주소(`chrome://`, `edge://`)와 로컬 파일 주소는 지원하지 않습니다. HTTP 상태를 관측할 수 없는 경우 `httpStatusCode`는 `null`입니다.

## 요청 API

```http
POST http://127.0.0.1:5180/api/fetch
Content-Type: application/json

{
  "url": "https://example.com/",
  "renderWaitMilliseconds": 900,
  "headers": {
    "Accept-Language": "ko-KR,ko;q=0.9",
    "X-Request-Source": "browser-bridge"
  },
  "cookies": {
    "sessionId": "example-value",
    "theme": "dark"
  }
}
```

요청 필드:

| 필드 | 형식 | 동작 |
| --- | --- | --- |
| `url` | 문자열, 필수 | 열려는 HTTP/HTTPS 페이지 |
| `renderWaitMilliseconds` | 정수, 선택 | 로딩 완료 후 추가 대기. 기본 900ms, 0~10000ms |
| `headers` | 이름·문자열 값 JSON 객체, 선택 | 지정한 헤더만 해당 요청에서 교체. 생략한 헤더는 브라우저 기본값 사용 |
| `cookies` | 이름·문자열 값 JSON 객체, 선택 | 해당 요청의 Cookie 헤더 전체를 지정한 쿠키로 교체 |

URL과 대기 시간만 보내던 기존 요청도 그대로 사용할 수 있습니다.

### 헤더·쿠키 적용 범위

- 통신 방식은 브라우저 탭의 **GET 페이지 이동**입니다. HTTP 메서드와 요청 본문은 받지 않습니다.
- 지정한 헤더·쿠키는 **요청용 탭에서 입력 URL의 메인 문서 요청**에만 적용합니다. URL의 `#fragment`는 HTTP 요청에 포함되지 않습니다.
- 다른 URL로의 리디렉션, 이미지·스크립트·iframe 및 페이지 내부 `fetch`/XHR에는 지정한 값이 적용되지 않습니다. 이 요청들은 브라우저의 기본 헤더와 저장된 쿠키를 사용합니다. 사용자 지정 쿠키만으로 페이지 내부 API까지 로그인 상태를 유지하려면 별도의 세션 관리가 필요합니다.
- `cookies`를 생략하거나 `null`로 보내면 브라우저의 기존 쿠키를 사용합니다. `{}`로 보내면 메인 문서 요청에서 Cookie 헤더를 제거합니다.
- `cookies`를 지정하면 기존 쿠키와 자동으로 합치지 않습니다. 필요한 쿠키를 모두 전달해야 합니다. 위 예시는 `Cookie: sessionId=example-value; theme=dark`로 전송됩니다.
- 입력한 쿠키는 브라우저 쿠키 저장소에 등록하지 않습니다. 대상 사이트 응답의 `Set-Cookie`와 페이지 스크립트에 의한 쿠키 변경은 일반 브라우저 동작대로 처리됩니다.
- `headers`에 `Cookie` 문자열을 직접 지정할 수도 있습니다. 이 경우 `cookies`를 함께 보내면 400을 반환합니다. 헤더 이름은 대소문자를 구분하지 않고, 쿠키 이름은 구분합니다.
- 헤더 값은 ASCII 문자열이어야 하며 줄바꿈을 허용하지 않습니다. 쿠키 값은 HTTP 쿠키 형식을 따라야 합니다. 값을 자동으로 인코딩하지 않습니다. 이름·값의 형식 오류, 대소문자만 다른 헤더 중복, Cookie 이중 지정은 400을 반환합니다.
- 브라우저 규칙을 설치할 수 없으면 HTML 요청을 진행하지 않고 결과의 `error`로 실패를 알립니다. 사이트 서비스 워커가 캐시에서 응답해 네트워크 요청이 발생하지 않는 경우에는 요청 헤더 변경으로 서버와 통신할 수 없습니다.

확장은 임시 `declarativeNetRequest` 세션 규칙을 페이지 이동 전에 등록하고 로딩 완료·실패 시 제거합니다. 서비스 워커가 다시 시작되면 남아 있던 규칙도 정리합니다. 브라우저 전체 설정이나 다른 탭의 요청 헤더를 변경하지 않습니다.

응답 필드:

- `requestId`: 요청 식별자
- `url`, `finalUrl`: 요청 URL 및 실제 페이지 URL
- `title`: 페이지 제목
- `httpStatusCode`: 관측된 메인 문서 응답 상태
- `html`: 브라우저가 반환한 DOM HTML
- `error`: 브라우저 작업 실패 원인. 성공하면 `null`

브라우저 작업 결과를 받은 경우 API는 200을 반환합니다. 대상 사이트의 응답 상태는 `httpStatusCode`를 확인합니다. 확장이 연결되지 않았거나 요청 중 연결이 끊어지면 503, 60초 이내에 결과가 없으면 504를 반환합니다.

여러 요청은 순서대로 처리합니다. 반환 메시지 크기는 15MiB로 제한합니다. 서버는 `127.0.0.1:5180`에서만 수신합니다.

## 확장 권한

HTTP/HTTPS 사이트의 탭 생성, HTML 읽기, 요청 탭의 메인 문서 응답 상태 확인 권한을 사용합니다. 요청 헤더 적용을 위해 `declarativeNetRequestWithHostAccess` 권한을 사용하며, 별도의 쿠키 저장소 읽기·쓰기 권한은 사용하지 않습니다. Chrome 116 이상에서 WebSocket 메시지를 주기적으로 교환해 SignalR 연결을 유지합니다.

확장 버전은 1.2.0입니다. 변경 사항을 사용하려면 서버를 다시 실행하고 확장을 다시 로드해야 합니다. CRX로 설치했다면 새 소스로 다시 패키징해야 합니다.

참고: [Chrome URL 권한 패턴](https://developer.chrome.com/docs/extensions/develop/concepts/match-patterns), [확장 서비스 워커 WebSocket 연결](https://developer.chrome.com/docs/extensions/how-to/web-platform/websockets), [요청 헤더 변경 및 세션 규칙](https://developer.chrome.com/docs/extensions/reference/api/declarativeNetRequest)


