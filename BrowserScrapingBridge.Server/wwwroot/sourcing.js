const byId = id => document.getElementById(id);
let sites = [];

function readJson(id, stringsOnly = false) {
  const value = byId(id).value.trim();
  if (!value) return undefined;
  const object = JSON.parse(value);
  if (object == null || Array.isArray(object) || typeof object !== "object") throw new Error(`${id}: JSON 객체를 입력해주세요.`);
  if (stringsOnly && Object.values(object).some(item => typeof item !== "string")) throw new Error(`${id}: 값은 문자열이어야 합니다.`);
  return object;
}

function updateSite() {
  const site = sites.find(item => item.id === byId("site").value);
  byId("site-info").textContent = site ? `${site.site} · ${site.currency} · 원문 파싱 ${site.supportsParsing ? "지원" : "미지원"}${site.requiredSettings.length ? " · 설정: " + site.requiredSettings.join(", ") : ""}` : "";
}

async function updateStatus() {
  try {
    const response = await fetch("/api/status", { cache: "no-store" });
    if (!response.ok) throw new Error();
    const status = await response.json();
    byId("status").classList.toggle("online", Boolean(status.browserConnected));
    byId("status").lastElementChild.textContent = status.browserConnected ?
      "브라우저 연결됨" : "브라우저 확장을 연결해주세요";
  } catch {
    byId("status").classList.remove("online");
    byId("status").lastElementChild.textContent = "서버 연결을 확인해주세요";
  }
}

byId("site").addEventListener("change", updateSite);
byId("operation").addEventListener("change", () => {
  const parsing = byId("operation").value === "parse";
  byId("parse-fields").hidden = !parsing;
});
byId("sourcing-form").addEventListener("submit", async event => {
  event.preventDefault();
  const button = byId("submit-button");
  const message = byId("message");
  message.classList.remove("error");
  byId("html-output").value = "";
  try {
    const operation = byId("operation").value;
    const request = {
      site: byId("site").value, url: byId("url").value.trim(), page: Number(byId("page").value),
      renderWaitMilliseconds: Number(byId("wait").value), timeoutSeconds: Number(byId("timeout").value),
      renderHtml: byId("render-html").checked,
      headers: readJson("headers", true), cookies: readJson("cookies", true),
      collectOptions: readJson("collect-options"), apiSettings: readJson("api-settings", true)
    };
    if (operation === "parse") {
      request.html = byId("raw-html").value;
      const data = byId("raw-data").value.trim();
      request.data = data ? JSON.parse(data) : undefined;
    }
    button.disabled = true;
    message.textContent = "수집 중…";
    const start = performance.now();
    const response = await fetch(`/api/sourcing/${operation}`, {
      method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(request)
    });
    const result = await response.json();
    byId("html-output").value = JSON.stringify(result, null, 2);
    if (!response.ok) throw new Error(result.error ?? result.detail ?? result.title ?? `HTTP ${response.status}`);
    message.textContent = `응답 완료 (${((performance.now() - start) / 1000).toFixed(1)}초). 상품의 collectStatus와 memo도 확인해주세요.`;
  } catch (error) {
    message.textContent = error.message;
    message.classList.add("error");
  } finally { button.disabled = false; }
});

void (async () => {
  try {
    const response = await fetch("/api/sourcing/sites");
    if (!response.ok) throw new Error("사이트 목록을 읽지 못했습니다.");
    sites = await response.json();
    for (const site of sites) {
      const option = document.createElement("option");
      option.value = site.id;
      option.textContent = `${site.name} (${site.id})`;
      byId("site").append(option);
    }
    updateSite();
  } catch (error) { byId("message").textContent = error.message; }
})();
void updateStatus();
setInterval(updateStatus, 5000);
