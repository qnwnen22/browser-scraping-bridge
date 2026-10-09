# Browser HTML Bridge

사이트에 HTML을 요청할 때 Chrome/Edge 브라우저의 세션을 사용하는 테스팅 도구입니다. 로컬 백엔드가 SignalR로 브라우저 확장에 URL을 전달하고, 확장이 페이지를 열어 가져온 HTML을 반환합니다. 요청 대상 사이트와의 통신은 브라우저가 수행합니다.

HTTP/HTTPS URL을 지원하며 특정 사이트로 제한하지 않습니다.

## 구성

- `BrowserScrapingBridge.Server`: ASP.NET Core 10 로컬 서버, SignalR 허브, HTML 요청 API, 사이트별 수집 API 및 테스트 화면
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

잘못된 요청은 400, 브라우저 연결 오류는 503, 응답 제한 시간 초과는 504로 반환합니다. `[ApiController]`가 JSON 요청 바인딩 오류도 400으로 처리합니다.

## 실행

프로젝트 폴더 `C:\Users\User\source\repos\browser-scraping-bridge`에서 서버를 실행합니다.

```powershell
dotnet run --project .\BrowserScrapingBridge.Server
```

Chrome의 `chrome://extensions` 또는 Edge의 `edge://extensions`에서 개발자 모드를 켜고 `extension` 폴더를 압축 해제된 확장 프로그램으로 로드합니다. 확장 수정 후에는 다시 로드해야 합니다. CRX 파일로 설치했다면 수정된 소스로 다시 패키징하거나 해당 폴더를 로드해야 합니다.

확장은 서버에 자동 연결됩니다. 확장 팝업과 테스트 화면에서 연결 상태를 확인하면 됩니다. 브라우저 선택이나 식별값 입력 없이 연결된 브라우저를 사용합니다.

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

URL만 보내도 사용할 수 있습니다. 대기 시간·헤더·쿠키는 필요할 때 지정합니다.

### 브라우저 자동 연결

확장은 인자 없는 SignalR `RegisterBrowser()`를 호출합니다. 서버가 `Context.ConnectionId`를 내부 메모리에 보관하고 요청할 브라우저를 자동으로 선택합니다. Redis나 DB는 사용하지 않습니다.

혼자 사용하는 브라우저 한 개를 기준으로 동작합니다. 여러 확장이 연결되면 마지막으로 등록된 연결을 사용합니다. 확장이 페이지를 열고 닫을 때 사용하는 실제 Chrome 탭 번호는 내부 처리에만 사용합니다.

수집 시작 때 자동 선택한 연결을 고정하여 HTML·쿠키·추가 API 요청에 사용합니다. 확장이 연결되어 있지 않거나 수집 중 연결이 끊기거나 변경되면 503입니다. 재접속한 브라우저는 새 수집 요청부터 자동으로 사용합니다.

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

HTTP/HTTPS 사이트의 탭 생성, HTML 읽기, 요청 탭의 메인 문서 응답 상태 확인 권한을 사용합니다. 요청 헤더 적용을 위해 `declarativeNetRequestWithHostAccess` 권한을 사용합니다. 사이트 파서의 로그인 확인·H5 서명을 위해 `cookies` 권한으로 요청된 사이트 도메인의 쿠키를 읽습니다. 쿠키를 임의로 브라우저 저장소에 등록하는 기능은 없습니다. Chrome 116 이상에서 WebSocket 메시지를 주기적으로 교환해 SignalR 연결을 유지합니다.

확장 버전은 1.4.2입니다. 변경 사항을 사용하려면 서버를 다시 실행하고 확장을 다시 로드해야 합니다. CRX로 설치했다면 새 소스로 다시 패키징해야 합니다.

