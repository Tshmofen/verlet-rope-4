#Requires -Version 7.0
<#
.SYNOPSIS
    Builds the project and runs both test lanes of the addon.
.DESCRIPTION
    Lane Unit   - the gdUnit4 unit tests, run through `dotnet test` and hosted by Godot.
    Lane Scenes - the scene integration runs, driven headless by `test/Scenes/scene_runner.tscn`.

    Godot is taken from -Godot, or from `$env:GODOT_BIN` when the parameter is omitted. The build runs first because the
    engine loads the assembly from the project's output folder and never builds it itself.
.EXAMPLE
    ./run-tests.ps1
.EXAMPLE
    ./run-tests.ps1 -Lane Scenes
.EXAMPLE
    ./run-tests.ps1 -Godot 'C:\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
#>
[CmdletBinding()]
param(
    [ValidateSet('All', 'Unit', 'Scenes')]
    [string] $Lane = 'All',

    [string] $Godot = $env:GODOT_BIN
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$project = Join-Path $root 'VerletRope.csproj'
$runSettings = Join-Path $root '.runsettings'
$sceneRunner = 'res://test/Scenes/scene_runner.tscn'

if ([string]::IsNullOrWhiteSpace($Godot)) {
    throw 'Godot was not found. Pass -Godot <path to Godot_*_mono_win64_console.exe>, or set GODOT_BIN.'
}

if (-not (Test-Path -LiteralPath $Godot)) {
    throw "The Godot executable '$Godot' does not exist."
}

Push-Location $root

try {
    Write-Host '--- build ---'
    dotnet build $project --nologo -v:q -clp:ErrorsOnly

    if ($LASTEXITCODE -ne 0) {
        throw 'The project build failed.'
    }

    Write-Host 'build ok'
    $failedLanes = 0

    if ($Lane -ne 'Scenes') {
        Write-Host '--- unit tests (gdUnit4) ---'
        $env:GODOT_BIN = $Godot

        $unitLog = Join-Path $env:TEMP 'verlet-rope-unit-tests.log'
        dotnet test $project --no-build --settings $runSettings --logger 'console;verbosity=normal' *> $unitLog

        # The adapter rebuilds the Godot project before a run. That engine process dies with an access violation on
        # shutdown now and then (measured on 4.7.2, roughly one run in five), and the adapter then reports every test as
        # "not executed". Nothing ran, so one retry is the whole fix - a real failure reports its own test results.
        $nothingRan = Select-String -Path $unitLog -Pattern 'not executed, the test engine reported no result' -Quiet
        $rebuildCrashed = Select-String -Path $unitLog -Pattern 'Rebuilding Godot Project ends with exit code: [1-9-]' -Quiet

        if ($LASTEXITCODE -ne 0 -and $nothingRan -and $rebuildCrashed) {
            Write-Host 'the test host crashed while rebuilding the Godot project, retrying once'
            dotnet test $project --no-build --settings $runSettings --logger 'console;verbosity=normal' *> $unitLog
        }

        Get-Content -LiteralPath $unitLog | Write-Host

        if ($LASTEXITCODE -ne 0) {
            $failedLanes++
            Write-Host "unit tests failed, log: $unitLog"
        }
    }

    if ($Lane -ne 'Unit') {
        Write-Host '--- scene tests (headless) ---'

        $sceneLog = Join-Path $env:TEMP 'verlet-rope-scene-tests.log'
        & $Godot --headless --path $root $sceneRunner *> $sceneLog

        # The same engine crash that hits the unit lane can take this run down before it reports a single scenario. Nothing
        # was verified in that case, so it is retried once instead of being read as a scene that failed its goals.
        $reportedNothing = -not (Select-String -Path $sceneLog -Pattern '\[scenes\]' -Quiet)

        if ($LASTEXITCODE -ne 0 -and $reportedNothing) {
            Write-Host 'the scene run died before it reported a scenario, retrying once'
            & $Godot --headless --path $root $sceneRunner *> $sceneLog
        }

        Get-Content -LiteralPath $sceneLog | Write-Host

        if ($LASTEXITCODE -ne 0) {
            $failedLanes++
            Write-Host "scene tests failed, log: $sceneLog"
        }
    }

    if ($failedLanes -gt 0) {
        Write-Host "FAILED: $failedLanes test lane(s)"
        exit 1
    }

    Write-Host 'PASSED: all test lanes'
}
finally {
    Pop-Location
}
