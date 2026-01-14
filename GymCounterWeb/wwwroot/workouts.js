// Array of workout slides
const workouts = [
  {
    day: "Monday",
    text: "Steady-State Cardio: 30–40 minutes of moderate jogging, cycling, or brisk walking."
  },
  {
    day: "Tuesday",
    text: "HIIT: 5-min warm-up, 20 minutes of 30s fast / 90s slow intervals, 5-min cooldown."
  },
  {
    day: "Wednesday",
    text: "Low-Impact Cardio + Core: 30 minutes swimming/rowing + 10 minutes core work."
  },
  {
    day: "Thursday",
    text: "Tempo Cardio: 5-min warm-up, 20–25 minutes at a comfortably hard pace, 5-min cooldown."
  },
  {
    day: "Friday",
    text: "Mixed Cardio Circuit: Rotate 5-minute blocks (jump rope, jog, stairs, rowing, boxing)."
  },
  {
    day: "Saturday",
    text: "Long Slow Distance: 45–60 minutes at an easy, conversational pace."
  },
  {
    day: "Sunday",
    text: "Active Recovery: 20–30 minutes of walking, stretching, or yoga."
  }
];

let currentIndex = 0;

// Update the slide content
function updateSlide() {
  const dayEl = document.getElementById("day");
  const textEl = document.getElementById("text");

  dayEl.textContent = workouts[currentIndex].day;
  textEl.textContent = workouts[currentIndex].text;
}

// Move to next day
function nextSlide() {
  currentIndex = (currentIndex + 1) % workouts.length;
  updateSlide();
}

// Move to previous day
function prevSlide() {
  currentIndex = (currentIndex - 1 + workouts.length) % workouts.length;
  updateSlide();
}

// Initialize on page load
window.onload = updateSlide;