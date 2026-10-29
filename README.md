# AMD Instinct MI50 on Windows: reading its sensors

Notes, probes and measurements for running a **Radeon Instinct MI50 (Vega 20, 32 GB HBM2) on Windows 11 with the
Radeon VII / Radeon Pro VII driver forced onto it**, and for reading its temperatures, power, voltages and clocks.

> Status: personal findings, one card, one machine. See [docs/RESULTS.md](docs/RESULTS.md) for exactly what was measured
> and what was **not**. Not affiliated with AMD.

## Setup this was tested on

- MI50 with its **original BIOS**, driver forced to the Radeon VII / Pro VII package (installed through "Have disk" in Device Manager).
- Same PC also has a Radeon RX 9070 XT with the current Adrenalin driver. This matters (see below).
- Windows 11 Pro 26200, .NET Framework csc for the probes (no SDK needed).

## Prerequisites

**Hardware**
- An MI50 (Vega 20, PCI id `VEN_1002&DEV_66A1`). Tested with the original BIOS, no BIOS flash.
- **Another GPU (or iGPU) for the display.** The MI50 has no video outputs.
- Two 8-pin PCIe power connectors and a PSU that can feed ~300 W (the card's rated TDP).
- **Active cooling.** The MI50 is a passive, server-style card: see [Cooling](#cooling).
- A PCIe x16 slot. Server cards usually need **Above 4G Decoding** (and often Resizable BAR) enabled in the motherboard BIOS;
  I have not tested without it, so treat this as a commonly reported requirement, not a measured one.

**Software**
- Windows 10/11 x64 (tested on Windows 11 Pro 26200). The probes need only the .NET Framework compiler (`csc.exe`, part of Windows).
- The AMD **Polaris/Vega legacy driver** (Adrenalin 26.5.2 for Polaris and Vega, May 2026), installed by hand:
  [release notes](https://www.amd.com/en/resources/support-articles/release-notes/RN-RAD-WIN-26-5-2-POLARIS-VEGA.html).
  On my machine the resulting driver is 31.0.21925.1001 dated 2026-05-20.
- Then bind the card to the **AMD Radeon Pro VII** driver by hand, because Windows does not offer it for the MI50:
  1. Device Manager → the detected card (shown as a Microsoft Basic Display Adapter or an unknown/compute device).
  2. **Driver** tab → *Update driver* → *Browse my computer for drivers* → *Let me pick from a list of available drivers on my computer*.
  3. Choose **AMD Radeon Pro VII**.
- Reboot if asked. The card should now appear as "AMD Radeon Pro VII" in Device Manager and Task Manager shows its temperature.
- The probes read the ADL DLL that this driver package leaves in the DriverStore, so the package must be installed
  (staged) *and* bound to the card. If you install a newer Radeon driver for another GPU later, System32 gets its newer
  `atiadlxx.dll`, but the Polaris/Vega package stays in the DriverStore, so the fix below keeps working.

Not verified by me: whether other driver versions or the plain Radeon VII entry (instead of Pro VII) behave the same.

## The problem

Tools that use AMD's ADL library (LibreHardwareMonitor, many others) show **no sensors at all** for the MI50, while
Task Manager shows its temperature. Every ADL call for the card returns `-5` (`ADL_ERR_INVALID_ADL_IDX`).

## Root cause

Windows has **one** `atiadlxx.dll` in `System32`, copied from the *newest* installed AMD display driver (here ADL 7.26,
from the RX 9070 XT). That runtime does not accept the MI50, which belongs to an older driver package.
The MI50's own driver package ships its **own** `atiadlxx.dll` (ADL 7.23) in the DriverStore, and that one works.
The adapter indices differ between the two DLLs, so match the card by PCI id / PNP string, not by index.

## What works

Load the package DLL directly and use **PMLog** (`ADL2_Adapter_PMLog_*`): 21 sensors, real values (watts, millivolts, MHz, °C).

| Works | Does not work (with the package DLL) |
|---|---|
| PMLog: temperatures (edge, junction, HBM, VR VDDC / SoC / MVDD0 / MVDD1), ASIC power (W), core/SoC/memory voltage, core/memory/SoC/UVD/VCE clock, core & memory activity, fan | Overdrive calls (`-8`), legacy ADL API (`-1`) |

Temperature alone is also available without any of this through WDDM (`D3DKMTQueryAdapterInfo`, `KMTQAITYPE_ADAPTERPERFDATA`,
value in tenths of a degree), which is what Task Manager uses.

## How to find the right DLL (no DLL is included in this repo)

Quick way: run [probes/Find-AdlPackageDll_v1.0.0_20261029-1200.ps1](probes/Find-AdlPackageDll_v1.0.0_20261029-1200.ps1) (read-only, no admin needed; it prints the path). By hand:

1. Find the card's PNP id: `Get-PnpDevice -InstanceId 'PCI\VEN_1002&DEV_66A1*'` (66A1 = Vega 20 / MI50).
2. `HKLM\SYSTEM\CurrentControlSet\Enum\<InstanceId>` → value `Driver` → `HKLM\SYSTEM\CurrentControlSet\Control\Class\<Driver>` → value `InfPath` (e.g. `oem12.inf`).
3. Map that INF to its DriverStore folder (`SetupGetInfDriverStoreLocationW`, or look in `C:\Windows\System32\DriverStore\FileRepository\`).
4. In that folder (a subfolder such as `B0xxxxx`) take `atiadlxx.dll` (64-bit) or `atiadlxy.dll` (32-bit).
5. Load it with `LoadLibraryEx(path, NULL, LOAD_WITH_ALTERED_SEARCH_PATH)`.

## Cooling

The MI50 has no fan. In my test, with the first model below (printed shroud plus fan; fan model and speed not recorded), the junction (hot spot) went from 37 to 91 °C in about 15 s at full synthetic load (~170 W), so plan a fan shroud/duct with a strong fan before using it for sustained loads, so plan a fan shroud/duct before using it. 3D-printable
options (community designs; check dimensions against your card and case, and verify fit and noise yourself). The first one is what I currently use; the other two I have not tried:

- [AMD Radeon Instinct MI50 (MakerWorld)](https://makerworld.com/it/models/1443230-amd-radeon-instinct-mi50#profileId-1502520)
- [AMD V340 / V520 / V620 / MI25 / MI50 / MI60 / MI100 / MI210 fan shroud (MakerWorld)](https://makerworld.com/it/models/2762655-amd-v340-v520-v620-mi25-mi50-mi60-mi100-mi210-fan)
- [AMD MI50 / MI100 / MI210 GPU 80 mm fan cooling attachment (Printables)](https://www.printables.com/model/1479089-amd-mi50-mi100-m210-gpu-80mm-fan-cooling-attachmen/related)

The PMLog sensors below make it easy to check the result: watch the HBM and junction temperatures under load.
The fan sensor reads 0 RPM because the fan is not the card's own, so use the card temperatures, not the fan value.

## Probes

All read-only. Build with the .NET Framework compiler: `csc /nologo /platform:x64 /out:x.exe file.cs`.

| File | What it does |
|---|---|
| [probes/Find-AdlPackageDll_v1.0.0_20261029-1200.ps1](probes/Find-AdlPackageDll_v1.0.0_20261029-1200.ps1) | Prints the path of the card's own `atiadlxx.dll` (PowerShell) |
| [probes/adl_pmlog_probe_v1.0.0_20261029-1100.cs](probes/adl_pmlog_probe_v1.0.0_20261029-1100.cs) | Loads a given `atiadlxx.dll`, finds the card by device id, prints 3 PMLog samples |
| [probes/adl_overdrive_probe_v1.0.0_20261029-1100.cs](probes/adl_overdrive_probe_v1.0.0_20261029-1100.cs) | Tries Overdrive / OverdriveN / PMLog support / legacy ADL, to reproduce the `-5` |
| [probes/adlx_metrics_probe_v1.0.0_20261029-1100.cs](probes/adlx_metrics_probe_v1.0.0_20261029-1100.cs) | ADLX metrics on the RX 9070 XT (ADLX never listed the MI50 here) |

## Upstream

I implemented this in LibreHardwareMonitor: [LibreHardwareMonitor/LibreHardwareMonitor#2597](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/pull/2597)
(driver-package ADL for PMLog + WDDM temperature fallback; fixes issue #1813). Status: open at time of writing.

## What Linux reads (for comparison)

From kernel/ROCm documentation, **not** measured on this card: `temp1/2/3` (edge/junction/HBM), `power1_average`, `in0`,
`freq1/2`, fan, `gpu_busy_percent`, `pp_dpm_*`, `pp_od_clk_voltage`, `gpu_metrics`. The PMLog set on Windows covers most
of the temperature/power/voltage/clock part; DPM tables, power cap and the V/F curve are not reachable from Windows this way.

## Quick start

```powershell
$dll = .\probes\Find-AdlPackageDll_v1.0.0_20261029-1200.ps1 | Select-Object -Last 1
csc /nologo /platform:x64 /out:adl_pmlog_probe.exe probes\adl_pmlog_probe_v1.0.0_20261029-1100.cs
.\adl_pmlog_probe.exe $dll
```

## Related

The findings were first used to feed an Android dashboard app (tab2revamp); the repo here does not depend on it.

## License

MIT, see [LICENSE](LICENSE). The AMD DLLs and driver are AMD's and are not distributed here.
