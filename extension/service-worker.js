importScripts("naver-snapshot.js");

const HUB_URL = "ws://127.0.0.1:5180/hubs/browser";
const RECORD_SEPARATOR = "\u001e";
const WEB_URLS = ["http://*/*", "https://*/*"];
const REGISTRATION_BUFFER_LIMIT = 32;
const BUFFERED_REGISTRATION_TARGETS = new Set(["FetchPage", "FetchResource", "ReadCookies", "CancelRequest"]);
let socket = null;
let connected = false;
let registrationError = null;
let invocationId = 0;
let reconnectDelay = 1000;
let reconnectTimer = null;
let requestRulesInitialized = false;
let nextRequestRuleId = 0;
const captureTabs = new Set();
const tabStatus = new Map();
const tabErrors = new Map();
const jobs = new Map();
let jobQueue = Promise.resolve();

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (message?.type === "get-status") {
    sendResponse(getStatus());
  }
  return false;
});

function getStatus() {
  return { connected, registrationError };
}

function abortConnectionJobs(connection) {
  for (const state of jobs.values()) if (state.connection === connection) state.controller.abort();
}

chrome.webRequest.onCompleted.addListener((details) => {
  if (captureTabs.has(details.tabId)) tabStatus.set(details.tabId, details.statusCode);
}, { urls: WEB_URLS, types: ["main_frame"] });

chrome.webRequest.onErrorOccurred.addListener((details) => {
  if (captureTabs.has(details.tabId)) tabErrors.set(details.tabId, details.error);
}, { urls: WEB_URLS, types: ["main_frame"] });

function sendFrame(message, connection = socket) {
  if (connection?.readyState !== WebSocket.OPEN) return false;
  connection.send(JSON.stringify(message) + RECORD_SEPARATOR);
  return true;
}

function invoke(target, args, connection = socket) {
  const id = String(++invocationId);
  return sendFrame({ type: 1, invocationId: id, target, arguments: args }, connection) ? id : null;
}

function connect() {
  if (!requestRulesInitialized) return;
  if (socket && (socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING)) return;
  let connection;
  try { connection = new WebSocket(HUB_URL); }
  catch { scheduleReconnect(); return; }
  socket = connection;
  connected = false;

  let receiveBuffer = "";
  let handshakeComplete = false;
  let registrationId = null;
  const pendingRegistrationFrames = [];
  connection.onopen = () => {
    if (socket !== connection) { connection.close(); return; }
    connection.send(JSON.stringify({ protocol: "json", version: 1 }) + RECORD_SEPARATOR);
  };
  connection.onmessage = (event) => {
    if (socket !== connection || connection.readyState !== WebSocket.OPEN) {
      pendingRegistrationFrames.length = 0;
      return;
    }
    try {
      receiveBuffer += event.data;
      const frames = receiveBuffer.split(RECORD_SEPARATOR);
      receiveBuffer = frames.pop() ?? "";
      for (const frame of frames) {
        if (socket !== connection || connection.readyState !== WebSocket.OPEN) {
          pendingRegistrationFrames.length = 0;
          return;
        }
        if (!frame) continue;
        const message = JSON.parse(frame);
        if (!handshakeComplete) {
          if (message.error) {
            pendingRegistrationFrames.length = 0;
            registrationError = "SignalR 연결 협상에 실패했습니다.";
            connection.close();
            return;
          }
          handshakeComplete = true;
          registrationId = invoke("RegisterBrowser", [], connection);
          continue;
        }
        if (message.type === 3 && message.invocationId === registrationId) {
          if (message.error) {
            pendingRegistrationFrames.length = 0;
            registrationError = "브라우저 연결 등록에 실패했습니다. 서버를 재실행하고 확장 프로그램을 다시 로드하세요.";
            connection.close();
            return;
          }
          connected = true;
          registrationError = null;
          reconnectDelay = 1000;
          // The server may route a request before the registration completion arrives.
          // Preserve frame order so a queued cancellation still follows its request.
          while (pendingRegistrationFrames.length && socket === connection && connected && connection.readyState === WebSocket.OPEN)
            handleFrame(pendingRegistrationFrames.shift(), connection);
          pendingRegistrationFrames.length = 0;
          continue;
        }
        if (message.type === 7)
          pendingRegistrationFrames.length = 0;
        if (!connected && message.type === 1 && BUFFERED_REGISTRATION_TARGETS.has(message.target)) {
          if (pendingRegistrationFrames.length >= REGISTRATION_BUFFER_LIMIT) {
            pendingRegistrationFrames.length = 0;
            registrationError = "브라우저 등록 완료 전 대기 요청이 한도를 초과했습니다. 서버 연결을 확인하세요.";
            connection.close();
            return;
          }
          pendingRegistrationFrames.push(message);
          continue;
        }
        handleFrame(message, connection);
      }
    } catch {
      pendingRegistrationFrames.length = 0;
      connection.close();
    }
  };
  connection.onclose = () => {
    pendingRegistrationFrames.length = 0;
    abortConnectionJobs(connection);
    if (socket === connection) {
      connected = false;
      socket = null;
      scheduleReconnect();
    }
  };
  connection.onerror = () => {
    pendingRegistrationFrames.length = 0;
    if (connection.readyState === WebSocket.OPEN || connection.readyState === WebSocket.CONNECTING)
      connection.close();
  };
}

