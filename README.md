# Early Sleep Bot

Windows app that helps you **sleep earlier** by running a background “agent” and providing a small **Setup** UI to install/update it and configure rules.

## What’s in this repo

- **Agent** (`src/SleepEarlyBot/`): the background app that enforces the sleep policy.
- **Setup** (`src/SleepEarlyBotSetup/`): a WPF UI that installs the agent to the user profile and creates a Scheduled Task.
- **Shared** (`src/SleepEarlyBotShared/`): shared models/storage logic used by both Agent and Setup.

## Requirements (for building)

- Windows 10/11
- .NET SDK (the project targets `net10.0-windows`)

## Install (non-technical users)

- Download the latest `SleepEarlyBotSetup.exe` from the **GitHub Releases** page.
- Right-click and open with administrator it to run.
- Click **Install (Per-user)**.

This installs the agent to:

- `%LocalAppData%\SleepEarlyBot\app\SleepEarlyBot.exe`

…and creates a Scheduled Task named:

- `SleepEarlyBot Agent`

## Build / publish (technical users)

All commands below are meant to be run from the repo root in **PowerShell**.

### Build a single Setup EXE (recommended)

This produces **one file** that already contains the agent inside it.

```powershell
dotnet publish .\src\SleepEarlyBotSetup\SleepEarlyBotSetup.csproj -c Release -o .\dist\setup
```

Output:

- `dist\setup\SleepEarlyBotSetup.exe`

## Publish separate Setup + Agent (zip a folder)

If you prefer to ship them separately (e.g. for debugging), publish:

### Agent (single EXE)

```powershell
dotnet publish .\src\SleepEarlyBot\SleepEarlyBot.csproj -c Release -r win-x64 -o .\dist\agent `
  -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

### Setup (single EXE, without embedding)

```powershell
dotnet publish .\src\SleepEarlyBotSetup\SleepEarlyBotSetup.csproj -c Release -r win-x64 -o .\dist\setup `
  -p:SelfContained=true -p:PublishSingleFile=true -p:EmbedAgentPayload=false
```

Then zip and ship this layout:

```
MyRelease\
  SleepEarlyBotSetup.exe
  agent\
    SleepEarlyBot.exe
```

The Setup app will look for `agent\SleepEarlyBot.exe` next to itself.

## Troubleshooting

### “schtasks … Access is denied / 拒绝访问”

Some Windows environments block creating a per-user task with an explicit `/RU DOMAIN\User`.
The Setup app will automatically fall back to creating the task **without** `/RU` when it detects:

- `Access is denied`
- `拒绝访问` / `访问被拒绝`

If it still fails:

- Run Setup once with **Administrator** privileges, or
- Create the task manually in Task Scheduler (run on logon, run only when user is logged on).

### Publish fails because files are in use

If `dotnet publish` says the EXE is locked, close the running `SleepEarlyBotSetup.exe` and re-run publish.

## Icon

- Setup EXE icon: `src/SleepEarlyBotSetup/icon.ico`
