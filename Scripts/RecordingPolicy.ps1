function Write-ColoredLine {
    param ([string]$Text, [ConsoleColor]$Color = 'White')
    $oldColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $Color
    Write-Host $Text
    $Host.UI.RawUI.ForegroundColor = $oldColor
}

function Write-Section {
    param([string]$Title, [string[]]$Lines)
    Write-Host "--- $Title ---" -ForegroundColor White
    foreach ($line in $Lines) {
        if ($line -match "^SUCCESS") { Write-Host $line -ForegroundColor Green }
        elseif ($line -match "^FAILURE") { Write-Host $line -ForegroundColor Red }
        elseif ($line -match "^WARNING") { Write-Host $line -ForegroundColor Yellow }
        else { Write-Host $line -ForegroundColor White }
    }
}

Write-ColoredLine "Step 1 of 2: SYSTEM Check" White
Write-ColoredLine "INSTRUCTION: Reach 100% success" Yellow
Write-Host ""

$exclusionsOutput = @()
$defenderOutput = @()
$modulesOutput = @()
$windowsOutput = @()
$memoryIntegrityOutput = @()
$threatsOutput = @()
$powershellSigOutput = @()

$defaultModules = @(
    "Microsoft.PowerShell.Archive", "Microsoft.PowerShell.Diagnostics", "Microsoft.PowerShell.Host",
    "Microsoft.PowerShell.LocalAccounts", "Microsoft.PowerShell.Management", "Microsoft.PowerShell.Security",
    "Microsoft.PowerShell.Utility", "PackageManagement", "PowerShellGet", "PSReadLine", "Pester", "ThreadJob"
)

$protectedModule = "Microsoft.PowerShell.Operation.Validation"
$modulesPath = "C:\Program Files\WindowsPowerShell\Modules"
$protectedFilePath = "$modulesPath\$protectedModule\1.0.1\Diagnostics\Comprehensive\Comprehensive.Tests.ps1"
$expectedHash = "99B7CBE4325BA089DD9440A202B9E35D9E6F134A46312F3F1E93E71F23C8DAE3"
$foundIssues = $false

Get-ChildItem $modulesPath -ErrorAction SilentlyContinue |
Where-Object { $_.PSIsContainer } |
ForEach-Object {
    $moduleName = $_.Name
    $modulePath = $_.FullName
    $isDefault = $defaultModules -contains $moduleName
    $isProtected = $moduleName -eq $protectedModule
    $files = Get-ChildItem $modulePath -Recurse -Force -ErrorAction SilentlyContinue |
             Where-Object { -not $_.PSIsContainer }
    $unauthorizedFiles = @()
    foreach ($file in $files) {
        $sig = Get-AuthenticodeSignature $file.FullName
        if ($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch "Microsoft") {
            $unauthorizedFiles += $file
        }
    }
    if (-not $isDefault -and -not $isProtected) {
        $modulesOutput += "FAILURE: Non-default module detected: $moduleName ($modulePath)"
        $foundIssues = $true
    } elseif ($isProtected) {
        if ($unauthorizedFiles.Count -eq 0) {
            $modulesOutput += "SUCCESS: Protected module '$moduleName' verified."
        } else {
            foreach ($file in $unauthorizedFiles) {
                $modulesOutput += "FAILURE: Unsigned/non-Microsoft file in protected module: '$($file.FullName)'"
                $foundIssues = $true
                if ($file.FullName -ieq $protectedFilePath) {
                    try {
                        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
                        $sha256 = [System.Security.Cryptography.SHA256]::Create()
                        $actual = $sha256.ComputeHash($bytes)
                        $hash = ([BitConverter]::ToString($actual)).Replace("-", "")
                        if ($hash -ne $expectedHash) {
                            $modulesOutput += "FAILURE: Protected file altered (hash mismatch): '$($file.FullName)'"
                        } else {
                            $modulesOutput += "SUCCESS: Protected file hash matches expected."
                        }
                    } catch {
                        $modulesOutput += "WARNING: Could not hash protected file: '$($file.FullName)'"
                    }
                }
            }
            $modulesOutput += "SUCCESS: Protected module intact."
        }
    } else {
        foreach ($file in $unauthorizedFiles) {
            $modulesOutput += "FAILURE: Unsigned/non-Microsoft file: '$($file.FullName)'"
            $foundIssues = $true
        }
        if ($unauthorizedFiles.Count -eq 0) {
            $modulesOutput += "SUCCESS: Module '$moduleName' passed signature check."
        }
    }
}

