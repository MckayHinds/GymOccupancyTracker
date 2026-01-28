// Detailed weekly cardio workout plan
const cardioWorkouts = [
  {
    day: "Cardio — Day 1: Steady-State Cardio",
    text: `
• 5-minute warm-up (light jog or brisk walk)
• 30–40 minutes at a steady, moderate pace
   - Jogging outdoors or treadmill
   - Cycling at a consistent cadence
   - Elliptical at medium resistance
• Focus on breathing rhythm and posture
• Optional: 5-minute cooldown walk + stretching
    `
  },
  {
    day: "Cardio — Day 2: HIIT Intervals",
    text: `
• 5-minute warm-up (dynamic stretching + light jog)
• 20 minutes of intervals:
   - 30 seconds fast (sprint, fast cycle, or high-resistance row)
   - 90 seconds slow recovery
• Keep intensity high during the fast bursts
• 5-minute cooldown + deep breathing
    `
  },
  {
    day: "Cardio — Day 3: Low-Impact + Core",
    text: `
• 30 minutes low-impact cardio:
   - Swimming laps
   - Rowing machine at steady pace
   - Incline treadmill walking
• 10 minutes core circuit:
   - 45s plank
   - 20 bicycle crunches
   - 15 leg raises
   - Repeat 2–3 times
• Finish with gentle stretching
    `
  },
  {
    day: "Cardio — Day 4: Tempo Cardio",
    text: `
• 5-minute warm-up
• 20–25 minutes at a “comfortably hard” pace:
   - You can talk, but only in short phrases
   - Maintain consistent speed
• Great for building speed + endurance
• 5-minute cooldown jog/walk
    `
  },
  {
    day: "Cardio — Day 5: Mixed Cardio Circuit",
    text: `
• 30–40 minutes rotating through 5-minute blocks:
   - Jump rope
   - Light jog or treadmill run
   - Stair climbing or step-ups
   - Rowing machine
   - Shadowboxing or cardio boxing
• Keep transitions quick to maintain heart rate
• End with 5 minutes stretching
    `
  },
  {
    day: "Cardio — Day 6: Long Slow Distance (LSD)",
    text: `
• 45–60 minutes at an easy, conversational pace
• Choose your favorite:
   - Running
   - Biking
   - Hiking
   - Elliptical
• Goal: build aerobic base, not speed
• Keep heart rate low and steady
    `
  },
  {
    day: "Cardio — Day 7: Active Recovery",
    text: `
• 20–30 minutes gentle movement:
   - Walking outside
   - Light yoga flow
   - Mobility stretching
• Focus on breathing and relaxation
• Helps reset your body for the next week
    `
  }
];

// Detailed Strength Workouts
const strengthWorkouts = [
  {
    day: "Strength — Day 1: Upper Body Push",
    text: `
• Warm-up: 5 minutes mobility
• 3×10 Bench Press or Push-Ups
• 3×12 Shoulder Press
• 3×12 Incline Press
• 3×15 Tricep Dips
• 2×20 Lateral Raises
    `
  },
  {
    day: "Strength — Day 2: Lower Body (Quads + Glutes)",
    text: `
• Warm-up: 5 minutes cycling
• 4×8 Squats
• 3×12 Lunges
• 3×12 Leg Press
• 3×15 Hip Thrusts
• 3×20 Calf Raises
    `
  },
  {
    day: "Strength — Day 3: Upper Body Pull",
    text: `
• Warm-up: band work
• 3×10 Pull-Ups or Pulldowns
• 3×12 Rows
• 3×12 Face Pulls
• 3×12 Bicep Curls
    `
  },
  {
    day: "Strength — Day 4: Lower Body (Hamstrings + Glutes)",
    text: `
• Warm-up: dynamic legs
• 4×8 Romanian Deadlifts
• 3×12 Hamstring Curls
• 3×12 Split Squats
• 3×15 Glute Kickbacks
    `
  },
  {
    day: "Strength — Day 5: Full Body",
    text: `
• 3×10 Deadlifts
• 3×10 Push-Ups
• 3×12 Rows
• 3×12 Goblet Squats
• Core circuit (plank, twists, leg raises)
    `
  },
  {
    day: "Strength — Day 6: Optional Accessory Day",
    text: `
• Choose 2–3 focus areas:
   - Arms
   - Shoulders
   - Glutes
   - Core
• 3×12 each
    `
  },
  {
    day: "Strength — Day 7: Rest & Mobility",
    text: `
• 10–20 minutes mobility flow
• Optional light walk
    `
  }
];


let cardioIndex = 0;
let strengthIndex = 0;

function updateCardio() {
  document.getElementById("cardio-day").innerText =
    cardioWorkouts[cardioIndex].day;
  document.getElementById("cardio-text").innerText =
    cardioWorkouts[cardioIndex].text;
}

function updateStrength() {
  document.getElementById("strength-day").innerText =
    strengthWorkouts[strengthIndex].day;
  document.getElementById("strength-text").innerText =
    strengthWorkouts[strengthIndex].text;
}

function nextCardio() {
  cardioIndex = (cardioIndex + 1) % cardioWorkouts.length;
  updateCardio();
}

function prevCardio() {
  cardioIndex = (cardioIndex - 1 + cardioWorkouts.length) % cardioWorkouts.length;
  updateCardio();
}

function nextStrength() {
  strengthIndex = (strengthIndex + 1) % strengthWorkouts.length;
  updateStrength();
}

function prevStrength() {
  strengthIndex = (strengthIndex - 1 + strengthWorkouts.length) % strengthWorkouts.length;
  updateStrength();
}

window.onload = () => {
  updateCardio();
  updateStrength();
};