# 早睡机器人（Sleep Early Bot）

**语言**：简体中文 | [English](README.md)

这是一个 Windows 应用，通过后台运行的“Agent（代理程序）”配合一个“Setup（安装/配置界面）”来帮助你 **更早睡觉**、减少熬夜使用电脑。

## 你可以用它做什么？

- **设置每日截止时间**（例如 02:00）：到点后开始严格执行。
- **提前弹出提醒**：在截止时间前若干分钟提醒（可配置）。
- **设置限制时间段**（例如 02:00–08:00）：在该时间段内更严格。
- **受控的“延长/覆盖”机制**：需要填写达到最小长度的理由 + 承诺短语，并可选按周限制次数。
- **安装一次自动运行**：Setup 会把 Agent 安装到当前用户目录并创建 **计划任务**，在登录后自动启动。
- **可视化修改配置**：通过 Setup 界面修改规则，无需手动编辑配置文件。

## 仓库结构

- **Agent**（`src/SleepEarlyBot/`）：后台执行与限制逻辑。
- **Setup**（`src/SleepEarlyBotSetup/`）：WPF 图形界面，用于安装/卸载 Agent、创建计划任务、修改配置。
- **Shared**（`src/SleepEarlyBotShared/`）：Agent 与 Setup 共用的数据模型与存储逻辑。

## 构建环境（仅开发者需要）

- Windows 10/11
- .NET SDK（项目目标为 `net10.0-windows`）

## 安装使用（非技术用户）

- 从 GitHub 的 **Releases** 页面下载最新的 `SleepEarlyBotSetup.exe`
- 右键以管理员身份运行（某些系统创建计划任务时需要）
- 在界面中点击 **Install (Per-user)**

它会把 Agent 安装到：

- `%LocalAppData%\SleepEarlyBot\app\SleepEarlyBot.exe`

并创建计划任务：

- `SleepEarlyBot Agent`

## 构建 / 发布（技术用户）

以下命令在仓库根目录的 **PowerShell** 中执行。

### 发布单文件 Setup（推荐）

生成 **一个 EXE**（内置 Agent，最适合上传到 Releases 给普通用户）：

```powershell
dotnet publish .\src\SleepEarlyBotSetup\SleepEarlyBotSetup.csproj -c Release -o .\dist\setup
```

输出：

- `dist\setup\SleepEarlyBotSetup.exe`

### 分开发布 Setup + Agent（打包成 zip 目录）

#### Agent（单文件 EXE）

```powershell
dotnet publish .\src\SleepEarlyBot\SleepEarlyBot.csproj -c Release -r win-x64 -o .\dist\agent `
  -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

#### Setup（单文件 EXE，不内置 Agent）

```powershell
dotnet publish .\src\SleepEarlyBotSetup\SleepEarlyBotSetup.csproj -c Release -r win-x64 -o .\dist\setup `
  -p:SelfContained=true -p:PublishSingleFile=true -p:EmbedAgentPayload=false
```

然后按如下结构打包并发布（例如 zip）：

```
MyRelease\
  SleepEarlyBotSetup.exe
  agent\
    SleepEarlyBot.exe
```

Setup 会在自身旁边查找 `agent\SleepEarlyBot.exe`。

## 常见问题

### “schtasks … Access is denied / 拒绝访问”

某些 Windows 环境不允许使用显式的 `/RU 域\用户名` 来创建计划任务。Setup 会在检测到以下提示时自动尝试不带 `/RU` 的创建方式：

- `Access is denied`
- `拒绝访问` / `访问被拒绝`

如果仍失败：

- 尝试 **以管理员身份运行** Setup，或
- 在“任务计划程序”中手动创建一个“登录时运行”的任务（仅在用户登录时运行）。

### 发布失败：文件被占用

如果 `dotnet publish` 提示 EXE 被占用，请先关闭正在运行的 `SleepEarlyBotSetup.exe`，再重新发布。

## 图标

- 原始图片：`src/Images/minecraft_bed.png`
- Setup EXE 图标：`src/SleepEarlyBotSetup/icon.ico`

