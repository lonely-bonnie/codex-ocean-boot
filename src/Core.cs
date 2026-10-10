using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace CodexOceanBoot {
 public sealed class Config {
  public string AppId { get; set; }
  public string ProcessPathContains { get; set; }
  public string[] ProcessNames { get; set; }
  public string MediaDirectory { get; set; }
  public string[] DisabledClips { get; set; }
  public string SelectedClip { get; set; }
  public bool RandomPlayback { get; set; }
  public bool SkipWhenRunning { get; set; }
  public bool Sound { get; set; }
  public bool ShowHint { get; set; }
  public double Volume { get; set; }
  public double FadeSeconds { get; set; }
  public double SkipFadeSeconds { get; set; }
  public double MaxWaitSeconds { get; set; }
  public double MediaTimeoutSeconds { get; set; }
  public int PlayerWidth { get; set; }
  public int PlayerHeight { get; set; }
  public Config() {
   AppId="OpenAI.Codex_2p2nqsd0c76g0!App";ProcessPathContains="OpenAI.Codex_";
   ProcessNames=new string[]{"ChatGPT","Codex"};MediaDirectory="media";
   DisabledClips=new string[0];SelectedClip="1.mp4";RandomPlayback=true;
   Sound=true;ShowHint=true;Volume=.35;FadeSeconds=2;SkipFadeSeconds=.25;
   MaxWaitSeconds=30;MediaTimeoutSeconds=6;PlayerWidth=1600;PlayerHeight=900;
  }
  public void Validate() {
   if(!System.Text.RegularExpressions.Regex.IsMatch(AppId??"",@"^[A-Za-z0-9_.-]+![A-Za-z0-9_.-]+$"))throw new ArgumentException("AppId must be an AppsFolder application ID.");
   if(String.IsNullOrWhiteSpace(ProcessPathContains)||ProcessNames==null||ProcessNames.Length==0||ProcessNames.Any(n=>!System.Text.RegularExpressions.Regex.IsMatch(n??"",@"^[A-Za-z0-9_.-]+$")))throw new ArgumentException("Invalid process matching configuration.");
   if(String.IsNullOrWhiteSpace(MediaDirectory)||Path.IsPathRooted(MediaDirectory)||MediaDirectory.Split('\\','/').Any(p=>p==".."))throw new ArgumentException("MediaDirectory must remain inside the launcher folder.");
   if(!Finite(Volume)||Volume<0||Volume>1||!Finite(FadeSeconds)||FadeSeconds<.12||FadeSeconds>5||!Finite(SkipFadeSeconds)||SkipFadeSeconds<.12||SkipFadeSeconds>1)throw new ArgumentException("Invalid volume or fade duration.");
   if(!Finite(MaxWaitSeconds)||MaxWaitSeconds<5||MaxWaitSeconds>120||!Finite(MediaTimeoutSeconds)||MediaTimeoutSeconds<1||MediaTimeoutSeconds>MaxWaitSeconds)throw new ArgumentException("Invalid timeout.");
   if(PlayerWidth<450||PlayerWidth>7680||PlayerHeight<280||PlayerHeight>4320)throw new ArgumentException("Invalid player size.");
   if(!String.IsNullOrEmpty(SelectedClip)&&Path.GetFileName(SelectedClip)!=SelectedClip)throw new ArgumentException("SelectedClip must be a file name.");
   if(DisabledClips==null)DisabledClips=new string[0];
  }
  private static bool Finite(double d) { return !Double.IsNaN(d)&&!Double.IsInfinity(d); }
  public static Config Load(string root) {
   string file=Path.Combine(root,"launcher.json");
   Config cfg=File.Exists(file)?new JavaScriptSerializer().Deserialize<Config>(File.ReadAllText(file)):new Config();
   if(cfg==null)throw new ArgumentException("Empty configuration.");cfg.Validate();return cfg;
  }
  public void Save(string root) {
   Validate();AtomicFile.Write(Path.Combine(root,"launcher.json"),new JavaScriptSerializer().Serialize(this));
  }
 }
 public static class AtomicFile {
  public static void Write(string path,string text) {
   Directory.CreateDirectory(Path.GetDirectoryName(path));
   string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try {
    File.WriteAllText(temporary,text,new System.Text.UTF8Encoding(false));
    if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
   } finally { if(File.Exists(temporary))File.Delete(temporary); }
  }
 }
 public sealed class ClipCatalog {
  private readonly Random random;
  public ClipCatalog(Random random) { this.random=random; }
  public static string[] List(string root,Config cfg) {
   string dir=Path.GetFullPath(Path.Combine(root,cfg.MediaDirectory));
   if(!Directory.Exists(dir))return new string[0];
   return Directory.GetFiles(dir).Where(p=>new string[]{".mp4",".m4v",".mov"}.Contains(Path.GetExtension(p).ToLowerInvariant())&&!cfg.DisabledClips.Contains(Path.GetFileName(p),StringComparer.OrdinalIgnoreCase)).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).ToArray();
  }
  public string Pick(string[] clips,bool shuffle,string selected,string previous) {
   if(clips.Length==0)return null;
   if(!shuffle) { string fixedClip=clips.FirstOrDefault(p=>String.Equals(Path.GetFileName(p),selected,StringComparison.OrdinalIgnoreCase));return fixedClip??clips[0]; }
   string[] pool=clips.Where(p=>!String.Equals(Path.GetFileName(p),previous,StringComparison.OrdinalIgnoreCase)).ToArray();
   if(pool.Length==0)pool=clips;return pool[random.Next(pool.Length)];
  }
 }
 public enum Phase { Playing,Waiting,Fading,Complete }
 public sealed class PlaybackGate {
  private readonly Config cfg;
  private bool tail;
  private double fadeStart,fadeSpan,fadeFrom;
  public Phase Phase { get; private set; }
  public double Progress { get; private set; }
  public string Reason { get; private set; }
  public PlaybackGate(Config cfg) { this.cfg=cfg;Phase=Phase.Playing;Reason="tail"; }
  public void Exit(double wall,string reason) { if(Phase==Phase.Complete)return;fadeFrom=Phase==Phase.Fading?Progress:0;tail=false;fadeStart=wall;fadeSpan=cfg.SkipFadeSeconds;Progress=fadeFrom;Phase=Phase.Fading;Reason=reason; }
  public void Step(double wall,double position,double duration,bool ended,bool frameAdvanced,bool windowReady) {
   if(Phase==Phase.Complete)return;
   if(wall>=cfg.MaxWaitSeconds&&Reason!="timeout")Exit(wall,"timeout");
   if(Phase==Phase.Fading) {
    Progress=fadeFrom+(1-fadeFrom)*Geometry.Clamp(((tail?position:wall)-fadeStart)/fadeSpan);
    if(tail&&ended)Progress=1;
    if(Progress>=1)Phase=Phase.Complete;return;
   }
   if(!frameAdvanced&&wall>=cfg.MediaTimeoutSeconds) { Exit(wall,"media-timeout");return; }
   if(ended) {
    if(!windowReady) { Phase=Phase.Waiting;return; }
    tail=false;fadeStart=wall;fadeSpan=cfg.FadeSeconds;Phase=Phase.Fading;Reason="late-window";return;
   }
   if(windowReady&&duration>0&&position>0&&duration-position<=cfg.FadeSeconds&&duration>=2*cfg.FadeSeconds) {
    tail=true;fadeStart=position;fadeSpan=Math.Max(.12,duration-position);Phase=Phase.Fading;Reason="tail";
   }
   // Short clips play completely, then use the ended path. Unknown duration is bounded by the wall timeout.
  }
 }
 public struct Bounds {
  public int Left,Top,Width,Height;
  public Bounds(int x,int y,int w,int h) { Left=x;Top=y;Width=w;Height=h; }
  public int Right { get { return Left+Width; } }
  public int Bottom { get { return Top+Height; } }
 }
 public static class Geometry {
  public static double Clamp(double n) { return Math.Max(0,Math.Min(1,n)); }
  public static double Ease(double n) { n=Clamp(n);return n*n*(3-2*n); }
  public static Bounds Lerp(Bounds a,Bounds b,double t) { return new Bounds((int)Math.Round(a.Left+(b.Left-a.Left)*t),(int)Math.Round(a.Top+(b.Top-a.Top)*t),Math.Max(1,(int)Math.Round(a.Width+(b.Width-a.Width)*t)),Math.Max(1,(int)Math.Round(a.Height+(b.Height-a.Height)*t))); }
  public static Bounds FitPlayer(Bounds area,int width,int height) {
   double scale=Math.Min(1,Math.Min(area.Width/(double)width,area.Height/(double)height));
   int w=Math.Max(1,(int)Math.Round(width*scale)),h=Math.Max(1,(int)Math.Round(height*scale));
   return new Bounds(area.Left+(area.Width-w)/2,area.Top+(area.Height-h)/2,w,h);
  }
 }
}
