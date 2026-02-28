# Neon Survivor

A 2D survivor-style game built with **Godot 4** and **C#**.

## Project Structure

- `Scenes/` – game scenes (`Game.tscn`, `MainMenu.tscn`)
- `Scripts/` – gameplay and UI scripts (`Player.cs`, `Enemy.cs`, `Game.cs`, etc.)
- `project.godot` – Godot project configuration
- `2d-game.sln` / `2d-game.csproj` – C# solution and project files

## Requirements

- [Godot 4.x with .NET support](https://godotengine.org/download)
- .NET SDK (version compatible with your installed Godot .NET build)

## Run Locally

1. Open this folder in Godot.
2. Let Godot restore/generate C# build artifacts.
3. Press **F5** (Run Project).

## Build/Export

- Configure export templates in Godot.
- Use `Project -> Export` to build platform binaries.
- Existing export artifacts are intentionally ignored by git.

## Development Notes

- Core gameplay logic is in `Scripts/Game.cs`.
- Main menu flow is in `Scripts/MainMenu.cs`.
- Player, enemy, bullet, XP gem, and UI systems are split into dedicated scripts.

## Git

This repository is configured to use:

- Remote: `origin`
- URL: `https://github.com/zlip0/neon-survivor.git`