function scheduleReconnect() {
  if (reconnectTimer != null) return;
  reconnectTimer = setTimeout(() => { reconnectTimer = null; connect(); }, reconnectDelay);
  reconnectDelay = Math.min(reconnectDelay * 2, 15000);
}

function handleFrame(message, connection) {
  if (message.type === 6) return;
  if (message.type === 7) { connection.close(); return; }
  if (message.type !== 1) return;
  if (!connected || socket !== connection) return;
  const job = message.arguments?.[0];
  if (!job?.requestId) return;
  if (message.target === "CancelRequest") { jobs.get(job.requestId)?.controller.abort(); return; }
  const handler = { FetchPage: fetchPage, FetchResource: fetchResource, ReadCookies: readCookies }[message.target];
  if (!handler) return;
  const controller = new AbortController();
  jobs.set(job.requestId, { controller, connection });
  jobQueue = jobQueue.then(async () => {
    if (controller.signal.aborted) return;
    await handler(job, connection, controller.signal);
  }).catch(() => console.warn("Browser request could not be completed."))
    .finally(() => jobs.delete(job.requestId));
}

async function fetchPage(job, connection, signal) {
  let tabId = -1;
  let requestRuleId = null;
  const result = {
    requestId: job.requestId,
    url: job.url,
    finalUrl: job.url,
    title: "",
    httpStatusCode: null,
    html: "",
    error: null
  };

  try {
    const target = new URL(job.url);
    if (!["http:", "https:"].includes(target.protocol)) throw new Error("Only HTTP and HTTPS URLs are supported.");
    const tab = await chrome.tabs.create({ url: "about:blank", active: false });
    if (!Number.isInteger(tab.id)) throw new Error("Could not create a browser tab.");
    tabId = tab.id;
    captureTabs.add(tabId);
    requestRuleId = await applyRequestOptions(tabId, target, job);
    await navigateAndWait(tabId, target.href, 45000, signal);
    await removeRequestRule(requestRuleId);
    requestRuleId = null;
    const wait = Math.min(10000, Math.max(0, job.renderWaitMilliseconds ?? 900));
    await abortableDelay(wait, signal);

    if (job.extract === "naver") {
      await chrome.scripting.executeScript({ target: { tabId }, func: () => window.scrollTo(0, document.body.scrollHeight * 0.5) });
      await abortableDelay(1500, signal);
      const snapshots = await chrome.scripting.executeScript({ target: { tabId }, world: "MAIN", func: readNaverSnapshot });
      result.extractedData = snapshots[0]?.result ?? {};
    }

    const scripts = await chrome.scripting.executeScript({
      target: { tabId },
      func: () => ({
        html: (document.doctype ? new XMLSerializer().serializeToString(document.doctype) + "\n" : "") +
          (document.documentElement?.outerHTML ?? ""),
        url: location.href,
        title: document.title
      })
    });
    const page = scripts[0]?.result;
    if (!page) throw new Error("Could not read the page HTML.");
    result.finalUrl = page.url;
    result.title = page.title;
    result.html = page.html;
    result.httpStatusCode = tabStatus.get(tabId) ?? null;
    result.error = tabErrors.get(tabId) ?? (!result.html ? "The page returned an empty document." : null);

    if (new TextEncoder().encode(JSON.stringify(result)).byteLength > 15 * 1024 * 1024) {
      result.html = "";
      result.error = "HTML response exceeds the 15MiB return limit.";
    }
  } catch (error) {
    result.error = signal.aborted ? "Browser request was cancelled." : "Could not load or read the page. Check the browser and extension permissions.";
    result.httpStatusCode = tabStatus.get(tabId) ?? null;
  } finally {
    if (tabId >= 0) {
      captureTabs.delete(tabId);
      tabStatus.delete(tabId);
      tabErrors.delete(tabId);
      try { await chrome.tabs.remove(tabId); } catch { }
    }
    try { await removeRequestRule(requestRuleId); }
    catch { result.error ??= "Could not clear temporary request headers. Reload the extension."; }
    completeResult(result, connection);
  }
}

