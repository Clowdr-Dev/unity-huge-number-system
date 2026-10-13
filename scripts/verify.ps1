[CmdletBinding()]
param(
    [string]$UnityPath = $env:UNITY_EDITOR_PATH,
    [ValidateSet('Both', 'EditMode', 'PlayMode')]
    [string]$TestPlatform = 'Both',
    [ValidateRange(1, 180)]
    [int]$TimeoutMinutes = 15,
    [string]$OutputDirectory = 'artifacts/verification'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$taskExitCode = 0
$taskRunDirectory = $null
$taskRuns = @()
$taskFailure = $null

try {
    $taskProjectPath = Split-Path -Parent $PSScriptRoot
    $taskVersionFile = Join-Path $taskProjectPath 'ProjectSettings/ProjectVersion.txt'
    $taskVersionMatch = Select-String -LiteralPath $taskVersionFile -Pattern '^m_EditorVersion: (.+)$'
    if ($null -eq $taskVersionMatch) {
        throw 'ProjectSettings/ProjectVersion.txt does not declare a Unity Editor version.'
    }
    $taskUnityVersion = $taskVersionMatch.Matches[0].Groups[1].Value.Trim()

    if ([string]::IsNullOrWhiteSpace($UnityPath)) {
        if ($env:OS -eq 'Windows_NT') {
            $taskHubRoot = Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'Unity/Hub/Editor'
            $UnityPath = Join-Path $taskHubRoot "$taskUnityVersion/Editor/Unity.exe"
        }
        else {
            $UnityPath = "/Applications/Unity/Hub/Editor/$taskUnityVersion/Unity.app/Contents/MacOS/Unity"
        }
    }
    if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
        throw "Unity Editor not found at '$UnityPath'. Install $taskUnityVersion or supply -UnityPath / UNITY_EDITOR_PATH."
    }
    $UnityPath = (Resolve-Path -LiteralPath $UnityPath).Path

    $taskOutputRoot = if ([IO.Path]::IsPathRooted($OutputDirectory)) {
        $OutputDirectory
    }
    else {
        Join-Path $taskProjectPath $OutputDirectory
    }
    $taskRunId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $taskRunDirectory = Join-Path $taskOutputRoot $taskRunId
    New-Item -ItemType Directory -Path $taskRunDirectory -Force | Out-Null
    $taskRunDirectory = (Resolve-Path -LiteralPath $taskRunDirectory).Path

    $taskPlatforms = if ($TestPlatform -eq 'Both') { @('EditMode', 'PlayMode') } else { @($TestPlatform) }
    foreach ($taskPlatform in $taskPlatforms) {
        $taskStem = $taskPlatform.ToLowerInvariant()
        $taskResultsPath = Join-Path $taskRunDirectory "$taskStem.xml"
        $taskLogPath = Join-Path $taskRunDirectory "$taskStem.log"
        $taskFilter = if ($taskPlatform -eq 'PlayMode') { 'HugeNumberSystem.Numerics.Player' } else { 'HugeNumberSystem.Numerics' }
        $taskArguments = @(
            '-batchmode', '-nographics',
            '-projectPath', $taskProjectPath,
            '-runTests', '-testPlatform', $taskPlatform,
            '-testFilter', $taskFilter,
            '-testResults', $taskResultsPath,
            '-logFile', $taskLogPath
        )
        # Start-Process joins ArgumentList; quote each argument so paths with spaces survive.
        $taskQuotedArguments = ($taskArguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }) -join ' '
        $taskLaunchOptions = @{
            FilePath = $UnityPath
            ArgumentList = $taskQuotedArguments
            WorkingDirectory = $taskProjectPath
            PassThru = $true
        }
        if ($env:OS -eq 'Windows_NT') {
            $taskLaunchOptions.WindowStyle = 'Hidden'
        }

        Write-Host "Running $taskPlatform tests with Unity $taskUnityVersion..."
        $taskProcess = Start-Process @taskLaunchOptions
        if (-not $taskProcess.WaitForExit($TimeoutMinutes * 60000)) {
            $taskProcess.Kill()
            $taskProcess.WaitForExit()
            throw "$taskPlatform exceeded $TimeoutMinutes minutes. Log: $taskLogPath"
        }
        $taskProcess.WaitForExit()
        if (-not (Test-Path -LiteralPath $taskResultsPath -PathType Leaf)) {
            throw "$taskPlatform did not produce test results (Unity exit code $($taskProcess.ExitCode)). Close this project in Unity before running batch tests. Log: $taskLogPath"
        }

        $taskXml = New-Object System.Xml.XmlDocument
        $taskXml.XmlResolver = $null
        $taskXml.Load($taskResultsPath)
        $taskRoot = $taskXml.DocumentElement
        if ($taskRoot.Name -ne 'test-run') {
            throw "$taskPlatform produced an unexpected test results format: $taskResultsPath"
        }
        $taskTotal = [int]$taskRoot.GetAttribute('total')
        $taskPassed = [int]$taskRoot.GetAttribute('passed')
        $taskFailed = [int]$taskRoot.GetAttribute('failed')
        $taskSkipped = [int]$taskRoot.GetAttribute('skipped')
        $taskResult = $taskRoot.GetAttribute('result')
        $taskRuns += [pscustomobject]@{
            platform = $taskPlatform
            result = $taskResult
            total = $taskTotal
            passed = $taskPassed
            failed = $taskFailed
            skipped = $taskSkipped
            results = $taskResultsPath
            log = $taskLogPath
        }
        if ($taskResult -ne 'Passed' -or $taskTotal -le 0 -or $taskFailed -ne 0 -or $taskSkipped -ne 0 -or $taskPassed -ne $taskTotal) {
            throw "$taskPlatform verification failed: $taskPassed/$taskTotal passed, $taskFailed failed, $taskSkipped skipped. Results: $taskResultsPath"
        }
        if ($taskProcess.ExitCode -ne 0) {
            throw "$taskPlatform Unity process exited with code $($taskProcess.ExitCode). Log: $taskLogPath"
        }
        Write-Host "$taskPlatform passed: $taskPassed/$taskTotal tests."
    }
}
catch {
    $taskExitCode = 1
    $taskFailure = $_.Exception.Message
    Write-Error -Message $taskFailure -ErrorAction Continue
}
finally {
    if ($null -ne $taskRunDirectory) {
        [pscustomobject]@{
            success = ($taskExitCode -eq 0)
            failure = $taskFailure
            unity = $UnityPath
            project = $taskProjectPath
            runs = @($taskRuns)
        } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskRunDirectory 'summary.json') -Encoding UTF8
        Write-Host "Verification artifacts: $taskRunDirectory"
    }
}

exit $taskExitCode
