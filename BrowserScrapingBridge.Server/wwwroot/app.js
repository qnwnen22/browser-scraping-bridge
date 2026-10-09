const form = document.getElementById("request-form");
const statusNode = document.getElementById("status");
const fetchButton = document.getElementById("fetch-button");
const messageNode = document.getElementById("message");
const resultNode = document.getElementById("result");
const htmlOutput = document.getElementById("html-output");
const downloadButton = document.getElementById("download-button");
let lastResult = null;
let requesting = false;

function readStringMap(id, label) {
  const text = document.getElementById(id).value.trim();
  if (!text) return undefined;
  let value;
  try { value = JSON.parse(text); }
  catch { throw new Error(`${label}를 올바른 JSON 객체로 입력해주세요.`); }
  if (value === null || Array.isArray(value) || typeof value !== "object" ||
      Object.values(value).some(item => typeof item !== "string"))
    throw new Error(`${label}는 이름과 문자열 값으로 구성된 JSON 객체여야 합니다.`);
  return value;
}

async function refreshStatus() {
  try {
    const response = await fetch("/api/status", { cache: "no-store" });
    if (!response.ok) throw new Error("Status unavailable");
    const status = await response.json();
    statusNode.classList.toggle("online", Boolean(status.browserConnected));
    statusNode.lastElementChild.textContent = status.browserConnected ?
      "브라우저 연결됨" : "브라우저 확장 연결 대기 중";
  } catch {
    statusNode.classList.remove("online");
    statusNode.lastElementChild.textContent = "서버에 연결할 수 없습니다";
  }
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (requesting) return;
  requesting = true;
  fetchButton.disabled = true;
  fetchButton.textContent = "요청 중…";
  messageNode.classList.remove("error");
  messageNode.textContent = "브라우저가 페이지를 열고 HTML을 가져오고 있습니다.";
  resultNode.hidden = true;
  downloadButton.disabled = true;
  lastResult = null;
  const startedAt = performance.now();

  try {
    const response = await fetch("/api/fetch", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        url: document.getElementById("url").value.trim(),
        renderWaitMilliseconds: Number(document.getElementById("wait").value),
        headers: readStringMap("headers", "헤더"),
        cookies: readStringMap("cookies", "쿠키")
      })
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.error ?? data.detail ?? "HTML 요청에 실패했습니다.");

    lastResult = data;
    resultNode.hidden = false;
    document.getElementById("page-title").textContent = data.title || "제목 없음";
    document.getElementById("final-url").textContent = data.finalUrl || data.url;
    document.getElementById("http-status").textContent = data.httpStatusCode ?? "확인할 수 없음";
    document.getElementById("html-size").textContent = new Blob([data.html || ""]).size.toLocaleString() + " bytes";
    document.getElementById("elapsed").textContent = ((performance.now() - startedAt) / 1000).toFixed(2) + "초";
    htmlOutput.value = data.html || "";
    downloadButton.disabled = !data.html;
    messageNode.classList.toggle("error", Boolean(data.error));
    messageNode.textContent = data.error || "브라우저가 HTML을 반환했습니다.";
  } catch (error) {
    messageNode.classList.add("error");
    messageNode.textContent = error.message || "서버에 연결할 수 없습니다.";
  } finally {
    requesting = false;
    fetchButton.disabled = false;
    fetchButton.textContent = "HTML 요청";
    void refreshStatus();
  }
});

downloadButton.addEventListener("click", () => {
  if (!lastResult?.html) return;
  const objectUrl = URL.createObjectURL(new Blob([lastResult.html], { type: "text/html;charset=utf-8" }));
  const link = document.createElement("a");
  let filename = "page";
  try { filename = new URL(lastResult.finalUrl || lastResult.url).hostname.replace(/[^a-zA-Z0-9.-]/g, "_"); } catch { }
  link.href = objectUrl;
  link.download = filename + ".html";
  link.click();
  setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
});

void refreshStatus();
setInterval(refreshStatus, 3000);

