const state = document.getElementById("state");

chrome.runtime.sendMessage({ type: "get-status" }, (response) => {
  const connected = Boolean(response?.connected);
  state.classList.toggle("online", connected);
  state.lastElementChild.textContent = connected ? "백엔드에 연결됨" : "백엔드 연결 대기 중";
});