async function applyRequestOptions(tabId, target, job, resource = false) {
  const requestHeaders = Object.entries(job.headers ?? {}).map(([header, value]) => ({
    header: header.toLowerCase(), operation: "set", value
  }));

  if (job.cookies != null) {
    if (requestHeaders.some(item => item.header === "cookie"))
      throw new Error("Specify either Headers.Cookie or Cookies, not both.");
    const value = Object.entries(job.cookies).map(([name, value]) => `${name}=${value}`).join("; ");
    requestHeaders.push(value
      ? { header: "cookie", operation: "set", value }
      : { header: "cookie", operation: "remove" });
  }
  if (!requestHeaders.length) return null;

  // Bind supplied credentials to the initial document URL and this temporary tab.
  // Redirects to other URLs and subresource requests keep the browser defaults.
  const requestUrl = new URL(target.href);
  requestUrl.hash = "";
  requestUrl.username = "";
  requestUrl.password = "";
  const regex = "^" + requestUrl.href.replace(/[.*+?^${}()|[\]\\]/g, "\\$&") + "$";
  const supported = await chrome.declarativeNetRequest.isRegexSupported({ regex, isCaseSensitive: true });
  if (!supported.isSupported)
    throw new Error("The URL is too complex to apply request headers with a browser rule.");

  const id = ++nextRequestRuleId;
  try {
    await chrome.declarativeNetRequest.updateSessionRules({
      addRules: [{
        id,
        priority: 1,
        action: { type: "modifyHeaders", requestHeaders },
        condition: {
          regexFilter: regex,
          isUrlFilterCaseSensitive: true,
          tabIds: [tabId],
          ...(resource ? { initiatorDomains: [chrome.runtime.id] } : {}),
          resourceTypes: [resource ? "xmlhttprequest" : "main_frame"],
          requestMethods: [resource ? job.method.toLowerCase() : "get"]
        }
      }]
    });
  } catch {
    throw new Error("Could not apply request headers. Check the header values and reload the extension with the updated permissions.");
  }
  return id;
}

async function removeRequestRule(ruleId) {
  if (ruleId != null)
    await chrome.declarativeNetRequest.updateSessionRules({ removeRuleIds: [ruleId] });
}

async function initializeRequestRules() {
  try {
    // Session rules survive a service-worker restart. Clear unfinished jobs first.
    const rules = await chrome.declarativeNetRequest.getSessionRules();
    if (rules.length)
      await chrome.declarativeNetRequest.updateSessionRules({ removeRuleIds: rules.map(rule => rule.id) });
    requestRulesInitialized = true;
    connect();
  } catch {
    console.warn("Could not initialize browser request headers. Reload the extension with the updated permissions.");
    setTimeout(initializeRequestRules, 15000);
  }
}

function navigateAndWait(tabId, url, timeoutMs, signal) {
  return new Promise((resolve, reject) => {
    let settled = false;
    const timeout = setTimeout(() => finish(new Error("Timed out waiting for the page to load.")), timeoutMs);
    const onUpdated = (updatedTabId, changeInfo, tab) => {
      if (updatedTabId === tabId && changeInfo.status === "complete" && tab.url !== "about:blank") finish();
    };
    const onRemoved = (removedTabId) => {
      if (removedTabId === tabId) finish(new Error("The page tab was closed before it loaded."));
    };
    const onAbort = () => finish(new Error("Browser request was cancelled."));
    function finish(error) {
      if (settled) return;
      settled = true;
      clearTimeout(timeout);
      chrome.tabs.onUpdated.removeListener(onUpdated);
      chrome.tabs.onRemoved.removeListener(onRemoved);
      signal.removeEventListener("abort", onAbort);
      error ? reject(error) : resolve();
    }
    chrome.tabs.onUpdated.addListener(onUpdated);
    chrome.tabs.onRemoved.addListener(onRemoved);
    signal.addEventListener("abort", onAbort, { once: true });
    if (signal.aborted) { onAbort(); return; }
    chrome.tabs.update(tabId, { url }).catch(finish);
  });
}