참고: [Chrome URL 권한 패턴](https://developer.chrome.com/docs/extensions/develop/concepts/match-patterns), [확장 서비스 워커 WebSocket 연결](https://developer.chrome.com/docs/extensions/how-to/web-platform/websockets), [요청 헤더 변경 및 세션 규칙](https://developer.chrome.com/docs/extensions/reference/api/declarativeNetRequest)

## 사이트별 수집 기능

기존 `omarket_250317/Omarket_RestApi/JwtAuthDemo/Sourcing`의 사이트별 파싱 로직을 이 프로젝트의 `BrowserScrapingBridge.Server/Sourcing`에 이식했습니다. 브리지 안의 소스만 빌드하며, 기존 프로젝트·CS 파일·DLL을 참조하지 않습니다. 기존 소스 수정도 필요 없습니다.

- `Sourcing/<사이트>`: 사이트별 상품·목록 파서
- `Sourcing/Models`: 상품·옵션·수집 설정 모델. DB 저장 속성 제거
- `Sourcing/Parsing`: 수집에 필요한 HTML·JSON·문자열 도구
- `Sourcing/Transport`: 브라우저 통신, 수집 실행 범위, 기존 호출 계약 `IChatHubService`
- `Controllers/SourcingController.cs`: 사이트 목록, 상품, 목록, 원문, 파싱, 로그인 확인 API
- NuGet 의존성은 `Newtonsoft.Json`, `HtmlAgilityPack` 두 개입니다.
- Selenium 실행, DB 연결 및 DB에서 API 키를 조회하는 코드는 이식 대상에서 제외합니다. DOM 수집은 연결된 브라우저 확장을 사용합니다.
- API 키가 필요한 사이트는 `apiSettings`로 전달한 값을 요청별로 사용합니다.

### 실행과 테스트 화면

서버 실행 후 확장 1.4.2를 다시 로드하고 브라우저를 켜둡니다. `http://127.0.0.1:5180/sourcing.html`에서 사이트·기능·URL을 입력합니다. 테스트 화면 없이 콘솔앱에서 아래 API를 호출해도 됩니다.

| 메서드 | 엔드포인트 | 기능 |
| --- | --- | --- |
| GET | `/api/sourcing/sites` | 파서 ID, 사이트명, 통화, 원문 파싱 지원 여부, 필요한 설정 |
| POST | `/api/sourcing/product` | 상품 상세 조회 |
| POST | `/api/sourcing/page` | 목록의 상품 URL·코드·제목 조회 |
| POST | `/api/sourcing/raw` | 파서가 수집한 `url`, `html`, `data` 반환 |
| POST | `/api/sourcing/parse` | 전달한 원문의 상품 파싱 |
| POST | `/api/sourcing/login` | 로그인 확인 결과의 `loginUrl` 반환 |

같은 사이트의 파서가 여러 버전이면 `/sites` 응답의 `id`로 정확한 버전을 선택할 수 있습니다. 예: `Amazon.AmazonUS`, `AliExpress_KR.AliExpress`. 기존 팩토리 별칭(`Amazon-US`, `AliExpress-KR`, `Gmarket`, `1688` 등)도 지원합니다. 모든 파서가 모든 기능을 구현한 것은 아니며, 미지원 기능은 422를 반환합니다. `login`은 사이트별 확인 코드를 호출하며 자동 로그인은 하지 않습니다.

```http
POST http://127.0.0.1:5180/api/sourcing/product
Content-Type: application/json

{
  "site": "Amazon-US",
  "url": "https://www.amazon.com/dp/상품코드",
  "renderWaitMilliseconds": 900,
  "timeoutSeconds": 180
}
```

목록은 `/page`에 목록 URL과 `"page": 1`을 보냅니다. 기본 전체 제한 시간은 180초이며 10~600초로 지정합니다. 개별 브라우저 작업은 60초 이내에 응답해야 합니다.

| 요청 필드 | 용도 |
| --- | --- |
| `headers`, `cookies` | 입력 URL과 같은 출처(스킴·호스트·포트)의 수집 요청에 적용. 다른 출처의 추가 API에는 사이트 파서의 헤더와 브라우저 세션을 사용 |
| `collectOptions` | 기존 `CollectOptionSetting` 구조. 예: `musinsaCollectOption`, `ssgCollectOption`, `lotteOnCollectOption` |
| `apiSettings` | DB에서 읽던 API 키·계정 정보를 요청별로 전달 |
| `renderHtml` | 기본 `false`. 기존 통신 어댑터에서 `true`면 **입력 URL과 동일한 GET 요청**을 DOM으로 수집. 현재 알리 파서는 이 설정과 관계없이 상품·목록 페이지를 DOM으로 수집하고, API·상세 설명은 원문으로 처리 |
| `html`, `data` | `/parse`에 사용할 저장된 원문. `/raw` 응답의 필드를 전달 |

API 설정:

- 도매매 상품: `"apiSettings": { "domemeApiKey": "본인의 API 키" }`
- 오너클랜: `ownerClanToken` 또는 `ownerClanAccount`와 `ownerClanPassword`. 발급 토큰을 DB에 저장하지 않습니다.
- 타오바오 상품의 기존 외부 API: `taobaoApiToken`. 기존 파서의 외부 API 이용 권한이 필요합니다.

### 알리익스프레스

`AliExpress-KR` 또는 `AliExpress_KR.AliExpress`는 `Sourcing/AliExpress-KR/AliExpressV2.cs`의 현재 파서를 사용합니다. `AliExpress_KR.AliExpress_Temp`는 별도로 남아 있는 구형 파서입니다.

```http
POST http://127.0.0.1:5180/api/sourcing/product
Content-Type: application/json

{
  "site": "AliExpress-KR",
  "url": "https://ko.aliexpress.com/item/상품코드.html",
  "timeoutSeconds": 180
}
```

상품 DOM HTML → H5 API URL에 적용되는 쿠키 조회 → 쿠키의 `_m_h5_tk`로 서명 → 상품 API → 상세 HTML 순서로 수집 시작 때 자동 선택한 연결을 사용합니다. 고정된 이전 Chrome 버전의 User-Agent를 덮어쓰던 코드는 제거하여 브라우저의 기본값을 사용합니다.

`AliExpressV2.cs`는 `ChatHubService.GetRequest` 대신 `coordinator.FetchAsync`로 조회합니다. 상품·목록 페이지는 기본 `BrowserFetchMode.Dom`으로 탭을 열어 DOM HTML을 읽습니다. 계정 JSON, 상품 JSONP, 공통 쿠키 설정, 상세 설명 HTML은 `BrowserFetchMode.Response`로 확장 서비스 워커의 `fetch`를 사용해 응답 원문을 받습니다. JSON을 DOM으로 변환하거나 상세 설명에 문서 태그를 추가하지 않도록 구분합니다. 쿠키 조회도 coordinator를 사용합니다.

이 모드는 서버 코드에서 지정하며 API 페이로드에는 추가할 필요가 없습니다. 같은 수집 실행 범위의 연결·취소 토큰·요청 헤더·쿠키를 전달합니다. `renderWaitMilliseconds`는 DOM 조회에 적용합니다.

서명 쿠키가 없으면 해당 브라우저에서 알리 상품 페이지를 열고 로그인·차단 상태를 확인해야 합니다. HTML에 DCData/extParams가 없거나 상품 API의 JSON/JSONP 구조가 다르면 원인을 오류로 반환합니다. 알리의 서명에는 브라우저에 저장된 쿠키를 사용합니다. 요청의 `cookies` 필드는 쿠키 저장소를 바꾸지 않으므로 서명용 쿠키를 준비하는 용도로 사용할 수 없습니다.

### 브라우저 통신 방식

```text
콘솔/테스트 화면 → SourcingController → 사이트 파서
→ BrowserRequestCoordinator / BrowserChatHubService / BrowserHttpClient / BrowserWebRequest
→ SignalR → 자동 연결된 브라우저 확장 → 사이트의 HTML·JSON 응답
→ 사이트 파서 → 상품/목록 JSON
```

옮겨온 `HttpClient`와 `WebRequest` 호출은 브라우저로 전달하는 어댑터로 교체했습니다. 서버의 HTTP 클라이언트 API는 메시지·본문·응답을 구성하는 용도이며, 대상 사이트로 서버에서 직접 요청하지 않습니다. 프록시, TLS 설정, 압축 해제와 연결 설정은 브라우저가 담당합니다. 기존 서버 프록시 설정을 브라우저에 적용하지 않습니다.

공통 수집 인터페이스는 서버가 자동 선택한 `connectionId`를 내부적으로 전달합니다. 현재 알리 파서는 `SourcingExecution`에서 coordinator와 요청 설정을 받아 `FetchAsync`·`ReadCookiesAsync`를 호출합니다. 다른 파서의 `IChatHubService`와 HTTP 호환 어댑터도 같은 수집 실행 범위의 연결을 사용합니다. API 호출자는 연결 ID를 지정할 필요가 없습니다.

기존 `IChatHubService.GetRequest/PostRequest`가 받던 응답 원문을 유지하기 위해 해당 어댑터의 기본 수집은 **확장 서비스 워커의 `fetch`**로 처리합니다. `credentials: include`로 브라우저 프로필의 세션을 사용하며 GET/POST, 요청 본문, 헤더를 지원합니다. 원문은 Base64 바이트로 전달하여 파서의 UTF-8/EUC-KR/GBK 인코딩 처리를 유지합니다. 개별 원문 응답은 10MiB 이하여야 합니다. DOM 방식은 `/api/fetch`, 현재 알리의 상품·목록, 쿠팡로켓, 네이버+스토어, `renderHtml` 설정에서 사용합니다. 쿠팡·네이버는 별도 ChromeDriver를 띄우지 않습니다.

원문 요청의 임시 헤더 규칙은 확장의 요청과 정확한 URL·메서드에만 적용합니다. 같은 수집의 다른 URL에도 각각 새 규칙을 등록합니다. 다른 URL로의 리디렉션이나 DOM 내부 API에 사용자 지정 값을 자동으로 전달하지 않습니다. 사이트 코드가 명시한 헤더는 사용자 헤더보다 우선하고, 같은 출처에 사용자가 지정한 `cookies`는 Cookie 헤더를 교체합니다. 생략하면 브라우저 쿠키를 사용합니다. 캐시·압축·Host·본문 길이·연결 헤더는 브라우저가 관리합니다.

확장 `fetch`와 탭 이동은 페이지 스크립트가 보내는 요청과 출처·렌더링 동작이 다릅니다. 사이트가 쿠키 외에 CSRF 토큰, 웹 저장소 값, 추가 인증, CAPTCHA 등을 요구하면 별도 사이트별 처리가 필요할 수 있습니다. 브라우저 사용만으로 차단 방지를 보장하지 않습니다. 참고: [확장 네트워크 요청](https://developer.chrome.com/docs/extensions/develop/concepts/network-requests), [쿠키 조회 권한과 파티션](https://developer.chrome.com/docs/extensions/reference/api/cookies).

### 수집 결과 확인

원본의 상품 JSON 필드와 상태 enum 이름을 유지합니다. HTTP 200이어도 상품의 `collectStatus`가 `Failed`일 수 있으므로 `collectStatus`, `saleStatus`, `memo`를 함께 확인합니다. 브라우저 미연결은 503, 시간 초과는 504, 확장 작업 실패는 502, CAPTCHA·미지원 기능·파싱 실패는 422입니다. 페이지 구조나 사이트 API가 바뀌면 해당 파서를 수정해야 합니다.

이식 후 빌드·JavaScript 구문 확인은 수행했으며, 각 실제 사이트의 로그인·수집 성공 여부는 아직 검증하지 않았습니다.