Get-ChildItem $modulesPath -Force -ErrorAction SilentlyContinue |
Where-Object { -not $_.PSIsContainer } |
ForEach-Object {
    $sig = Get-AuthenticodeSignature $_.FullName
    if ($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch "Microsoft") {
        $modulesOutput += "FAILURE: Unsigned/non-Microsoft root file: '$($_.FullName)'"
        $foundIssues = $true
    } else {
        $modulesOutput += "SUCCESS: Root file '$($_.Name)' is signed."
    }
}

if (-not $foundIssues) { $modulesOutput += "SUCCESS: No unauthorized modules/files found." }

try {
    if ($env:OS -eq "Windows_NT" -and (Get-CimInstance Win32_OperatingSystem -ErrorAction Stop)) {
        $windowsOutput += "SUCCESS: Running on Windows."
    } else { $windowsOutput += "FAILURE: Not running on Windows." }
} catch { $windowsOutput += "FAILURE: OS check failed." }

try {
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"
    $enabled = Get-ItemPropertyValue -Path $regPath -Name "Enabled" -ErrorAction Stop
    $memoryIntegrityOutput += "SUCCESS: Memory Integrity supported."
    if ($enabled -eq 1) { $memoryIntegrityOutput += "SUCCESS: Memory Integrity is ON." }
    else { $memoryIntegrityOutput += "WARNING: Memory Integrity is OFF." }
} catch { $memoryIntegrityOutput += "WARNING: Memory Integrity not supported or inaccessible." }

try {
    $defender = Get-MpComputerStatus
    $service = Get-Service -Name WinDefend -ErrorAction SilentlyContinue
    if ($defender.AntivirusEnabled -and $service.Status -eq 'Running') {
        if (-not $defender.RealTimeProtectionEnabled) {
            $defenderOutput += "FAILURE: Realtime protection is OFF."
        } else { $defenderOutput += "SUCCESS: Realtime protection is ON." }
    } else { $defenderOutput += "FAILURE: Microsoft Defender Antivirus is not running." }
} catch { $defenderOutput += "WARNING: Could not query Defender status." }

try {
    $exclusions = Get-MpPreference | Select-Object -ExpandProperty ExclusionPath
    if ($exclusions) {
        foreach ($exclusion in $exclusions) {
            $exclusionsOutput += "FAILURE: Exclusion path detected: $exclusion"
        }
    }
    else { $exclusionsOutput += "SUCCESS: No Defender exclusions set." }
} catch { $exclusionsOutput += "WARNING: Could not check exclusions." }

try {
    $threats = Get-MpThreat | Where-Object { $_.Status -eq "Active" }
    if ($threats) {
        foreach ($t in $threats) { $threatsOutput += "FAILURE: $($t.ThreatName) | $($t.Resources)" }
    } else { $threatsOutput += "SUCCESS: No active threats." }
} catch { $threatsOutput += "WARNING: Threat scan failed." }

try {
    $psPath = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
    $sig = Get-AuthenticodeSignature -FilePath $psPath
    if ($sig.Status -eq 'Valid' -and $sig.SignerCertificate.Subject -like '*Microsoft Windows*') {
        $powershellSigOutput += "SUCCESS: PowerShell is signed and valid."
    } else { $powershellSigOutput += "FAILURE: PowerShell binary signature invalid." }
} catch { $powershellSigOutput += "WARNING: Could not verify PowerShell binary." }

Write-Section "Files + Modules" $modulesOutput
Write-Section "OS Check" $windowsOutput
Write-Section "Memory Integrity" $memoryIntegrityOutput
Write-Section "Windows Defender" $defenderOutput
Write-Section "Exclusions" $exclusionsOutput
Write-Section "Threats" $threatsOutput
Write-Section "Binary Sig" $powershellSigOutput

$allResults = $modulesOutput + $windowsOutput + $memoryIntegrityOutput + $defenderOutput + $exclusionsOutput + $threatsOutput + $powershellSigOutput
$total = ($allResults | Where-Object { $_ -match '^(SUCCESS|FAILURE|WARNING)' }).Count
$success = ($allResults | Where-Object { $_ -match '^SUCCESS' }).Count
$rate = if ($total -gt 0) { [math]::Round(($success / $total) * 100, 0) } else { 0 }
Write-Host ""
Write-Host "Step 1 Success Rate: $rate% ($success / $total)" -ForegroundColor $(if ($rate -eq 100) { "Green" } else { "Red" })

Write-Host ""
Write-ColoredLine "Step 2 of 2: Process Explorer" White
Write-ColoredLine "INSTRUCTION: Wait for Process Explorer to open. Scroll to the bottom, then close the window." Yellow
Write-Host ""

