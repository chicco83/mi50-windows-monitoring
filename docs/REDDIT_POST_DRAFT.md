# Reddit post draft (v0.1, 2026-10-29) - NOT posted

Target: r/LocalLLaMA (primary), possibly r/radeon, r/homelab. Check each sub's self-promotion rules before posting.

**Title:** Reading MI50 sensors on Windows (forced Radeon VII driver): why every tool shows nothing, and a fix

**Body:**

If you run an MI50 on Windows with the Radeon VII / Pro VII driver forced onto it, you've probably noticed that
HWiNFO-style tools and LibreHardwareMonitor show no sensors for it, while Task Manager shows a temperature.
I dug into why and found a way to read much more than just temperature.

**Cause.** Windows keeps a single `atiadlxx.dll` in System32, taken from the newest AMD driver installed. In my box that is
the one from an RX 9070 XT (ADL 7.26), and it rejects the MI50 with `ADL_ERR_INVALID_ADL_IDX` (-5) on every call.
The MI50's own driver package in the DriverStore ships its own `atiadlxx.dll` (ADL 7.23). Load *that* one and ADL PMLog works.

**Prerequisites in short.** Polaris/Vega legacy driver 26.5.2 installed by hand, then Device Manager → card → Update driver → Let me pick → AMD Radeon Pro VII. A second GPU for display, and real cooling (passive card; the repo links a few printable fan shrouds).

**What I can read at idle (21 sensors):** edge / junction / HBM temperature, VR temperatures, real ASIC power in watts,
core / SoC / memory voltage, core / memory / SoC clocks, activity, fan. Example: 47/48 °C edge/junction, 16 W, 737 mV core.
Overdrive calls still fail, so no clock/power tuning this way, monitoring only.

**What I did about it.** Notes + three small read-only C# probes (no AMD DLLs included, just how to find yours):
<REPO LINK>
I also sent a patch to LibreHardwareMonitor that picks the right DLL per card: <PR LINK>

**Under load** (synthetic DirectCompute test, LHM build with my patch): core 100 %, 1687 MHz, 0.98 V, ~170 W, and hot spot went from 37 to 91 °C in about 15 s with no dedicated cooling, so I stopped it: plan cooling.

**Honest limits.** One card, one machine, original BIOS, Windows 11. Load was only a synthetic kernel for seconds, not a real AI workload;
no multi-hour run, no 32-bit process test, and I haven't tried a system with only the MI50. If you have a different BIOS or
driver version, I'd love to hear whether the probe works for you (run `adl_pmlog_probe.exe <path to the package atiadlxx.dll>`).
Linux still exposes more (DPM tables, power cap, V/F curve); this is about getting useful monitoring on Windows.

