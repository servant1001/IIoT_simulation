[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $Name,

    [Parameter(Mandatory)]
    [ValidateSet(1, 2, 3)]
    [int] $ProtocolType,

    [Parameter(Mandatory)]
    [ValidateRange(0, 22)]
    [int] $ActualFaultType,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $TargetHost,

    [Parameter(Mandatory)]
    [ValidateRange(1, 65535)]
    [int] $Port,

    [string] $DeviceName = "Experiment Target",
    [ValidateRange(1, 1000)]
    [int] $RepeatCount = 100,
    [ValidateRange(1, 86400)]
    [int] $DurationSeconds = 10,
    [ValidateRange(100, 60000)]
    [int] $PollingIntervalMs = 1000,
    [ValidateRange(1, 10000)]
    [int] $TagCount = 6,
    [string] $ApiBaseUrl = "http://127.0.0.1:5080"
)

$ErrorActionPreference = "Stop"

try {
    $health = Invoke-RestMethod "$ApiBaseUrl/health" -TimeoutSec 5
    if ($health -ne "Healthy") {
        throw "API health check did not return Healthy."
    }
}
catch {
    throw "Cannot reach the API at $ApiBaseUrl. Start Docker, apply migrations, then start the API. $($_.Exception.Message)"
}

$devices = @((Invoke-RestMethod "$ApiBaseUrl/api/devices") | ForEach-Object { $_ })
$target = $devices | Where-Object { $_.name -eq $DeviceName } | Select-Object -First 1
$targetBody = @{
    name = $DeviceName
    protocolType = $ProtocolType
    host = $TargetHost
    port = $Port
    isEnabled = $true
} | ConvertTo-Json

if ($null -eq $target) {
    $target = Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/api/devices" -ContentType "application/json" -Body $targetBody
}
else {
    $target = Invoke-RestMethod -Method Put -Uri "$ApiBaseUrl/api/devices/$($target.id)" -ContentType "application/json" -Body $targetBody
}

foreach ($device in $devices | Where-Object { $_.id -ne $target.id -and $_.isEnabled }) {
    $disableBody = @{
        name = $device.name
        protocolType = $device.protocolType
        host = $device.host
        port = $device.port
        isEnabled = $false
    } | ConvertTo-Json
    Invoke-RestMethod -Method Put -Uri "$ApiBaseUrl/api/devices/$($device.id)" -ContentType "application/json" -Body $disableBody | Out-Null
}

$experimentBody = @{
    name = $Name
    protocolType = $ProtocolType
    actualFaultType = $ActualFaultType
    deviceCount = 1
    tagCount = $TagCount
    pollingIntervalMs = $PollingIntervalMs
    durationSeconds = $DurationSeconds
    repeatCount = $RepeatCount
} | ConvertTo-Json

$experiment = Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/api/experiments" -ContentType "application/json" -Body $experimentBody
Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/api/experiments/$($experiment.id)/start" | Out-Null

[PSCustomObject]@{
    ExperimentId = $experiment.id
    Name = $experiment.name
    ProtocolType = $experiment.protocolType
    ActualFaultType = $experiment.actualFaultType
    RepeatCount = $experiment.repeatCount
    DurationSeconds = $experiment.durationSeconds
    RecordsUrl = "$ApiBaseUrl/api/experiments/$($experiment.id)/records"
    RunsUrl = "$ApiBaseUrl/api/experiments/$($experiment.id)/runs"
}
