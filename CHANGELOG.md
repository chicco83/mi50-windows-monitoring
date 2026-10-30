# Changelog
## 2026-10-30
- Release `v0.1.0-lhm-mi50`: prebuilt unofficial LibreHardwareMonitor (commit 529a670, net472 x64, MPL-2.0 included, SHA-256 on the release page). Unzipped to a clean folder: 41 sensors on the MI50 and the GUI starts. README, GUIDE_LHM and GUIDE_TABLET point to it. Rollback: `gh release delete v0.1.0-lhm-mi50 --cleanup-tag`.

## 2026-10-29 (5)
- README: new section "Start here: how do I get the readings" (probe / LHM / tablet). New `docs/GUIDE_LHM.md` (build LHM from the fork branch; verified from a fresh clone, 41 sensors) and `docs/GUIDE_TABLET.md` (Tab2revamp). Removed a claim about HWiNFO that I had not tested.

## 2026-10-29 (4)
- Added MIT LICENSE (copyright holder: chicco83, the GitHub user; change if you prefer another name) and updated README.

## 2026-10-29 (3)
- Added `tools/lhm_sensor_log` and `tools/gpu_stress_d3d11`; LHM-patched build tested at idle (60 s) and under load; RESULTS updated (hot spot 91 C in ~15 s at 170 W without proper cooling).

## 2026-10-29 (2)
- README: Prerequisites (Polaris/Vega 26.5.2 driver, manual bind to Radeon Pro VII, display GPU, power, 4G decoding as unverified), Cooling section with 3 shroud links, Quick start, Related. New `Find-AdlPackageDll` PowerShell script (tested on the MI50 machine).

## 2026-10-29
- Initial version: README, results, three read-only probes (versioned `v1.0.0_20261029-1100`), Reddit draft. No git remote yet; license undecided.
