# Measured results

Machine: Windows 11 Pro 26200, MI50 (DEV_66A1, original BIOS, Radeon VII / Pro VII driver 31.0.21925.1001, ADL 7.23 in its package),
plus RX 9070 XT (current Adrenalin, ADL 7.26 in System32). The ADL probe values below are at **idle**; a load test with the LHM build follows. Date: 2026-10-29.

## ADL with the System32 DLL (7.26)
Every ADL call for the MI50 returns `-5` (`ADL_ERR_INVALID_ADL_IDX`).

## ADL with the driver-package DLL (7.23): PMLog
`PMLog_Device_Create=0`, `PMLog_Support_Get=0` (21 sensors), `PMLog_Start=0`, three consistent samples at 1000 ms:

| Sensor | Value |
|---|---|
| Edge / junction temperature | 47 / 48 °C |
| HBM temperature | 46–47 °C |
| VR temperature VDDC / SoC / MVDD0 / MVDD1 | 48 / 45–46 / 42–43 / 43 °C |
| ASIC power | 16 W (real watts, not a percentage) |
| Core / SoC / memory voltage | 737 / 775 / 850 mV |
| Core / memory / SoC clock | 28 / 350 / 9 MHz |
| UVD1 / UVD2 / VCE clock | 33 MHz |
| Core and memory activity | 0 % |
| Fan | 0 RPM / 0 % (passive card, cooled by case fan) |

Edge temperature matches WDDM (`ADAPTERPERFDATA`), which is what Task Manager shows.
With the package DLL: Overdrive calls return `-8`, legacy `ADL_Overdrive5` returns `-1`; only PMLog works.

## LibreHardwareMonitor with my patch (PR #2597)
MI50 exposes GPU Core 49 °C, Memory(HBM) 49, VR VDDC 50, Hot Spot 50, Fan 0, voltages (core 0.737, SoC 0.962, memory 0.85 V),
clocks (core 28, SoC 26, memory 1000 MHz), loads 0 %, Power Package 19 W. Four open/close cycles gave the same 41 sensors each time.
RX 9070 XT unchanged. Both `atiadlxx.dll` copies loaded in the same process without problems.
(Small differences from the standalone probe are different moments in time.)

## LibreHardwareMonitor (PR #2597 build) under load - 2026-10-29
Load generated with `tools/gpu_stress_d3d11` (DirectCompute ALU loop on the MI50 adapter only), sampled with `tools/lhm_sensor_log`.
Same 41 sensors in every run. Cooling during these tests: a [3D-printed fan shroud (MakerWorld)](https://makerworld.com/it/models/1443230-amd-radeon-instinct-mi50#profileId-1502520) with its own fan (fan model and speed were not recorded, and the card reads 0 RPM because the fan is not the card's own).

| Sensor | Idle | ~40 % duty (40 s) | Full load (~12 s, stopped by my 90 °C guard) |
|---|---|---|---|
| GPU Core load (PMLog) | 0 % | 27-34 % | 100 % |
| D3D 3D engine load (WDDM) | 0 % | 39.5-40.7 % | ~100 % |
| Core clock | 28 MHz | 379-467 MHz | 1686-1687 MHz |
| Memory clock | 350-450 MHz | 492-625 MHz | 799 MHz (compute kernel touches little memory) |
| SoC clock | 9-26 MHz | 189-261 MHz | 849 MHz |
| Core / SoC / memory voltage | 0.737 / 0.78 / 0.85 V | 0.737 / 0.775-0.781 / 0.85 V | 0.981 / 0.918 / 0.85 V |
| Package power | 13-14 W | 43-50 W | 169-172 W |
| Core (edge) temperature | 36 °C | 41-47 °C | 54 → 64 °C |
| HBM temperature | 35-36 °C | 39-46 °C | 51 → 61 °C |
| VR VDDC temperature | 32 °C | 35-38 °C | 41 → 45 °C |
| **Hot spot (junction)** | 36-37 °C | 49-56 °C | 81 → 91 °C in ~15 s |

Observations
- Load, clocks, voltages and power track the workload as expected, so the PMLog mapping in LHM is correct under load, not only at idle.
- Hot spot rose roughly 1.5-2 °C per second at full load and passed 90 °C within seconds: even with that printed shroud and fan the junction passed 90 °C within seconds at full synthetic load, so a faster fan or a better duct may be needed for sustained loads (see README). It dropped back to 54 °C about 20 s after the load stopped.
- Memory load reads 0 % (the compute kernel only uses registers); not a sensor fault, but a memory-bandwidth workload was not tried.
- The first runs' CSV had decimal commas (Italian locale) that shifted columns; fixed in the logger (invariant culture).

## ADLX
With the RX 9070 XT, `IADLXGPUMetrics` returned temperature, hotspot, usage, board power, clocks, fan, VRAM, voltage (power: error 12).
ADLX did **not** enumerate the MI50 with the System32 runtime. ADLX 1.1.0.56 from the MI50's package was **not** tested.

## NOT verified
- A **real AI workload** (llama.cpp etc.) and memory-bandwidth-heavy loads; only a synthetic ALU kernel was used. Sustained full load was never run (stopped at 90 °C hot spot).
- Long-run stability (hours) with both DLLs loaded (longest run: ~2.5 min of sampling).
- 32-bit processes (`atiadlxy.dll`).
- ADLX from the driver package.
- Other MI50 BIOSes / other driver versions / a system with only the MI50.
- LHM's PMLog mapping does not expose VR SoC / MVDD0 / MVDD1 temperatures (probe shows them).
