# Guide: show the MI50 on the Android tablet (Tab2revamp)

[Tab2revamp](https://github.com/chicco83/Tab2revamp) turns an old Android tablet (built and tested on a Galaxy Tab 2 10.1, GT-P5100) into a live dashboard of a Windows PC:
CPU, RAM, disks, network, GPUs and every temperature/fan sensor. A small Python server on the PC feeds an Android app over USB or Wi-Fi.
This guide only covers what is specific to the MI50. For the tablet itself read that project's `MANUAL.md` (in Italian).

> You do **not** need the tablet to read the MI50. If you only want the sensors on the PC, use the [LHM guide](GUIDE_LHM.md) or the probe.

## What the tablet shows for the MI50
- A card on page 2 ("GPU e sensori") with load, VRAM and the card's sensors: core, memory (HBM), VR VDDC and hot spot temperatures, fan.
- With the patched LHM (below) those temperatures come from the card's own ADL PMLog. Without it the server falls back to the WDDM source (edge temperature, as in Task Manager).
- Not shown yet (as of 2026-10-29, per entry 31 of the Tab2revamp changelog): power, voltages, clocks and loads of the MI50. They exist in LHM; the server and app do not use them yet.

## Steps
1. **MI50 on Windows**: do the README prerequisites (driver bound to AMD Radeon Pro VII).
2. **Get Tab2revamp**: `git clone https://github.com/chicco83/Tab2revamp`. You need Python 3.8+ and `adb` (Android platform tools) for the USB mode.
3. **Patched LHM for the server** (until PR #2597 is released):
   1. Get the patched LHM: unzip the prebuilt zip from the [latest release](https://github.com/chicco83/mi50-windows-monitoring/releases/latest) (its files are in the folder `LibreHardwareMonitor-mi50`), or build it as in the [LHM guide](GUIDE_LHM.md).
   2. Download the official LibreHardwareMonitor v0.9.6 release and unzip it to `Tab2revamp\windows\lhm\LibreHardwareMonitor_v0.9.6-mi50\`.
   3. Overwrite in that folder these four files with the ones from the release zip (or, if you built it yourself, from `bin\Release\x64\net472\`):
      `LibreHardwareMonitorLib.dll`, `BlackSharp.Core.dll`, `DiskInfoToolkit.dll`, `RAMSPDToolkit-NDD.dll`.
   4. From Git Bash in the Tab2revamp folder: `bash windows/agent/sensors/build_tab2sensors_v0.2.0_20261028-1200.sh` (builds `Tab2Sensors.exe` next to those DLLs).
      The server uses the highest `LibreHardwareMonitor_v*` folder name, so `-mi50` wins over `v0.9.6`.
   - Rollback: delete the `-mi50` folder and restart the server.
4. **Start the server** (as administrator for CPU/board sensors): `windows\agent\start_tab2monitor_v0.2.0_20261022-0055.cmd`, or install it as a login task with
   `install_tab2monitor_service_v0.2.0_20261022-0055.cmd`. Open `http://localhost:8090` on the PC to check: the GPU list must include the Radeon Pro VII.
   The live JSON is at `http://localhost:8090/api/metrics` (`srv.lhm` should say `lib ok`).
5. **Tablet app**: build and install the APK with the scripts in `android/` (MANUAL section 2.4: `build_tab2monitor_*.sh`, then `install_tab2monitor_*.sh` with the tablet on USB debugging).
6. **Names and fans**: open `http://localhost:8090/config` on the PC to rename sensors or hide the ones you do not want on the tablet.

## Check it works
- PC: `http://localhost:8090/api/metrics` lists the MI50 with temperatures; the green dot in the app header means fresh data.
- Tablet: page 2 shows a card for "AMD Radeon Pro VII". Under load the hot spot rises first, so use it as the early warning for cooling.

## Limits
- Not tested with a tablet other than the Galaxy Tab 2 10.1.
- Windows only; the server is Python plus `psutil` and the `Tab2Sensors.exe` helper built from LHM.
- The steps above follow the Tab2revamp changelog and manual; I re-verified only the LHM build and the sensor list, not a full reinstall from scratch.
- Check the Tab2revamp `CHANGELOG.md` for newer MI50 features and file names (the version numbers in the commands change).
