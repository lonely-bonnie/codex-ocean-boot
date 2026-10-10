using System;
using System.IO;
using System.Linq;
using CodexOceanBoot;

internal static class CoreTests {
 private static int checks;
 private static void Check(bool value,string message) { if(!value)throw new Exception(message);checks++; }
 private static Config Defaults() { return new Config(); }
 private static void Reject(Action<Config> mutation) { Config cfg=Defaults();mutation(cfg);bool rejected=false;try { cfg.Validate(); }catch(ArgumentException) { rejected=true; }Check(rejected,"Invalid configuration accepted."); }
 public static int Main(string[] args) {
  try {
   Config c=Defaults();c.Validate();
   Reject(x=>x.AppId="app!App extra");Reject(x=>x.Volume=Double.NaN);Reject(x=>x.Volume=1.1);
   Reject(x=>x.FadeSeconds=Double.PositiveInfinity);Reject(x=>x.FadeSeconds=0);
   Reject(x=>x.SkipFadeSeconds=0);Reject(x=>x.MaxWaitSeconds=4);Reject(x=>x.MediaTimeoutSeconds=121);
   Reject(x=>x.MediaDirectory="..\\outside");Reject(x=>x.MediaDirectory="C:\\outside");
   Reject(x=>x.ProcessNames=new string[]{"ChatGPT.exe --bad"});Reject(x=>x.SelectedClip="..\\other.mp4");
   PlaybackGate fast=new PlaybackGate(c);
   fast.Step(1,1,8.06,false,true,true);Check(fast.Phase==Phase.Playing,"Early-ready must preserve the full clip.");
   fast.Step(6.05,6.05,8.06,false,true,true);Check(fast.Phase==Phase.Playing,"Tail must not start early.");
   fast.Step(6.1,6.1,8.06,false,true,true);Check(fast.Phase==Phase.Fading&&fast.Reason=="tail","Tail threshold was missed.");
   fast.Step(7.08,7.08,8.06,false,true,true);Check(Math.Abs(fast.Progress-.5)<.001,"Tail progress must follow media time.");
   fast.Step(8,7.08,8.06,false,true,true);Check(Math.Abs(fast.Progress-.5)<.001,"Stalled media must not advance a tail fade by wall time.");
   fast.Step(8.06,8.06,8.06,true,true,true);Check(fast.Phase==Phase.Complete,"Tail must end with the video.");
   PlaybackGate tailSkip=new PlaybackGate(c);tailSkip.Step(6,6,8,false,true,true);tailSkip.Step(7,7,8,false,true,true);tailSkip.Exit(7,"skip");Check(Math.Abs(tailSkip.Progress-.5)<.001,"Skipping mid-tail must not flash the video back or raise the volume.");tailSkip.Step(7.125,7.125,8,false,true,true);Check(tailSkip.Progress>.5,"Accelerated skip must continue from the current opacity.");tailSkip.Step(7.3,7.3,8,false,true,true);Check(tailSkip.Phase==Phase.Complete,"Mid-tail skip must finish quickly.");
   PlaybackGate late=new PlaybackGate(c);late.Step(8.06,8.06,8.06,true,true,false);Check(late.Phase==Phase.Waiting,"Ended clip must wait for window.");
   late.Step(10,8.06,8.06,true,true,true);Check(late.Phase==Phase.Fading&&late.Reason=="late-window","Late readiness must fade held last frame.");
   late.Step(11,8.06,8.06,true,true,true);Check(Math.Abs(late.Progress-.5)<.001,"Held-frame fade must use wall time.");
   late.Step(12,8.06,8.06,true,true,true);Check(late.Phase==Phase.Complete,"Late handoff must finish.");
   PlaybackGate shortClip=new PlaybackGate(c);shortClip.Step(1,1,3,false,true,true);Check(shortClip.Phase==Phase.Playing,"Short clip must not vanish before its content.");
   shortClip.Step(3,3,3,true,true,true);Check(shortClip.Reason=="late-window","Short clip must fade after end.");
   PlaybackGate skip=new PlaybackGate(c);skip.Exit(1,"skip");skip.Step(1.3,1,8,false,true,false);Check(skip.Phase==Phase.Complete&&skip.Reason=="skip","Skip must release even without a window.");
   PlaybackGate deadMedia=new PlaybackGate(c);deadMedia.Step(6,0,0,false,false,true);Check(deadMedia.Reason=="media-timeout","Metadata-only media must be bounded.");
   deadMedia.Step(6.3,0,0,false,false,true);Check(deadMedia.Phase==Phase.Complete,"Media timeout must release.");
   PlaybackGate slow=new PlaybackGate(c);slow.Step(30,15,15,true,true,false);Check(slow.Reason=="timeout","Window wait must be bounded.");slow.Step(30.3,15,15,true,true,false);Check(slow.Phase==Phase.Complete,"Window timeout must release.");
   PlaybackGate unknown=new PlaybackGate(c);unknown.Step(20,20,0,false,true,true);Check(unknown.Phase==Phase.Playing,"Unknown duration must not be guessed.");unknown.Step(30,30,0,false,true,true);Check(unknown.Reason=="timeout","Unknown duration must remain bounded.");
   PlaybackGate lost=new PlaybackGate(c);lost.Step(6.5,6.5,8,false,true,true);lost.Exit(6.6,"target-lost");lost.Step(6.9,6.9,8,false,true,false);Check(lost.Phase==Phase.Complete,"Lost target must release.");
   string[] clips={"one.mp4","two.mp4","three.mp4"};ClipCatalog chooser=new ClipCatalog(new Random(42));
   string previous="one.mp4";for(int i=0;i<100;i++) { string chosen=chooser.Pick(clips,true,null,previous);Check(chosen!=previous,"Random rotation repeated the last clip.");previous=chosen; }
   Check(chooser.Pick(new string[0],true,null,null)==null,"Empty pool must be supported.");
   Check(chooser.Pick(new string[]{"one.mp4"},true,null,"one.mp4")=="one.mp4","Single clip must remain playable.");
   Check(chooser.Pick(clips,false,"two.mp4",null)=="two.mp4","Fixed selection was ignored.");
   foreach(Bounds area in new Bounds[]{new Bounds(0,0,1366,728),new Bounds(-3840,0,3840,2120),new Bounds(1920,-1440,2560,1400),new Bounds(0,0,900,1360)}) {
    Bounds player=Geometry.FitPlayer(area,1600,900);Check(player.Left>=area.Left&&player.Top>=area.Top&&player.Right<=area.Right&&player.Bottom<=area.Bottom,"Player escaped the work area.");
    Bounds destination=new Bounds(area.Left+10,area.Top+20,800,600);
    for(int i=0;i<=100;i++) { Bounds interpolated=Geometry.Lerp(player,destination,Geometry.Ease(i/100.0));Check(interpolated.Width>0&&interpolated.Height>0,"Invalid interpolated bounds."); }
    Bounds final=Geometry.Lerp(player,destination,1);Check(final.Left==destination.Left&&final.Top==destination.Top&&final.Width==destination.Width&&final.Height==destination.Height,"Handoff bounds must converge exactly.");
   }
   string scratch=Path.Combine(args[0],".local","tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path.Combine(scratch,"media"));
   File.WriteAllText(Path.Combine(scratch,"media","a.mp4"),"");File.WriteAllText(Path.Combine(scratch,"media","b.mp4"),"");File.WriteAllText(Path.Combine(scratch,"media","ignore.txt"),"");
   c.DisabledClips=new string[]{"a.mp4"};Check(ClipCatalog.List(scratch,c).Select(Path.GetFileName).SequenceEqual(new string[]{"b.mp4"}),"Disabled or unrelated files entered the pool.");
   c.Save(scratch);c.Volume=.2;c.Save(scratch);Check(Config.Load(scratch).Volume==.2,"Atomic replacement must preserve the saved configuration.");
   Console.WriteLine("PASS: "+checks+" assertions (configuration, lifecycle, media-time fade, rotation, geometry and persistence).");return 0;
  }catch(Exception e) { Console.Error.WriteLine(e);return 1; }
 }
}
