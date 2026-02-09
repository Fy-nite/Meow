#!/usr/bin/env pwsh
Set-StrictMode -Version Latest

# Test harness: build sample projects under testprojs using the locally-built Meow CLI
# Location: run this script from the repository root (it will work when executed directly).

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$meowDll = Join-Path $scriptRoot "src\Meow.CLI\bin\Debug\net9.0\meow.dll"
$projectsRoot = Join-Path $scriptRoot "testprojs"

if (-not (Test-Path $projectsRoot)) {
    Write-Error "testprojs directory not found: $projectsRoot"
    exit 2
}

# Ensure CLI is built
if (-not (Test-Path $meowDll)) {
    Write-Host "Meow CLI DLL not found; building Meow.CLI project..."
    dotnet build "$scriptRoot\src\Meow.CLI" -c Debug
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build Meow.CLI"
        exit 3
    }
}

$results = @()

Get-ChildItem -Path $projectsRoot -Directory | ForEach-Object {
    $projName = $_.Name
    $projPath = $_.FullName
    Write-Host "`n === Building project: $projName ==="
    Push-Location $projPath
    try {
        # Backup meow.yaml and enable linking for this test run
        $yamlPath = Join-Path $projPath "meow.yaml"
        $bakPath = Join-Path $projPath "meow.yaml.bak"
        if (Test-Path $yamlPath) {
            Copy-Item -Path $yamlPath -Destination $bakPath -Force
            (Get-Content $yamlPath) -replace '(^\s*link:\s*)(false)', '${1}true' | Set-Content $yamlPath
        }

        $cmd = @($meowDll, 'build')
        Write-Host "Running: dotnet $($cmd -join ' ') (cwd=$projPath)"
        $output = dotnet $meowDll build 2>&1
        $exit = $LASTEXITCODE
        if ($exit -eq 0) {
            Write-Host "[OK] Build succeeded for $projName"
        } else {
            Write-Host "[FAIL] Build failed for $projName (exit $exit)"
        }
        Write-Host $output

        # If linking was enabled, attempt to run the produced executable
        if (Test-Path $yamlPath) {
            $yamlText = Get-Content $yamlPath -Raw
            $projNameYaml = $projName
            if ($yamlText -match '^name:\s*(\S+)') { $projNameYaml = $Matches[1] }
            $outDir = 'build'
            if ($yamlText -match '^\s*output:\s*(\S+)') { $outDir = $Matches[1] }
            $compiler = ''
            if ($yamlText -match '^\s*compiler:\s*(\S+)') { $compiler = $Matches[1] }
            $linkFlag = 'false'
            if ($yamlText -match '^\s*link:\s*(\S+)') { $linkFlag = $Matches[1] }
            if ($linkFlag -eq 'true') {
                $outPath = Join-Path $projPath $outDir
                if (Test-Path $outPath) {
                    $candidates = Get-ChildItem -Path $outPath -File -Recurse | Where-Object { $_.BaseName -eq $projNameYaml -or $_.Name -eq "$projNameYaml.exe" }
                    $found = $candidates | Select-Object -First 1
                    if ($null -ne $found) {
                        $foundPath = $found.FullName
                        Write-Host "Running linked executable: $foundPath"
                        try {
                            $runOut = & $foundPath 2>&1
                            Write-Host "Executable output:`n$runOut"
                        }
                        catch {
                            Write-Host "Error running executable: $_"
                        }
                    }
                    else {
                        Write-Host "No linked executable found under: $outPath for name $projNameYaml"
                    }
                } else {
                    Write-Host "Output directory not found: $outPath"
                }
            }
        }

        $results += [pscustomobject]@{ Project = $projName; ExitCode = $exit; Output = $output }
    }
    finally {
        # Restore original meow.yaml if we backed it up
        if (Test-Path $bakPath) {
            try { Move-Item -Path $bakPath -Destination $yamlPath -Force } catch { }
        }
        Pop-Location
    }
}

Write-Host "`n === Summary ==="
$failures = @($results | Where-Object { $_.ExitCode -ne 0 })
foreach ($r in $results) {
    $status = if ($r.ExitCode -eq 0) { 'OK' } else { 'FAIL' }
    Write-Host "$($r.Project): $status (exit $($r.ExitCode))"
}

if ($null -ne $failures -and $failures.Count -gt 0) {
    Write-Error "Some projects failed. See details above."
    exit 1
} else {
    Write-Host "All sample projects built successfully." -ForegroundColor Green
    exit 0
}
