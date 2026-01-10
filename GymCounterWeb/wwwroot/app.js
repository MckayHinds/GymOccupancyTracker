// If you set API_KEY in appsettings.json, put the same key here for the test button.
// If API_KEY is "", leave this as "".
const API_KEY = "CHANGE_ME_TO_SOMETHING_SECRET"; // or ""

async function getOccupancy() {
  const res = await fetch("/api/occupancy", { cache: "no-store" });
  if (!res.ok) throw new Error("Could not load occupancy");
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
    const data = await getOccupancy();
    setText("count", data.activeLastHour);
    setText("status", "");
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