function abortableDelay(ms, signal) {
  return new Promise((resolve, reject) => {
    const onAbort = () => { clearTimeout(timer); reject(new Error("Browser request was cancelled.")); };
    const timer = setTimeout(() => { signal.removeEventListener("abort", onAbort); resolve(); }, ms);
    signal.addEventListener("abort", onAbort, { once: true });
    if (signal.aborted) onAbort();
  });
}

function emptyResult(job) {
  return { requestId: job.requestId, url: job.url ?? "", finalUrl: job.url ?? "", title: "",
    httpStatusCode: null, html: "", error: null };
}

function completeResult(result, connection) {
  if (new TextEncoder().encode(JSON.stringify(result)).byteLength > 15 * 1024 * 1024)
    result = { ...emptyResult(result), error: "Browser result exceeds the 15MiB return limit." };
  if (socket === connection) invoke("CompleteFetch", [result], connection);
}

function base64Bytes(value) {
  const bytes = new Uint8Array(value);
  let binary = "";
  for (let offset = 0; offset < bytes.length; offset += 32768)
    binary += String.fromCharCode(...bytes.subarray(offset, offset + 32768));
  return btoa(binary);
}

async function fetchResource(job, connection, signal) {
  const result = emptyResult(job);
  let ruleId = null;
  const controller = new AbortController();
  const cancel = () => controller.abort();
  signal.addEventListener("abort", cancel, { once: true });
  const timer = setTimeout(cancel, 45000);
  try {
    const target = new URL(job.url);
    if (!["http:", "https:"].includes(target.protocol)) throw new Error("Invalid URL.");
    job.method = (job.method ?? "GET").toUpperCase();
    if (!["GET", "POST", "HEAD", "PUT", "PATCH", "DELETE", "OPTIONS"].includes(job.method)) throw new Error("Invalid HTTP method.");
    if (signal.aborted) controller.abort();
    controller.signal.throwIfAborted();
    ruleId = await applyRequestOptions(-1, target, job, true);
    const body = job.bodyBase64 == null ? undefined : Uint8Array.from(atob(job.bodyBase64), char => char.charCodeAt(0));
    const response = await fetch(target.href, {
      method: job.method,
      ...(body && !["GET", "HEAD"].includes(job.method) ? { body } : {}),
      credentials: "include", redirect: "follow", cache: "no-store", signal: controller.signal
    });
    result.finalUrl = response.url;
    result.httpStatusCode = response.status;
    result.responseHeaders = Object.fromEntries(response.headers.entries());
    const chunks = [];
    let length = 0;
    const reader = response.body?.getReader();
    if (reader) {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        length += value.length;
        if (length > 10 * 1024 * 1024) { await reader.cancel(); throw new Error("Response exceeds 10MiB."); }
        chunks.push(value);
      }
    }
    const bytes = new Uint8Array(length);
    let offset = 0;
    for (const chunk of chunks) { bytes.set(chunk, offset); offset += chunk.length; }
    result.bodyBase64 = base64Bytes(bytes);
  } catch {
    result.error = "Browser resource request failed, timed out, or exceeded 10MiB. Check the site and extension permissions.";
  } finally {
    clearTimeout(timer);
    signal.removeEventListener("abort", cancel);
    try { await removeRequestRule(ruleId); }
    catch { result.error ??= "Could not clear temporary request headers. Reload the extension."; }
    completeResult(result, connection);
  }
}

async function readCookies(job, connection, signal) {
  const result = emptyResult(job);
  try {
    signal.throwIfAborted();
    let query;
    if (/^https?:\/\//i.test(job.domain)) query = { url: new URL(job.domain).href };
    else {
      const domain = String(job.domain ?? "").replace(/^\./, "");
      if (!domain || domain.includes("/") || domain.includes(":")) throw new Error("Invalid cookie domain.");
      query = { domain };
    }
    // 기존 사이트 파서에서 로그인/H5 서명에 필요한 대상 도메인의 쿠키만 읽습니다.
    result.cookiesJson = JSON.stringify(await chrome.cookies.getAll(query));
  } catch { result.error = "Could not read cookies for the requested site. Check extension permissions."; }
  completeResult(result, connection);
}

setInterval(() => {
  if (socket?.readyState === WebSocket.OPEN) sendFrame({ type: 6 });
  else connect();
}, 15000);

void initializeRequestRules();

