using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace CodexOceanBoot {
 internal sealed class BootWindow:Window {
  private readonly Config cfg;
  private readonly RunLog log;
  private readonly bool preview;
  private readonly PlaybackGate gate;
  private readonly Stopwatch clock=new Stopwatch();
  private readonly MediaElement media;
  private readonly TextBlock hint;
  private readonly Border movingBar;
  private readonly Grid rootGrid;
  private readonly DispatcherTimer timer;
  private IntPtr host,target,stableTarget,thumbnail;
  private Bounds initialBounds,fadeBounds,beforeBounds;
  private bool ready,ended,frameAdvanced,transitionStarted,closing,dwmFailed,muted,midpointLogged;
  private double stableAt,lastProbe=-1,duration;
  private readonly string clip;
  internal BootWindow(Config config,string video,bool isPreview,RunLog run) {
   cfg=config;clip=video;preview=isPreview;log=run;gate=new PlaybackGate(cfg);
   Title="Codex Ocean Boot";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;
   ShowInTaskbar=true;Topmost=true;Background=Brushes.Black;WindowStartupLocation=WindowStartupLocation.Manual;
   target=Native.FindApp(cfg);
   if(target!=IntPtr.Zero&&!preview&&Native.IsIconic(target))Native.ShowWindow(target,9);
   System.Windows.Forms.Screen monitor=target!=IntPtr.Zero?System.Windows.Forms.Screen.FromHandle(target):System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
   System.Drawing.Rectangle area=monitor.WorkingArea;
   initialBounds=target!=IntPtr.Zero&&!Native.IsIconic(target)?Native.WindowBounds(target):Geometry.FitPlayer(new Bounds(area.Left,area.Top,area.Width,area.Height),cfg.PlayerWidth,cfg.PlayerHeight);
   beforeBounds=target!=IntPtr.Zero?Native.WindowBounds(target):new Bounds();
   rootGrid=new Grid { Background=SeaBrush() };
   media=new MediaElement { LoadedBehavior=MediaState.Manual,UnloadedBehavior=MediaState.Manual,ScrubbingEnabled=true,Stretch=Stretch.Uniform,Opacity=0,Volume=cfg.Sound?cfg.Volume:0 };
   media.OpacityMask=new RadialGradientBrush { GradientOrigin=new Point(.5,.5),Center=new Point(.5,.5),RadiusX=.65,RadiusY=.65,GradientStops=new GradientStopCollection { new GradientStop(Colors.White,0),new GradientStop(Colors.White,.65),new GradientStop(Colors.Transparent,1) } };
   rootGrid.Children.Add(media);
   StackPanel footer=new StackPanel { VerticalAlignment=VerticalAlignment.Bottom,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(24,0,24,28) };
   footer.Children.Add(new TextBlock { Text="C O D E X   /   O C E A N",FontSize=12,Foreground=Brush("#9FC4E6"),HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,10) });
   Grid bar=new Grid { Width=280,Height=2,Background=Brush("#354B62"),ClipToBounds=true };
   movingBar=new Border { Width=70,Height=2,HorizontalAlignment=HorizontalAlignment.Left,Background=new LinearGradientBrush(Color.FromRgb(79,157,217),Color.FromRgb(143,216,255),0) };
   bar.Children.Add(movingBar);footer.Children.Add(bar);
   hint=new TextBlock { Text=preview?"预览中 · Esc 跳过":"正在打开 · Esc 跳过",FontSize=12,Foreground=Brush("#8FB3D4"),HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,10,0,0),Visibility=cfg.ShowHint?Visibility.Visible:Visibility.Collapsed };
   footer.Children.Add(hint);rootGrid.Children.Add(footer);
   Button skip=ControlButton("跳过  Esc");skip.HorizontalAlignment=HorizontalAlignment.Right;skip.VerticalAlignment=VerticalAlignment.Top;skip.Margin=new Thickness(16);skip.Click+=delegate { RequestExit("skip"); };rootGrid.Children.Add(skip);
   Button sound=ControlButton(cfg.Sound?"声音 开":"声音 关");muted=!cfg.Sound;sound.HorizontalAlignment=HorizontalAlignment.Right;sound.VerticalAlignment=VerticalAlignment.Bottom;sound.Margin=new Thickness(16);sound.Click+=delegate { muted=!muted;sound.Content=muted?"声音 关":"声音 开";media.Volume=muted?0:cfg.Volume; };rootGrid.Children.Add(sound);
   Content=rootGrid;
   PreviewKeyDown+=delegate(object s,KeyEventArgs e) { if(e.Key==Key.Escape||e.Key==Key.Space||e.Key==Key.Enter) { e.Handled=true;RequestExit("skip"); } };
   rootGrid.MouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e) { if(e.OriginalSource is MediaElement||e.OriginalSource==rootGrid)RequestExit("skip"); };
   SourceInitialized+=delegate {
    host=new WindowInteropHelper(this).Handle;int corner=1;Native.DwmSetWindowAttribute(host,33,ref corner,4);
    PositionPlayer(initialBounds);log.Event("player-created",new { bounds=initialBounds,targetFound=target!=IntPtr.Zero });
   };
   Loaded+=delegate {
    clock.Start();Activate();timer.Start();
    if(target==IntPtr.Zero&&!preview) { try { Native.Launch(cfg);log.Event("app-launch-requested"); }catch(Exception e) { log.Event("app-launch-failed",new { message=e.Message });RequestExit("launch-error"); } }
    if(String.IsNullOrEmpty(clip)) { RequestExit("no-media");return; }
    log.Event("media-selected",new { file=Path.GetFileName(clip),preview=preview });media.Source=new Uri(clip);media.Play();
   };
   media.MediaOpened+=delegate { duration=media.NaturalDuration.HasTimeSpan?media.NaturalDuration.TimeSpan.TotalSeconds:0;log.Event("media-opened",new { duration=duration,width=media.NaturalVideoWidth,height=media.NaturalVideoHeight }); };
   media.MediaEnded+=delegate { ended=true;log.Event("media-ended"); };
   media.MediaFailed+=delegate(object s,ExceptionRoutedEventArgs e) { log.Event("media-failed",new { message=e.ErrorException.Message });RequestExit("media-error"); };
   timer=new DispatcherTimer(DispatcherPriority.Render) { Interval=TimeSpan.FromMilliseconds(16) };timer.Tick+=Tick;
   Closed+=delegate {
    timer.Stop();media.Stop();DropThumbnail();
    IntPtr current=target!=IntPtr.Zero&&Native.IsWindow(target)?target:Native.FindApp(cfg);
    Bounds after=current!=IntPtr.Zero?Native.WindowBounds(current):new Bounds();bool focus=preview?false:Native.Focus(current);
    log.Event("released",new { reason=gate.Reason,frameAdvanced=frameAdvanced,dwmFailed=dwmFailed,targetFound=current!=IntPtr.Zero,focusRequested=focus,foregroundMatches=current!=IntPtr.Zero&&Native.GetForegroundWindow()==current,beforeBounds=beforeBounds,afterBounds=after,elapsed=clock.Elapsed.TotalSeconds });
   };
  }
  private static Brush Brush(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
  private static Brush SeaBrush() { return new RadialGradientBrush { Center=new Point(.5,.42),GradientOrigin=new Point(.5,.42),RadiusX=.7,RadiusY=.75,GradientStops=new GradientStopCollection { new GradientStop(Color.FromRgb(11,27,46),0),new GradientStop(Color.FromRgb(5,11,20),.62),new GradientStop(Colors.Black,1) } }; }
  private static Button ControlButton(string title) { return new Button { Content=title,Padding=new Thickness(12,7,12,7),FontSize=12,Foreground=Brush("#CFE4F6"),Background=Brush("#142C46"),BorderBrush=Brush("#354B62"),Opacity=.7,Focusable=false }; }
  private void PositionPlayer(Bounds bounds) { Native.SetWindowPos(host,new IntPtr(-1),bounds.Left,bounds.Top,bounds.Width,bounds.Height,0x10); }
  private void Probe() {
   double now=clock.Elapsed.TotalSeconds;if(now-lastProbe<.25)return;lastProbe=now;
   IntPtr candidate=transitionStarted?target:Native.FindApp(cfg);
   if(candidate==IntPtr.Zero||!Native.IsWindow(candidate)||!Native.IsWindowVisible(candidate)||Native.IsIconic(candidate)||!Native.Responsive(candidate)) { ready=false;stableTarget=IntPtr.Zero;return; }
   if(candidate!=stableTarget) { stableTarget=candidate;stableAt=now;ready=false; }else ready=now-stableAt>=.6;
   if(target==IntPtr.Zero)beforeBounds=Native.WindowBounds(candidate);target=candidate;
  }
  private void RequestExit(string reason) { if(closing||gate.Phase==Phase.Complete)return;gate.Exit(clock.Elapsed.TotalSeconds,reason); }
  private void StartTransition() {
   transitionStarted=true;fadeBounds=target!=IntPtr.Zero&&Native.IsWindow(target)&&!Native.IsIconic(target)?Native.WindowBounds(target):initialBounds;
   if(target!=IntPtr.Zero&&ready) {
    int result=Native.DwmRegisterThumbnail(host,target,out thumbnail);dwmFailed=result!=0;
    log.Event("dwm-connected",new { success=!dwmFailed,hresult=result });
   }else dwmFailed=true;
   log.Event("transition-started",new { reason=gate.Reason,position=media.Position.TotalSeconds,duration=duration,destination=fadeBounds,windowReady=ready });
  }
  private void Tick(object sender,EventArgs args) {
   if(closing)return;Probe();double wall=clock.Elapsed.TotalSeconds,position=media.Position.TotalSeconds;
   if(!frameAdvanced&&position>.05&&media.NaturalVideoWidth>0) { frameAdvanced=true;log.Event("media-advanced",new { position=position }); }
   if(!transitionStarted) {
    media.Opacity=frameAdvanced?Geometry.Ease(Math.Min(1,position/.8)):0;
    movingBar.RenderTransform=new TranslateTransform((Math.Sin(wall*2.5)+1)*105,0);
    hint.Text=preview?"预览中 · Esc 跳过":ready?"即将进入 · Esc 跳过":ended?"等待窗口 · Esc 跳过":"正在打开 · Esc 跳过";
   }
   if(transitionStarted&&target!=IntPtr.Zero&&(!Native.IsWindow(target)||Native.IsIconic(target))) { DropThumbnail();dwmFailed=true;target=IntPtr.Zero;gate.Exit(wall,"target-lost"); }
   gate.Step(wall,position,duration,ended,frameAdvanced,ready||preview);
   if(gate.Phase==Phase.Fading||gate.Phase==Phase.Complete) {
    if(!transitionStarted)StartTransition();double alpha=Geometry.Ease(gate.Progress);
    // Only the player follows the target. The application itself is never moved or resized.
    if(target!=IntPtr.Zero&&Native.IsWindow(target))fadeBounds=Native.WindowBounds(target);
    PositionPlayer(Geometry.Lerp(initialBounds,fadeBounds,alpha));media.Volume=muted?0:cfg.Volume*(1-alpha);
    if(!midpointLogged&&gate.Progress>=.5) { midpointLogged=true;log.Event("transition-midpoint",new { progress=gate.Progress,volume=media.Volume }); }
    if(thumbnail!=IntPtr.Zero) {
     Bounds size=Native.ClientSize(host);Native.Thumbnail props=new Native.Thumbnail { Flags=1|4|8|16,Destination=new Native.Rect { Left=0,Top=0,Right=size.Width,Bottom=size.Height },Opacity=(byte)Math.Round(255*alpha),Visible=true,ClientOnly=false };
     int result=Native.DwmUpdateThumbnailProperties(thumbnail,ref props);
     if(result!=0) { log.Event("dwm-update-failed",new { hresult=result });DropThumbnail();dwmFailed=true; }
    }
    if(dwmFailed)Opacity=1-alpha;
   }
   if(gate.Phase==Phase.Complete) { closing=true;Close(); }
  }
  private void DropThumbnail() { if(thumbnail!=IntPtr.Zero) { Native.DwmUnregisterThumbnail(thumbnail);thumbnail=IntPtr.Zero; } }
 }
}
