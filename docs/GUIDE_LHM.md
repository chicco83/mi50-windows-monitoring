# Guide: see the MI50 sensors in LibreHardwareMonitor (LHM)

Official LibreHardwareMonitor releases **do not show the MI50 yet**. The fix is in pull request
[LibreHardwareMonitor#2597](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/pull/2597) (open at time of writing).
Until it is merged and released you build LHM from my branch. It takes a few minutes and was verified from a clean clone (see "Verified" below).

## 0. Prerequisites
Do the [Prerequisites in the README](../README.md#prerequisites) first: driver installed and bound to "AMD Radeon Pro VII".
Check: Device Manager shows "AMD Radeon Pro VII" without a warning, and Task Manager → Performance shows the GPU temperature.

## 1. Tools you need
- Windows 10/11 x64.
- [.NET SDK 8 or newer](https://dotnet.microsoft.com/download) (I used 10.0.403) and Git.
- Nothing from AMD besides the driver you already installed. The fix uses the `atiadlxx.dll` that your MI50 driver package already left in the DriverStore.

## 2. Build
```powershell
git -c core.longpaths=true clone --branch amd-mi50-support https://github.com/chicco83/LibreHardwareMonitor.git
cd LibreHardwareMonitor
dotnet build LibreHardwareMonitor.Windows.Forms\LibreHardwareMonitor.Windows.Forms.csproj -c Release -f net472 -p:Platform=x64 -p:CsWin32PlatformTarget=x64
```
Use a **short folder** (for example `C:\w`): Windows path limits can break the clone. The two `-p:` options are needed, otherwise the build stops with `PInvoke005`.
Result: `bin\Release\net472\LibreHardwareMonitor.Windows.Forms.exe`.

## 3. Run
1. Right-click the exe → **Run as administrator** (LHM needs it for other hardware; the GPU part may work without).
2. In the tree, open **AMD Radeon Pro VII**. You should see about 41 sensors, including the ones below.
3. Optional: *Options → Remote Web Server → Run* serves the same values as JSON at `http://localhost:8085/data.json` for your own tools.

## What you should see
| Group | Sensors (names as LHM shows them) |
|---|---|
| Temperatures | GPU Core, GPU Memory (HBM), GPU VR VDDC, GPU Hot Spot |
| Power | GPU Package (watts) |
| Voltages | GPU Core, GPU SoC, GPU Memory |
| Clocks | GPU Core, GPU SoC, GPU Memory |
| Load | GPU Core, GPU Memory, plus the D3D engine loads |
| Fan | GPU Fan (0 on this card: the fan is not the card's own) |

The values come from the ADL PMLog of your card's own driver package. Idle reference on my card: 36 °C core, 13 W, 28 MHz core.
Under a synthetic full load: 100 %, 1687 MHz, 0.98 V, about 170 W (details in [RESULTS.md](RESULTS.md)).

## If you do not see the card, or only a few sensors
- Run the read-only probe from the README quick start first. If it prints 21 PMLog values, the driver side is fine and the problem is the LHM build.
- If the probe cannot find the DLL, the Radeon Pro VII package is not installed or not bound (see prerequisites).
- LHM can write a report: *File → Save Report*. Look for lines starting with `Driver package ADL:` and `Driver package PMLog`.
- Open an issue here with the report text and the probe output (remove anything personal).

## How the fix works (short)
LHM asks the System32 `atiadlxx.dll` for the card's sensors and gets `-5` (invalid adapter). The patch finds the DLL of the card's own
driver package (registry → INF → DriverStore), loads it, matches the adapter by its PNP id and uses it only for PMLog. It also adds a WDDM
temperature fallback, the same source Task Manager uses.

## Verified
Built from a fresh shallow clone of `amd-mi50-support` on 2026-10-29 (0 errors); that build exposed the same 41 sensors on the MI50.
Not verified: other MI50 setups, 32-bit processes, long runs. The GUI exe itself was built but I only exercised the library with my own logger tool.

## When the PR is merged
Use the official LHM release instead and skip this guide; this page will say so.
