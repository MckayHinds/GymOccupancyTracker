async function loadTrafficPeaks() {
  const status = document.getElementById("status");
  status.textContent = "Loading traffic data…";

  try {
    const res = await fetch("/api/traffic/peaks", { cache: "no-store" });
    if (!res.ok) throw new Error("Could not load traffic data");

    const data = await res.json();

    // Fill Day table
    const dayBody = document.querySelector("#dayTable tbody");
    dayBody.innerHTML = "";
    for (const row of data.peakByDayOfWeek) {
      dayBody.insertAdjacentHTML(
        "beforeend",
        `<tr><td>${row.dayOfWeek}</td><td>${row.peak}</td></tr>`
      );
    }

    // Fill Hour table
    const hourBody = document.querySelector("#hourTable tbody");
    hourBody.innerHTML = "";
    for (const row of data.peakByHour) {
      const hourLabel = String(row.hour).padStart(2, "0") + ":00";
      hourBody.insertAdjacentHTML(
        "beforeend",
        `<tr><td>${hourLabel}</td><td>${row.peak}</td></tr>`
      );
    }

    status.textContent = `Data from last ${data.retentionDays} days`;
  } catch (err) {
    status.textContent = err.message;
  }
}

loadTrafficPeaks();
