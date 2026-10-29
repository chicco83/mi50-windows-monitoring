// lhm_sensor_log v1.0.0 - 2026-10-29 13:00
// Read-only: opens LibreHardwareMonitorLib (GPU only), samples every GPU matching a name filter and
// prints every sensor each interval; at the end prints min/max per sensor. Optional CSV output.
// Build (x64, reference the lib you want to test; copy its dependencies next to the exe):
//   csc /nologo /platform:x64 /r:LibreHardwareMonitorLib.dll /out:lhm_sensor_log.exe lhm_sensor_log_v1.0.0_20261029-1300.cs
// Usage: lhm_sensor_log.exe <seconds> <intervalSeconds> [nameFilter=MI50|Pro VII] [csvPath] [abortTempC=88]
// Aborts early (returns 3) if any temperature sensor of the matched GPU exceeds abortTempC.
using System; using System.Collections.Generic; using System.IO; using System.Linq; using System.Threading; using System.Globalization;
using LibreHardwareMonitor.Hardware;
public static class T {
 public static int Main(string[] a){
  int secs=int.Parse(a[0]), step=int.Parse(a[1]); string[] filt=(a.Length>2?a[2]:"MI50|Pro VII").Split('|');
  string csv=a.Length>3?a[3]:null; double abortC=a.Length>4?double.Parse(a[4]):88;
  var c=new Computer{IsGpuEnabled=true}; c.Open();
  var gpus=new List<IHardware>(); foreach(var h in c.Hardware) if((h.HardwareType==HardwareType.GpuAmd||h.HardwareType==HardwareType.GpuNvidia||h.HardwareType==HardwareType.GpuIntel) && filt.Any(f=>h.Name.IndexOf(f,StringComparison.OrdinalIgnoreCase)>=0)) gpus.Add(h);
  Console.WriteLine("GPUs matched: "+string.Join("; ",gpus.Select(g=>g.Name+" ["+g.Identifier+"]")));
  if(gpus.Count==0){ foreach(var h in c.Hardware) Console.WriteLine("  seen: "+h.Name+" ["+h.Identifier+"]"); return 2; }
  var mn=new Dictionary<string,float>(); var mx=new Dictionary<string,float>(); StreamWriter w=csv!=null?new StreamWriter(csv):null;
  if(w!=null) w.WriteLine("time,hardware,sensor,type,value");
  DateTime end=DateTime.Now.AddSeconds(secs); int n=0; int rc=0;
  while(DateTime.Now<end){
   foreach(var g in gpus){ g.Update();
    var line=new List<string>();
    foreach(var s in g.Sensors){ if(!s.Value.HasValue) continue; string k=g.Name+"|"+s.SensorType+"|"+s.Name; float v=s.Value.Value;
     if(!mn.ContainsKey(k)||v<mn[k]) mn[k]=v; if(!mx.ContainsKey(k)||v>mx[k]) mx[k]=v;
     if(w!=null) w.WriteLine(DateTime.Now.ToString("HH:mm:ss")+","+g.Name+","+s.Name+","+s.SensorType+","+v.ToString(CultureInfo.InvariantCulture));
     if(s.SensorType==SensorType.Temperature && v>abortC){ Console.WriteLine("ABORT: "+s.Name+" "+v+" C > "+abortC); rc=3; }
     if(s.SensorType==SensorType.Temperature||s.SensorType==SensorType.Power||s.SensorType==SensorType.Load||s.SensorType==SensorType.Voltage||s.Name.Contains("Core")&&s.SensorType==SensorType.Clock) line.Add(s.Name+"="+v.ToString("0.###",CultureInfo.InvariantCulture)); }
    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss")+" "+g.Name+" ("+g.Sensors.Length+" sensors): "+string.Join(", ",line)); }
   n++; if(rc!=0) break; Thread.Sleep(step*1000); }
  Console.WriteLine("---- min/max over "+n+" samples");
  foreach(var k in mn.Keys.OrderBy(x=>x)) Console.WriteLine("  "+k.PadRight(60)+" min="+mn[k].ToString("0.###",CultureInfo.InvariantCulture)+" max="+mx[k].ToString("0.###",CultureInfo.InvariantCulture));
  if(w!=null) w.Close(); c.Close(); return rc;
 }
}
