# scripts\install_task.ps1
$ErrorActionPreference = "Stop"

$taskName = "SleepEarlyBot Agent"
$exePath  = "C:\Program Files\SleepEarlyBot\SleepEarlyBot.exe"

if (-not (Test-Path $exePath)) {
    throw "SleepEarlyBot.exe not found at: $exePath"
}

# Remove existing task if present
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue | Out-Null

# Create action: run exe with args
$action = New-ScheduledTaskAction -Execute $exePath -Argument "--agent"

# Trigger: at logon of current user
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME

# Run with highest privileges
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -RunLevel Highest

# Basic settings (optional but recommended)
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable

Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings | Out-Null

Write-Host "Installed scheduled task: $taskName"
Write-Host "Action: $exePath --agent"
Write-Host "Trigger: At log on ($env:USERNAME)"
