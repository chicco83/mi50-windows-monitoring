// adlx_metrics_probe v1.0.0 - 2026-10-29 11:00
// Read-only ADLX probe: initializes ADLX (amdadlx64.dll, must be on PATH / next to the exe), lists GPUs and prints
// IADLXGPUMetrics values with their return codes. Calls COM-style vtables by slot index, so the slot numbers are
// tied to ADLX 1.x headers (IADLXGPUList::At_GPUList = slot 11, GetGPUMetrics = slot 18 of IADLXPerformanceMonitoringServices,
// metric getters 4..13). Verify against the ADLX SDK headers before trusting it on a new ADLX version.
// Only tested on an RX 9070 XT: it never saw the MI50 (ADLX did not enumerate it with the System32 runtime).
// Build: csc /nologo /platform:x64 /out:adlx_metrics_probe.exe adlx_metrics_probe_v1.0.0_20261029-1100.cs
using System; using System.Runtime.InteropServices;
public static class X {
 [DllImport("amdadlx64.dll")] static extern int ADLXInitialize(ulong ver, out IntPtr sys);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int D1(IntPtr t, out IntPtr o);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int D2(IntPtr t, uint i, out IntPtr o);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate uint DS(IntPtr t);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int DStr(IntPtr t, out IntPtr s);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int D3(IntPtr t, IntPtr g, out IntPtr o);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int DD(IntPtr t, out double d);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int DI(IntPtr t, out int d);
 static IntPtr Slot(IntPtr obj,int i){ IntPtr vt=Marshal.ReadIntPtr(obj); return Marshal.ReadIntPtr(vt,i*IntPtr.Size); }
 static T F<T>(IntPtr o,int i) where T:class { return (T)(object)Marshal.GetDelegateForFunctionPointer(Slot(o,i),typeof(T)); }
 static void P(string s){ Console.WriteLine(s); Console.Out.Flush(); }
 public static void Main(){
  IntPtr sys; int r=ADLXInitialize(1UL<<48,out sys); P("init="+r); if(r!=0) return;
  IntPtr gl; P("GetGPUs="+F<D1>(sys,1)(sys,out gl));
  uint n=F<DS>(gl,3)(gl); P("gpus="+n);
  IntPtr ps; P("perf="+F<D1>(sys,9)(sys,out ps));
  for(uint i=0;i<n;i++){ IntPtr g; P("At="+F<D2>(gl,11)(gl,i,out g));
   IntPtr nm; F<DStr>(g,7)(g,out nm); IntPtr pnp; F<DStr>(g,9)(g,out pnp);
   P("GPU "+i+": "+Marshal.PtrToStringAnsi(nm)+" | "+Marshal.PtrToStringAnsi(pnp));
   IntPtr m; int rr=F<D3>(ps,18)(ps,g,out m); P("  metrics ret="+rr); if(rr!=0) continue;
   double d; int k;
   rr=F<DD>(m,7)(m,out d); P("  temp ret="+rr+" "+d);
   rr=F<DD>(m,8)(m,out d); P("  hotspot ret="+rr+" "+d);
   rr=F<DD>(m,4)(m,out d); P("  usage ret="+rr+" "+d);
   rr=F<DD>(m,9)(m,out d); P("  power ret="+rr+" "+d);
   rr=F<DD>(m,10)(m,out d); P("  boardpower ret="+rr+" "+d);
   rr=F<DI>(m,5)(m,out k); P("  clock ret="+rr+" "+k);
   rr=F<DI>(m,6)(m,out k); P("  vramclock ret="+rr+" "+k);
   rr=F<DI>(m,11)(m,out k); P("  fan ret="+rr+" "+k);
   rr=F<DI>(m,12)(m,out k); P("  vram ret="+rr+" "+k);
   rr=F<DI>(m,13)(m,out k); P("  volt ret="+rr+" "+k);
  } }
}
