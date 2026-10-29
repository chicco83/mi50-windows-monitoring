// adl_pmlog_probe v1.0.0 - 2026-10-29 11:00
// Read-only ADL PMLog probe. Loads a *specific* atiadlxx.dll (e.g. the one shipped inside the
// driver package of the card, NOT the one in System32), finds the adapter by PCI device id
// and prints the PMLog sensors for three samples. Stops and destroys the PMLog device at exit.
//
// Build (x64 only, .NET Framework csc is enough):
//   csc /nologo /platform:x64 /out:adl_pmlog_probe.exe adl_pmlog_probe_v1.0.0_20261029-1100.cs
// Usage:
//   adl_pmlog_probe.exe "<path>\atiadlxx.dll" [DEV_66A1]
using System; using System.Runtime.InteropServices; using System.Threading; using System.Collections.Generic;
public static class P {
 [DllImport("kernel32", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr LoadLibraryEx(string f, IntPtr h, uint fl);
 [DllImport("kernel32", CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr m, string n);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Alloc(int s);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FCreate(Alloc a, int c, ref IntPtr ctx);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FNum(IntPtr ctx, ref int n);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FInfo(IntPtr ctx, IntPtr buf, int size);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FPmCreate(IntPtr ctx, int idx, ref uint dev);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FPmSup(IntPtr ctx, int idx, IntPtr buf);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FPmStart(IntPtr ctx, int idx, IntPtr inp, IntPtr outp, uint dev);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FPmStop(IntPtr ctx, int idx, uint dev);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FPmDestroy(IntPtr ctx, uint dev);
 // ADL calls this to allocate memory it hands back to us.
 static Alloc A = delegate(int s){ return Marshal.AllocCoTaskMem(s); };
 static void W(string s){ Console.WriteLine(s); Console.Out.Flush(); }
 static T Fn<T>(IntPtr m, string n) where T:class { IntPtr p=GetProcAddress(m,n); if(p==IntPtr.Zero) return null; return (T)(object)Marshal.GetDelegateForFunctionPointer(p,typeof(T)); }
 // PMLog sensor ids (ADL_PMLOG_*). Names as observed on a Vega 20 / MI50; ids not listed print as "id N".
 static Dictionary<int,string> N=new Dictionary<int,string>{{1,"CLK_GFX MHz"},{2,"CLK_MEM MHz"},{3,"CLK_SOC MHz"},{4,"CLK_UVD1"},{5,"CLK_UVD2"},{6,"CLK_VCE"},{8,"TEMP_EDGE C"},{9,"TEMP_MEM(HBM) C"},{10,"TEMP_VRVDDC C"},{14,"FAN_RPM"},{15,"FAN_%"},{16,"SOC_VOLT mV"},{19,"ACTIVITY_GFX %"},{20,"ACTIVITY_MEM %"},{21,"GFX_VOLT mV"},{22,"MEM_VOLT mV"},{23,"ASIC_POWER W"},{24,"TEMP_VRSOC C"},{25,"TEMP_VRMVDD0 C"},{26,"TEMP_VRMVDD1 C"},{27,"TEMP_HOTSPOT(junction) C"}};
 public static void Main(string[] a){
  if(a.Length<1){ W("usage: adl_pmlog_probe.exe <atiadlxx.dll> [DEV_xxxx]"); return; }
  string dev_id = a.Length>1 ? a[1] : "DEV_66A1"; // 66A1 = Vega 20 / MI50
  // 8 = LOAD_WITH_ALTERED_SEARCH_PATH, so the DLL's own dependencies are found next to it.
  IntPtr m=LoadLibraryEx(a[0],IntPtr.Zero,8); if(m==IntPtr.Zero){ W("LoadLibrary failed, error "+Marshal.GetLastWin32Error()); return; }
  IntPtr ctx=IntPtr.Zero; Fn<FCreate>(m,"ADL2_Main_Control_Create")(A,1,ref ctx);
  int n=0; Fn<FNum>(m,"ADL2_Adapter_NumberOfAdapters_Get")(ctx,ref n);
  // sizeof(ADLAdapterInfo) on x64 = 1572 bytes; adapter index is the int at offset 4.
  int SZ=1572; IntPtr buf=Marshal.AllocCoTaskMem(n*SZ); for(int i=0;i<n*SZ;i++) Marshal.WriteByte(buf,i,0);
  Fn<FInfo>(m,"ADL2_Adapter_AdapterInfo_Get")(ctx,buf,n*SZ);
  int idx=-1;
  for(int i=0;i<n;i++){ IntPtr b=IntPtr.Add(buf,i*SZ);
   // strPNPString sits after iSize,iAdapterIndex,strUDID[256],iBusNumber..iFunctionNumber,iVendorID,strAdapterName[256],strDisplayName[256],iPresent,iExist,strDriverPath[256],strDriverPathExt[256]
   string pnp=Marshal.PtrToStringAnsi(IntPtr.Add(b,8+256+16+256+256+8+256+256),256);
   if(pnp.IndexOf(dev_id,StringComparison.OrdinalIgnoreCase)>=0){ idx=Marshal.ReadInt32(b,4); break; } }
  W("adapter "+dev_id+" index in THIS dll = "+idx); if(idx<0) return;
  uint dev=0; W("PMLog_Device_Create="+Fn<FPmCreate>(m,"ADL2_Device_PMLog_Device_Create")(ctx,idx,ref dev)+" dev="+dev);
  IntPtr sup=Marshal.AllocCoTaskMem(2048); for(int k=0;k<2048;k++) Marshal.WriteByte(sup,k,0);
  W("PMLog_Support_Get="+Fn<FPmSup>(m,"ADL2_Adapter_PMLog_Support_Get")(ctx,idx,sup));
  // Request every supported sensor id (zero-terminated list of 16-bit ids), sample rate 1000 ms at offset 512.
  IntPtr inp=Marshal.AllocCoTaskMem(576); for(int k=0;k<576;k++) Marshal.WriteByte(inp,k,0);
  int c=0; for(;c<256;c++){ short id=Marshal.ReadInt16(sup,c*2); if(id==0) break; Marshal.WriteInt16(inp,c*2,id); }
  Marshal.WriteInt16(inp,c*2,0); Marshal.WriteInt32(inp,512,1000);
  IntPtr outp=Marshal.AllocCoTaskMem(8); Marshal.WriteInt64(outp,0,0);
  W("PMLog_Start="+Fn<FPmStart>(m,"ADL2_Adapter_PMLog_Start")(ctx,idx,inp,outp,dev));
  // Output is a pointer to a buffer: 16-byte header, then (sensorId,value) pairs of uint32.
  IntPtr data=Marshal.ReadIntPtr(outp,0); W("pLoggingAddress="+(data==IntPtr.Zero?"null":"ok"));
  if(data!=IntPtr.Zero) for(int s=0;s<3;s++){ Thread.Sleep(1500);
   W("-- sample "+s+" version="+(uint)Marshal.ReadInt32(data,0)+" rate="+Marshal.ReadInt32(data,4)+" lastUpdated="+Marshal.ReadInt64(data,8));
   for(int k=0;k<256*2;k+=2){ uint id=(uint)Marshal.ReadInt32(data,16+k*4); if(id==0) break; uint v=(uint)Marshal.ReadInt32(data,16+(k+1)*4);
    string nm; if(!N.TryGetValue((int)id,out nm)) nm="id "+id; W("   "+nm.PadRight(28)+" = "+v); } }
  Fn<FPmStop>(m,"ADL2_Adapter_PMLog_Stop")(ctx,idx,dev); Fn<FPmDestroy>(m,"ADL2_Device_PMLog_Device_Destroy")(ctx,dev); W("stopped and destroyed PMLog device");
 }
}
