// ---------------- CONFIG ----------------

const BACKEND_BASE_URL = "http://localhost:5168";

// If you set API_KEY in appsettings.json, put the same key here.
// If API_KEY is "", leave this as "".
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
    // Try to extract server response for debugging
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

    // Update big number (hour-based count)
    setText("count", occ.activeLastHour);

    // Status shows latest MQTT message if we have one
    if (
      latest.latestMessage &&
      latest.latestMessageUtc &&
      latest.latestMessageUtc !== "0001-01-01T00:00:00"
    ) {
      const t = new Date(latest.latestMessageUtc).toLocaleTimeString();
      setText("status", `Last activity: ${latest.latestMessage} @ ${t}`);
    } else {
      setText("status", "Waiting for sensor data…");
    }
  } catch (err) {
    setText("status", err.message);
  }
}

document.getElementById("testBtn").addEventListener("click", async () => {
  try {
    await postEntry();
  } catch (err) {
    setText("status", err.message);
  }
  await refresh();
});

document.getElementById("refreshBtn").addEventListener("click", refresh);

refresh();
setInterval(refresh, 5000);
