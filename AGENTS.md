# Repository Guidelines

## Project Structure & Module Organization
- `EyeLog.sln` is the solution entry point.
- `EyeLog/` contains the single C# console project.
  - `EyeLog/Program.cs` hosts the main loops (input parsing and gaze tracking) and CLI flags.
  - `EyeLog/Bound.cs` defines area-of-interest bounds and hit testing.
  - `EyeLog/App.config` holds runtime configuration.
- The Tobii native wrapper is referenced at `tobii/lib/x64/tobii_interaction_lib_cs.dll` (path is resolved relative to the repo root). Ensure that path exists, or update the hint path if you build for a different architecture.

## Build, Test, and Development Commands
- Build in Visual Studio (recommended) with **x86** (the only solution configuration today). If you add x64 configs, update `EyeLog.sln` and the Tobii hint path.
- CLI build (requires VS Build Tools / MSBuild):
  - `msbuild EyeLog.sln /p:Configuration=Debug /p:Platform=x86`
- Run from the output directory:
  - `EyeLog\bin\x86\Debug\EyeLog.exe --raw --out=gaze_log.txt`

## Coding Style & Naming Conventions
- Follow the existing C# style in `EyeLog/Program.cs`: 4-space indentation, Allman braces, and explicit `private static` members.
- Naming: `PascalCase` for types/methods, `camelCase` for locals, and `SCREAMING_SNAKE_CASE` for static timeout constants.
- Keep console output formats stable (`enter:`, `exit`, `click:`) because downstream tooling may parse them.

## Testing Guidelines
- There are no automated tests in the repo today. If you add tests, create a separate test project (for example, `EyeLog.Tests/`) and name files `*Tests.cs`.
- Focus tests on parsing/validation and `Bound` hit detection; hardware-dependent logic should be covered by manual verification.

## Commit & Pull Request Guidelines
- Commit history is short and direct (e.g., `Update Program.cs`). Keep commit subjects concise and imperative; include details in the body when behavior changes.
- PRs should include: a brief description, how to verify (commands or manual steps), and any Tobii SDK/runtime requirements. Include sample output when changing log formats.

## Configuration & Dependencies
- Requires Tobii Interaction SDK plus the Visual C++ Redistributable (x64) as noted in `README.md`.
- Ensure Tobii services are running before manual testing.