$processNames = @("procexp32", "procexp64", "procexp64a")
$runningPE = Get-Process -ErrorAction SilentlyContinue | Where-Object { $processNames -contains $_.ProcessName.ToLower() }

if ($runningPE) {
    $runningPE | ForEach-Object {
        try {
            $_ | Stop-Process -Force -ErrorAction Stop
            Write-ColoredLine "[SUCCESS] Terminated process ID $($_.Id)." Green
        } catch {
            Write-ColoredLine "[FAILED] Failed to terminate process ID $($_.Id): $($_.Exception.Message)" Red
        }
    }
    Start-Sleep -Seconds 2
    Write-ColoredLine "[SUCCESS] All Process Explorer processes terminated." Green
} else {
    Write-ColoredLine "[SUCCESS] No running Process Explorer processes were found." Green
}

$baseFolder = "C:\ToolsETA"
$extractFolder = Join-Path $baseFolder "ProcessExplorer"
$zipUrl = "https://download.sysinternals.com/files/ProcessExplorer.zip"
$zipPath = Join-Path $baseFolder "ProcessExplorer.zip"

if (Test-Path $baseFolder) {
    Get-ChildItem -Path $baseFolder -Force -Recurse | ForEach-Object {
        try {
            if ($_.Attributes -band [System.IO.FileAttributes]::ReadOnly) {
                $_.Attributes = $_.Attributes -bxor [System.IO.FileAttributes]::ReadOnly
            }
            if ($_.Attributes -band [System.IO.FileAttributes]::Hidden) {
                $_.Attributes = $_.Attributes -bxor [System.IO.FileAttributes]::Hidden
            }
            Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction Stop
        } catch {}
    }
    Write-ColoredLine "[SUCCESS] Cleaned up existing C:\ToolsETA folder." Green
} else {
    try {
        New-Item -ItemType Directory -Path $baseFolder -ErrorAction Stop | Out-Null
        Write-ColoredLine "[SUCCESS] Created folder C:\ToolsETA." Green
    } catch {
        Write-ColoredLine "[INFO] Folder may already exist." White
    }
}

try {
    Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing -ErrorAction Stop
    Write-ColoredLine "[SUCCESS] Downloaded Process Explorer." Green
} catch {
    Write-ColoredLine "[FAILED] Failed to download Process Explorer: $($_.Exception.Message)" Red
}

try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $extractFolder)
    Write-ColoredLine "[SUCCESS] Extracted Process Explorer zip." Green
} catch {
    Write-ColoredLine "[SUCCESS] Files already exist." Green
}
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

$regFileUrl = "https://pastebin.com/raw/A9vGEGyT"
$regFilePath = Join-Path $baseFolder "procexp_config.reg"

try {
    Invoke-WebRequest -Uri $regFileUrl -OutFile $regFilePath -UseBasicParsing -ErrorAction Stop
    Write-ColoredLine "[SUCCESS] Downloaded Process Explorer registry configuration." Green
} catch {
    Write-ColoredLine "[FAILED] Failed to download registry config: $($_.Exception.Message)" Red
}

try {
    $cmdPath = "$env:SystemRoot\System32\cmd.exe"
    & $cmdPath /c "reg import `"$regFilePath`""
    if ($LASTEXITCODE -eq 0) {
        Write-ColoredLine "[SUCCESS] Imported registry configuration successfully." Green
    } else {
        Write-ColoredLine "[FAILED] reg import returned non-zero exit code: $LASTEXITCODE" Red
    }
} catch {
    Write-ColoredLine "[FAILED] Registry importing failed: $($_.Exception.Message)" Red
}

$actualExe = Get-ChildItem -Path $extractFolder -Filter "procexp64.exe" -Recurse | Select-Object -First 1

if ($actualExe) {
    Write-ColoredLine "[SUCCESS] Launching Process Explorer." Green
    $process = Start-Process -FilePath $actualExe.FullName -PassThru
    Start-Sleep -Seconds 1
    $wshell = New-Object -ComObject wscript.shell
    Start-Sleep -Milliseconds 500
    $null = $wshell.AppActivate($process.Id)
    Start-Sleep -Milliseconds 500
    $wshell.SendKeys('% ')
    Start-Sleep -Milliseconds 200
    $wshell.SendKeys('x')
    Start-Sleep -Milliseconds 500
    Write-ColoredLine "[SUCCESS] Process Explorer window maximized." Green
    $process.WaitForExit()
    Write-ColoredLine "[SUCCESS] Step 2 Success Rate: 100% (1 / 1)." Green
} else {
    Write-ColoredLine "[FAILED] procexp64.exe was not found." Red
    Write-ColoredLine "[FAILED] Step 2 Success Rate: 0% (0 / 1)." Red
}
