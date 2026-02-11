# scripts\uninstall_task.ps1
$ErrorActionPreference = "Stop"

$taskName = "SleepEarlyBot Agent"
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
Write-Host "Uninstalled scheduled task: $taskName"
