# Find-AdlPackageDll v1.0.0 - 2026-10-29 12:00
# Read-only. Prints the path of the atiadlxx.dll that ships in the driver package bound to a PCI device,
# i.e. the DLL to pass to adl_pmlog_probe.exe. Does not need administrator rights and does not copy anything.
# Usage: .\Find-AdlPackageDll_v1.0.0_20261029-1200.ps1 [-DeviceId DEV_66A1]
param([string]$DeviceId = 'DEV_66A1')   # 66A1 = Vega 20 / MI50
$ErrorActionPreference = 'Stop'
$dev = Get-PnpDevice -PresentOnly | Where-Object { $_.InstanceId -like "PCI\VEN_1002&$DeviceId*" } | Select-Object -First 1
if (-not $dev) { Write-Error "No present PCI device matching VEN_1002&$DeviceId"; exit 1 }
Write-Host "Device : $($dev.FriendlyName)  [$($dev.InstanceId)]"
# Device instance -> driver key -> installed INF (oemNN.inf)
$drv = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Enum\$($dev.InstanceId)").Driver
$cls = Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$drv"
$oem = $cls.InfPath
Write-Host "Driver : $($cls.DriverDesc) $($cls.DriverVersion)  ($oem)"
# oemNN.inf -> original INF name via pnputil (works without elevation), then the DriverStore folder.
$orig = $null; $pub = $null
foreach ($line in (pnputil /enum-drivers)) {
  if ($line -match '^\s*(Published Name|Nome pubblicato)\s*:\s*(\S+)') { $pub = $Matches[2] }
  elseif ($pub -eq $oem -and $line -match '^\s*(Original Name|Nome originale)\s*:\s*(\S+)') { $orig = $Matches[2]; break }
}
if (-not $orig) { Write-Error "Could not map $oem to its original INF with pnputil (non-English Windows? check 'pnputil /enum-drivers')"; exit 1 }
$store = Get-ChildItem "$env:windir\System32\DriverStore\FileRepository" -Directory -Filter "$orig`_*" | Select-Object -First 1
if (-not $store) { Write-Error "DriverStore folder for $orig not found"; exit 1 }
$dll = Get-ChildItem $store.FullName -Recurse -Filter atiadlxx.dll | Select-Object -First 1
if (-not $dll) { Write-Error "atiadlxx.dll not found under $($store.FullName)"; exit 1 }
$ver = $dll.VersionInfo.FileVersion
Write-Host "ADL DLL: $($dll.FullName)  (file version $ver)"
$dll.FullName
