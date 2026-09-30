const HUB_URL = "ws://127.0.0.1:5180/hubs/browser";
const RECORD_SEPARATOR = "\u001e";
const WEB_URLS = ["http://*/*", "https://*/*"];
let socket = null;
let connected = false;
let invocationId = 0;
let reconnectDelay = 1000;
let reconnectTimer = null;
let requestRulesInitialized = false;
let nextRequestRuleId = 0;
const captureTabs = new Set();
const tabStatus = new Map();
const tabErrors = new Map();

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (message?.type === "get-status") sendResponse({ connected });
  return false;
});

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

  let receiveBuffer = "";
  let handshakeComplete = false;
  let registrationId = null;
  connection.onopen = () => connection.send(JSON.stringify({ protocol: "json", version: 1 }) + RECORD_SEPARATOR);
  connection.onmessage = (event) => {
    try {
      receiveBuffer += event.data;
      const frames = receiveBuffer.split(RECORD_SEPARATOR);
      receiveBuffer = frames.pop() ?? "";
      for (const frame of frames) {
        if (!frame) continue;
        const message = JSON.parse(frame);
        if (!handshakeComplete) {
          if (message.error) { connection.close(); return; }
          handshakeComplete = true;
          registrationId = invoke("RegisterBrowser", [], connection);
          continue;
        }
        if (message.type === 3 && message.invocationId === registrationId) {
          if (message.error) { connection.close(); return; }
          connected = true;
          reconnectDelay = 1000;
          continue;
        }
        handleFrame(message, connection);
      }
    } catch {
      connection.close();
    }
  };
  connection.onclose = () => {
    if (socket === connection) {
      connected = false;
      socket = null;
      scheduleReconnect();
    }
  };
  connection.onerror = () => {
    if (connection.readyState === WebSocket.OPEN || connection.readyState === WebSocket.CONNECTING)
      connection.close();
  };
}

function scheduleReconnect() {
  if (reconnectTimer) return;
  reconnectTimer = setTimeout(() => { reconnectTimer = null; connect(); }, reconnectDelay);
  reconnectDelay = Math.min(reconnectDelay * 2, 15000);
}

function handleFrame(message, connection) {
  if (message.type === 6) return;
  if (message.type === 7) { connection.close(); return; }
  if (message.type !== 1 || message.target !== "FetchPage") return;
  const job = message.arguments?.[0];
  if (job?.requestId && job?.url)
    void fetchPage(job, connection).catch(() => console.warn("HTML request could not be completed."));
}

async function fetchPage(job, connection) {
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
    await navigateAndWait(tabId, target.href, 45000);
    await removeRequestRule(requestRuleId);
    requestRuleId = null;
    const wait = Math.min(10000, Math.max(0, job.renderWaitMilliseconds ?? 900));
    await new Promise((resolve) => setTimeout(resolve, wait));

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
    result.error = String(error?.message ?? error);
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
    if (socket === connection) invoke("CompleteFetch", [result], connection);
  }
}

async function applyRequestOptions(tabId, target, job) {
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
          resourceTypes: ["main_frame"],
          requestMethods: ["get"]
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

function navigateAndWait(tabId, url, timeoutMs) {
  return new Promise((resolve, reject) => {
    let settled = false;
    const timeout = setTimeout(() => finish(new Error("Timed out waiting for the page to load.")), timeoutMs);
    const onUpdated = (updatedTabId, changeInfo, tab) => {
      if (updatedTabId === tabId && changeInfo.status === "complete" && tab.url !== "about:blank") finish();
    };
    const onRemoved = (removedTabId) => {
      if (removedTabId === tabId) finish(new Error("The page tab was closed before it loaded."));
    };
    function finish(error) {
      if (settled) return;
      settled = true;
      clearTimeout(timeout);
      chrome.tabs.onUpdated.removeListener(onUpdated);
      chrome.tabs.onRemoved.removeListener(onRemoved);
      error ? reject(error) : resolve();
    }
    chrome.tabs.onUpdated.addListener(onUpdated);
    chrome.tabs.onRemoved.addListener(onRemoved);
    chrome.tabs.update(tabId, { url }).catch(finish);
  });
}

setInterval(() => {
  if (socket?.readyState === WebSocket.OPEN) sendFrame({ type: 6 });
  else connect();
}, 15000);

void initializeRequestRules();

