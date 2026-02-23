// ---------------- CONFIG ----------------
const BACKEND_BASE_URL = "http://localhost:5168";
const API_KEY = "";

// ---------------- HELPERS ----------------
function setText(id, value) {
  const el = document.getElementById(id);
  if (el) el.textContent = value;
}

async function fetchJson(path, options = {}) {
  const url = `${BACKEND_BASE_URL}${path}`;
  const res = await fetch(url, { cache: "no-store", ...options });

  if (!res.ok) {
    let bodyText = "";
    try { bodyText = await res.text(); } catch {}
    throw new Error(`Request failed: ${res.status} ${res.statusText}${bodyText ? " - " + bodyText : ""}`);
  }

  return await res.json();
}

// ---------------- API CALLS ----------------
function getOccupancy() {
  return fetchJson("/api/occupancy");
}

function getLatest() {
  return fetchJson("/api/latest");
}

function postEntry() {
  return fetchJson("/api/entry", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      source: "web-test",
      apiKey: API_KEY
    })
  });
}

// ---------------- UI REFRESH ----------------
async function refresh() {
  try {
    setText("backendInfo", `Backend: ${BACKEND_BASE_URL}`);

    const [occ, latest] = await Promise.all([getOccupancy(), getLatest()]);

    setText("gym-count", occ.activeLastHour);
    if (
      latest.latestMessage &&
      latest.latestMessageUtc &&
      latest.latestMessageUtc !== "0001-01-01T00:00:00"
    ) {
      const t = new Date(latest.latestMessageUtc).toLocaleTimeString();
      setText("backendInfo", `Backend: ${BACKEND_BASE_URL} • Last activity: ${latest.latestMessage} @ ${t}`);
    } else {
      setText("backendInfo", `Backend: ${BACKEND_BASE_URL} • Waiting for sensor data…`);
    }
  } catch (err) {
    setText("backendInfo", `Backend: ${BACKEND_BASE_URL} • Error: ${err.message}`);
  }
}

// ---------------- OPTIONAL BUTTONS ----------------
const testBtn = document.getElementById("testBtn");
if (testBtn) {
  testBtn.addEventListener("click", async () => {
    try {
      await postEntry();
    } catch (err) {
      setText("backendInfo", err.message);
    }
    await refresh();
  });
}

const refreshBtn = document.getElementById("refreshBtn");
if (refreshBtn) {
  refreshBtn.addEventListener("click", refresh);
}

// Start polling
refresh();
setInterval(refresh, 2000);