#Requires -Version 7.0
<#
.SYNOPSIS
    Builds the project and runs the test lanes the gdUnit4 adapter owns.
.DESCRIPTION
    Lane Unit        - test/Unit, the suites that cover the addon on its own.
    Lane Integration - test/Integration, one case per scene, driving the real demo scenes in the same host.

    The Godot binary is taken from `godot.path` (gitignored) by `test/godot-test.targets`, which also writes the
    `.runsettings` the adapter starts with, so no environment variable has to be set here. The build runs first because
    the engine loads the assembly from the project output folder and never builds it.
.EXAMPLE
    ./run-tests.ps1
.EXAMPLE
    ./run-tests.ps1 -Lane Integration
#>
[CmdletBinding()]
param(
    [ValidateSet('All', 'Unit', 'Integration')]
    [string] $Lane = 'All'
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$project = Join-Path $root 'VerletRope.csproj'

Push-Location $root

try {
    Write-Host '--- build ---'
    dotnet build $project --nologo -v:q -clp:ErrorsOnly

    if ($LASTEXITCODE -ne 0) {
        throw 'The project build failed.'
    }

    Write-Host 'build ok'

    $filter = switch ($Lane) {
        'Unit' { 'Trait.lane=unit' }
        'Integration' { 'Trait.lane=integration' }
        default { '' }
    }

    Write-Host "--- tests (lane $Lane) ---"

    $testLog = Join-Path $env:TEMP 'verlet-rope-tests.log'
    $testArguments = @('test', $project, '--no-build', '--logger', 'console;verbosity=normal')

    if ($filter) {
        $testArguments += @('--filter', $filter)
    }

    dotnet @testArguments *> $testLog
    $testExit = $LASTEXITCODE

    Get-Content -LiteralPath $testLog | Write-Host

    if ($testExit -ne 0) {
        Write-Host "FAILED: lane $Lane, log: $testLog"
        exit 1
    }

    Write-Host "PASSED: lane $Lane"
}
finally {
    Pop-Location
}
