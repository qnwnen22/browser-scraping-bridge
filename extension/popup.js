const state = document.getElementById("state");
const registrationError = document.getElementById("registration-error");

function renderStatus(response) {
  const connected = Boolean(response?.connected);
  state.classList.toggle("online", connected);
  state.lastElementChild.textContent = connected ? "백엔드에 연결됨" : "백엔드 연결 대기 중";
  registrationError.textContent = response?.registrationError || "";
  registrationError.hidden = !response?.registrationError;
}

async function refreshStatus() {
  try {
    const response = await chrome.runtime.sendMessage({ type: "get-status" });
    if (response) renderStatus(response);
  } catch {
    state.classList.remove("online");
    state.lastElementChild.textContent = "연결 상태를 확인하지 못했습니다";
  }
}

void refreshStatus();
setInterval(() => { void refreshStatus(); }, 1000);
