// adl_overdrive_probe v1.0.0 - 2026-10-29 11:00
// Read-only ADL probe. Loads the atiadlxx.dll at the given path and, for the adapter matching a PCI
// device id, tries Overdrive caps, OverdriveN temperature, PMLog support and the legacy (context-less) API.
// Use it twice: with the System32 DLL (expect -5 ADL_ERR_INVALID_ADL_IDX for the MI50) and with the
// DLL from the card's own driver package (PMLog works, Overdrive returns -8 / legacy -1).
// Build: csc /nologo /platform:x64 /out:adl_overdrive_probe.exe adl_overdrive_probe_v1.0.0_20261029-1100.cs
// Usage: adl_overdrive_probe.exe "<path>tiadlxx.dll"   (device id hard-coded below: DEV_66A1)
using System; using System.Runtime.InteropServices; using System.Text;
public static class P {
 [DllImport("kernel32", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr LoadLibraryEx(string f, IntPtr h, uint fl);
 [DllImport("kernel32", CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr m, string n);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Alloc(int s);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FCreate(Alloc a, int c, ref IntPtr ctx);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FNum(IntPtr ctx, ref int n);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FInfo(IntPtr ctx, IntPtr buf, int size);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FCaps(IntPtr ctx, int idx, ref int s, ref int e, ref int v);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FTemp5(IntPtr ctx, int idx, int tc, ref int[] t);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FTempN(IntPtr ctx, int idx, int type, ref int t);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FPmCreate(IntPtr ctx, int idx, ref IntPtr dev);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FPmSup(IntPtr ctx, int idx, IntPtr buf);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FLegacyCreate(Alloc a, int c);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FLegacyTemp(int idx, int tc, IntPtr t);
 static Alloc A = delegate(int s){ return Marshal.AllocCoTaskMem(s); };
 static void W(string s){ Console.WriteLine(s); Console.Out.Flush(); }
 static T Fn<T>(IntPtr m, string n) where T:class { IntPtr p=GetProcAddress(m,n); if(p==IntPtr.Zero) return null; return (T)(object)Marshal.GetDelegateForFunctionPointer(p,typeof(T)); }
 const int SZ=1572; // sizeof(ADLAdapterInfo) x64
 public static void Main(string[] a){
  string dll=a[0]; W("DLL: "+dll);
  IntPtr m=LoadLibraryEx(dll,IntPtr.Zero,8); if(m==IntPtr.Zero){ W("LoadLibrary failed "+Marshal.GetLastWin32Error()); return; }
  var create=Fn<FCreate>(m,"ADL2_Main_Control_Create"); IntPtr ctx=IntPtr.Zero;
  W("ADL2_Main_Control_Create="+create(A,1,ref ctx));
  int n=0; W("NumberOfAdapters="+Fn<FNum>(m,"ADL2_Adapter_NumberOfAdapters_Get")(ctx,ref n)+" n="+n);
  IntPtr buf=Marshal.AllocCoTaskMem(n*SZ); for(int i=0;i<n*SZ;i++) Marshal.WriteByte(buf,i,0);
  W("AdapterInfo_Get="+Fn<FInfo>(m,"ADL2_Adapter_AdapterInfo_Get")(ctx,buf,n*SZ));
  var caps=Fn<FCaps>(m,"ADL2_Overdrive_Caps"); var tN=Fn<FTempN>(m,"ADL2_OverdriveN_Temperature_Get");
  var pmc=Fn<FPmCreate>(m,"ADL2_Device_PMLog_Device_Create"); var pms=Fn<FPmSup>(m,"ADL2_Adapter_PMLog_Support_Get");
  for(int i=0;i<n;i++){
   IntPtr b=IntPtr.Add(buf,i*SZ); int idx=Marshal.ReadInt32(b,4);
   string name=Marshal.PtrToStringAnsi(IntPtr.Add(b,8+256+16),256); int e=name.IndexOf('\0'); if(e>=0) name=name.Substring(0,e);
   string pnp=Marshal.PtrToStringAnsi(IntPtr.Add(b,8+256+16+256+256+8+256+256),256); e=pnp.IndexOf('\0'); if(e>=0) pnp=pnp.Substring(0,e);
   if(pnp.IndexOf("DEV_66A1",StringComparison.OrdinalIgnoreCase)<0) continue;
   W("-- adapter idx="+idx+" "+name+" "+pnp);
   int s=0,en=0,v=0; W("   Overdrive_Caps="+(caps!=null?caps(ctx,idx,ref s,ref en,ref v):-999)+" sup="+s+" en="+en+" ver="+v);
   for(int t=1;t<=8;t++){ int x=0; int r=tN!=null?tN(ctx,idx,t,ref x):-999; W("   OverdriveN temp type "+t+": ret="+r+" val="+x); }
   IntPtr dev=IntPtr.Zero; int rc=pmc!=null?pmc(ctx,idx,ref dev):-999; W("   PMLog_Device_Create="+rc);
   IntPtr sb=Marshal.AllocCoTaskMem(2048); for(int k=0;k<2048;k++) Marshal.WriteByte(sb,k,0);
   int rs=pms!=null?pms(ctx,idx,sb):-999; W("   PMLog_Support_Get="+rs);
   if(rs==0){ var sbld=new StringBuilder("   sensori: "); for(int k=0;k<256;k++){ ushort id=(ushort)Marshal.ReadInt16(sb,k*2); if(id==0) break; sbld.Append(id+" "); } W(sbld.ToString()); }
  }
  // legacy variant (no context)
  var lc=Fn<FLegacyCreate>(m,"ADL_Main_Control_Create");
  if(lc!=null){ W("ADL_Main_Control_Create (legacy)="+lc(A,1));
   var lt=Fn<FLegacyTemp>(m,"ADL_Overdrive5_Temperature_Get");
   for(int idx=0;idx<n && lt!=null;idx++){ IntPtr t=Marshal.AllocCoTaskMem(8); Marshal.WriteInt32(t,0,8); Marshal.WriteInt32(t,4,0);
    int r=lt(idx,0,t); W("   legacy OD5 temp idx "+idx+": ret="+r+" milliC="+Marshal.ReadInt32(t,4)); } }
 }
}
