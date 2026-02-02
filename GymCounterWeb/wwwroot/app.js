// If you set API_KEY in appsettings.json, put the same key here for the test button.
// If API_KEY is "", leave this as "".
const API_KEY = ""; // or our key

async function getOccupancy() {
  const res = await fetch("/api/occupancy", { cache: "no-store" });
  if (!res.ok) throw new Error("Could not load occupancy");
  return await res.json();
}

async function getLatest() {
  const res = await fetch("/api/latest", { cache: "no-store" });
  if (!res.ok) throw new Error("Could not load latest message");
  return await res.json();
}

async function postEntry() {
  const res = await fetch("/api/entry", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      source: "web-test",
      apiKey: API_KEY
    })
  });
  if (res.status === 401) throw new Error("Unauthorized (API key incorrect)");
  if (!res.ok) throw new Error("Could not record entry");
  return await res.json();
}

function setText(id, value) {
  document.getElementById(id).textContent = value;
}

async function refresh() {
  try {
    // Fetch both endpoints in parallel
    const [occ, latest] = await Promise.all([getOccupancy(), getLatest()]);

    // Update big number
    setText("count", occ.activeLastHour);

    // Status shows latest MQTT message if we have one
    if (latest.latestMessage && latest.latestMessageUtc && latest.latestMessageUtc !== "0001-01-01T00:00:00") {
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
