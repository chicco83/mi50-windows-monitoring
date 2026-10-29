// gpu_stress_d3d11 v1.0.0 - 2026-10-29 13:30
// Minimal DirectCompute (D3D11) ALU load on ONE adapter chosen by name, for testing sensors under load.
// Small dispatches (a few ms each) so Windows TDR never triggers. Stops after <seconds> or when stop.flag exists next to the exe.
// Build: csc /nologo /platform:x64 /out:gpu_stress_d3d11.exe gpu_stress_d3d11_v1.0.0_20261029-1330.cs
// Usage: gpu_stress_d3d11.exe <seconds> "<adapter name part, e.g. Pro VII>" [pauseMs=2]
// COM is called through raw vtable slots (D3D11 headers: Device: CreateBuffer=3, CreateUAV=8, CreateComputeShader=18;
// Context: Dispatch=41, CSSetUAVs=68, CSSetShader=69, Flush=111; DXGI factory EnumAdapters1=12, adapter GetDesc1=10).
using System; using System.IO; using System.Runtime.InteropServices; using System.Threading; using System.Diagnostics;
public static class S {
 [DllImport("dxgi.dll")] static extern int CreateDXGIFactory1(ref Guid r, out IntPtr f);
 [DllImport("d3d11.dll")] static extern int D3D11CreateDevice(IntPtr adapter,int driverType,IntPtr sw,uint flags,IntPtr lv,uint n,uint sdk,out IntPtr dev,out int fl,out IntPtr ctx);
 [DllImport("d3dcompiler_47.dll")] static extern int D3DCompile(byte[] src,IntPtr len,string name,IntPtr def,IntPtr inc,string entry,string target,uint f1,uint f2,out IntPtr code,out IntPtr err);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int D_EnumAdapters1(IntPtr t,uint i,out IntPtr a);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int D_GetDesc1(IntPtr t,IntPtr desc);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int D_CreateBuffer(IntPtr t,ref BufDesc d,IntPtr init,out IntPtr b);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int D_CreateUAV(IntPtr t,IntPtr res,ref UavDesc d,out IntPtr v);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int D_CreateCS(IntPtr t,IntPtr code,IntPtr len,IntPtr link,out IntPtr cs);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void D_CSSetShader(IntPtr t,IntPtr cs,IntPtr ci,uint n);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void D_CSSetUAVs(IntPtr t,uint start,uint n,IntPtr[] uavs,IntPtr[] init);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void D_Dispatch(IntPtr t,uint x,uint y,uint z);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void D_Flush(IntPtr t);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate IntPtr D_Blob(IntPtr t);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate IntPtr D_BlobSize(IntPtr t);
 [StructLayout(LayoutKind.Sequential)] struct BufDesc{public uint ByteWidth,Usage,BindFlags,CPUAccess,MiscFlags,StructureByteStride;}
 [StructLayout(LayoutKind.Sequential)] struct UavDesc{public uint Format,ViewDimension,FirstElement,NumElements,Flags;}
 static IntPtr Slot(IntPtr o,int i){ return Marshal.ReadIntPtr(Marshal.ReadIntPtr(o),i*IntPtr.Size); }
 static T F<T>(IntPtr o,int i) where T:class { return (T)(object)Marshal.GetDelegateForFunctionPointer(Slot(o,i),typeof(T)); }
 const string HLSL=@"RWStructuredBuffer<float> o:register(u0);
[numthreads(256,1,1)] void main(uint3 id:SV_DispatchThreadID){ float a=id.x*0.001f+1.0f,b=0.5f;
 [loop] for(int i=0;i<2048;i++){ a=mad(a,1.0001f,b); b=mad(b,0.9999f,a*0.0001f); a=mad(a,0.9999f,b); }
 o[id.x]=a+b; }";
 public static int Main(string[] a){
  // 3rd arg: pause in ms after every 4 dispatches (0 = full load)
  int secs=int.Parse(a[0]); int sleepMs=a.Length>2?int.Parse(a[2]):2; string want=a[1]; Guid g=new Guid("770aae78-f26f-4dba-a829-253c83d1b387"); // IDXGIFactory1
  IntPtr fac; if(CreateDXGIFactory1(ref g,out fac)!=0){ Console.WriteLine("no DXGI"); return 1; }
  IntPtr ad=IntPtr.Zero; IntPtr desc=Marshal.AllocHGlobal(512);
  for(uint i=0;;i++){ IntPtr x; if(F<D_EnumAdapters1>(fac,12)(fac,i,out x)!=0) break; F<D_GetDesc1>(x,10)(x,desc);
   string n=Marshal.PtrToStringUni(desc); Console.WriteLine("adapter "+i+": "+n); if(ad==IntPtr.Zero && n.IndexOf(want,StringComparison.OrdinalIgnoreCase)>=0) ad=x; }
  if(ad==IntPtr.Zero){ Console.WriteLine("adapter not found"); return 2; }
  IntPtr dev,ctx; int fl; int hr=D3D11CreateDevice(ad,0,IntPtr.Zero,0,IntPtr.Zero,0,7,out dev,out fl,out ctx); Console.WriteLine("D3D11CreateDevice hr="+hr+" fl=0x"+fl.ToString("x")); if(hr!=0) return 3;
  IntPtr code,err; hr=D3DCompile(System.Text.Encoding.ASCII.GetBytes(HLSL),(IntPtr)HLSL.Length,"s",IntPtr.Zero,IntPtr.Zero,"main","cs_5_0",0,0,out code,out err);
  if(hr!=0){ Console.WriteLine("compile failed "+hr); return 4; }
  IntPtr cptr=F<D_Blob>(code,3)(code); IntPtr csz=F<D_BlobSize>(code,4)(code); IntPtr cs; hr=F<D_CreateCS>(dev,18)(dev,cptr,csz,IntPtr.Zero,out cs); if(hr!=0){ Console.WriteLine("CreateCS "+hr); return 5; }
  uint threads=256*4096; var bd=new BufDesc{ByteWidth=threads*4,Usage=0,BindFlags=0x80,CPUAccess=0,MiscFlags=0x40,StructureByteStride=4}; IntPtr buf;
  hr=F<D_CreateBuffer>(dev,3)(dev,ref bd,IntPtr.Zero,out buf); if(hr!=0){ Console.WriteLine("CreateBuffer "+hr); return 6; }
  var ud=new UavDesc{Format=0,ViewDimension=1,FirstElement=0,NumElements=threads,Flags=0}; IntPtr uav; hr=F<D_CreateUAV>(dev,8)(dev,buf,ref ud,out uav); if(hr!=0){ Console.WriteLine("CreateUAV "+hr); return 7; }
  F<D_CSSetShader>(ctx,69)(ctx,cs,IntPtr.Zero,0); F<D_CSSetUAVs>(ctx,68)(ctx,0,1,new IntPtr[]{uav},new IntPtr[]{new IntPtr(-1)});
  var disp=F<D_Dispatch>(ctx,41); var flush=F<D_Flush>(ctx,111); var sw=Stopwatch.StartNew(); long n2=0; string stop=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"stop.flag");
  Console.WriteLine("load running on '"+want+"' for "+secs+" s");
  while(sw.Elapsed.TotalSeconds<secs && !File.Exists(stop)){ disp(ctx,4096,1,1); n2++; if((n2&3)==0){ flush(ctx); if(sleepMs>0) Thread.Sleep(sleepMs); } }
  Console.WriteLine("done, dispatches="+n2); return 0;
 }
}
