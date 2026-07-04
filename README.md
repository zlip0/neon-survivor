# Neon Survivor

A minimalist neon arena-survival game built with **Godot 4.6 (.NET/C#)**. Move through a glowing grid, automatically fire at the nearest enemy, collect XP gems, and choose upgrades to survive escalating waves for as long as possible.

## Gameplay

- **Move:** `WASD` or arrow keys
- **Start / restart:** `Enter` or `Space`
- **Attack:** auto-aim and auto-fire target the nearest enemy
- **Level up:** collect XP gems and choose one of three upgrades with the mouse or number keys `1`, `2`, `3`

## Features

- Four enemy types with different movement, health, damage, and XP rewards:
  - Chaser
  - Sprinter
  - Drifter
  - Tank
- XP, leveling, score, kill counter, and survival timer
- Upgrade system for multishot, fire rate, bullet size, damage, movement speed, max HP, regeneration, magnet radius, and piercing
- Neon-style procedural drawing for the player, enemies, bullets, gems, HUD, explosions, starfields, and grid background
- Increasing difficulty over time with faster and larger enemy waves

## Requirements

- [Godot 4.6 with .NET support](https://godotengine.org/download)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Run Locally

1. Open this folder in Godot.
2. Allow Godot to restore/build the C# project if prompted.
3. Press **F5** or click **Run Project**.
4. From the main menu, press **Enter** or **Space** to start.

## Build / Export

1. Install Godot export templates for your Godot version.
2. Open **Project -> Export**.
3. Select or configure a platform preset.
4. Export the project.

Generated exports and Godot/.NET build artifacts are ignored by git.
