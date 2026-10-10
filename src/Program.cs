using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;

[assembly: AssemblyTitle("Codex Ocean Boot")]
[assembly: AssemblyProduct("Codex Ocean Boot")]
[assembly: AssemblyDescription("Portable community video launcher for the Codex desktop app")]
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]
namespace CodexOceanBoot {
 internal sealed class RunLog {
  private readonly string file;
  internal RunLog(string root) { string directory=Path.Combine(root,"logs");Directory.CreateDirectory(directory);file=Path.Combine(directory,"run-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".jsonl"); }
  internal void Event(string name) { Event(name,null); }
  internal void Event(string name,object data) {
   try { File.AppendAllText(file,new JavaScriptSerializer().Serialize(new { utc=DateTime.UtcNow.ToString("o"),name=name,data=data })+Environment.NewLine,new System.Text.UTF8Encoding(false)); }catch {}
  }
 }
 public static class Program {
  [STAThread]public static int Main(string[] args) {
   string root=AppDomain.CurrentDomain.BaseDirectory;
   try {
    Config cfg=Config.Load(root);
    if(args.Contains("--diagnostics")) {
     IntPtr window=Native.FindApp(cfg);string dir=Path.Combine(root,"qa");Directory.CreateDirectory(dir);
     AtomicFile.Write(Path.Combine(dir,"diagnostics.json"),new JavaScriptSerializer().Serialize(new { windowFound=window!=IntPtr.Zero,responsive=Native.Responsive(window),bounds=Native.WindowBounds(window),appId=cfg.AppId,clips=ClipCatalog.List(root,cfg).Select(Path.GetFileName).ToArray() }));return 0;
    }
    Application app=new Application();
    if(args.Contains("--settings"))return app.Run(new SettingsWindow(root,cfg));
    bool own;using(Mutex mutex=new Mutex(true,"Local\\CodexOceanBoot",out own)) {
     if(!own)return 0;
     bool preview=args.Contains("--preview");
     IntPtr existing=Native.FindApp(cfg);
     if(!preview&&cfg.SkipWhenRunning&&existing!=IntPtr.Zero&&Native.Responsive(existing)) { Native.Focus(existing);return 0; }
     RunLog log=new RunLog(root);log.Event("started",new { version="0.1.0",preview=preview });
     string previous=null,state=Path.Combine(root,".state","last-clip.txt");try { if(File.Exists(state))previous=File.ReadAllText(state).Trim(); }catch {}
     string[] clips=ClipCatalog.List(root,cfg);
     string selected=cfg.SelectedClip;int index=Array.IndexOf(args,"--clip");
     if(index>=0) {
      if(index+1>=args.Length||Path.GetFileName(args[index+1])!=args[index+1])throw new ArgumentException("--clip requires a file name from media/.");
      selected=args[index+1];if(!clips.Any(p=>String.Equals(Path.GetFileName(p),selected,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Selected clip is not enabled or is missing.");
     }
     string video=new ClipCatalog(new Random()).Pick(clips,index<0&&cfg.RandomPlayback,selected,previous);
     if(!preview&&video!=null) { try { AtomicFile.Write(state,Path.GetFileName(video)); }catch(Exception e) { log.Event("state-unavailable",new { message=e.Message }); } }
     app.DispatcherUnhandledException+=delegate(object sender,System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
      e.Handled=true;log.Event("unhandled-error",new { message=e.Exception.Message });Native.Focus(Native.FindApp(cfg));app.Shutdown(1);
     };
     return app.Run(new BootWindow(cfg,video,preview,log));
    }
   }catch(Exception e) { MessageBox.Show("启动器无法继续："+e.Message,"Codex Ocean Boot",MessageBoxButton.OK,MessageBoxImage.Error);return 1; }
  }
 }
}
