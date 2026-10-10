using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CodexOceanBoot {
 internal sealed class SettingsWindow:Window {
  internal SettingsWindow(string root,Config cfg) {
   Title="Codex Ocean Boot · 设置";Width=500;Height=570;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterScreen;
   StackPanel content=new StackPanel { Margin=new Thickness(30) };Content=content;
   content.Children.Add(new TextBlock { Text="启动动画",FontSize=26,Margin=new Thickness(0,0,0,8) });
   content.Children.Add(new TextBlock { Text="鲸鱼娘短片 · 片尾溶解进入 Codex",Margin=new Thickness(0,0,0,22) });
   string[] videos=Directory.Exists(Path.Combine(root,cfg.MediaDirectory))?Directory.GetFiles(Path.Combine(root,cfg.MediaDirectory)).Where(p=>new string[]{".mp4",".mov",".m4v"}.Contains(Path.GetExtension(p).ToLowerInvariant())).Select(Path.GetFileName).OrderBy(p=>p).ToArray():new string[0];
   content.Children.Add(new TextBlock { Text="固定播放 / 预览的短片",Margin=new Thickness(0,0,0,6) });
   ComboBox selection=new ComboBox { ItemsSource=videos,SelectedItem=cfg.SelectedClip,Height=30,Margin=new Thickness(0,0,0,12) };if(selection.SelectedIndex<0&&videos.Length>0)selection.SelectedIndex=0;content.Children.Add(selection);
   CheckBox random=Check("随机播放，避免连续重复",cfg.RandomPlayback);content.Children.Add(random);
   CheckBox skip=Check("Codex 已运行时直接进入",cfg.SkipWhenRunning);content.Children.Add(skip);
   CheckBox sound=Check("播放声音",cfg.Sound);content.Children.Add(sound);
   CheckBox hint=Check("显示底部提示",cfg.ShowHint);content.Children.Add(hint);
   content.Children.Add(new TextBlock { Text="音量",Margin=new Thickness(0,10,0,4) });
   Slider volume=new Slider { Minimum=0,Maximum=1,Value=cfg.Volume,TickFrequency=.05,IsSnapToTickEnabled=true,Margin=new Thickness(0,0,0,12) };content.Children.Add(volume);
   content.Children.Add(new TextBlock { Text="片尾溶解时长（秒）",Margin=new Thickness(0,0,0,6) });
   ComboBox fade=new ComboBox { ItemsSource=new double[]{1,1.5,2,3,4},SelectedItem=cfg.FadeSeconds,Height=30 };if(fade.SelectedIndex<0)fade.SelectedIndex=2;content.Children.Add(fade);
   TextBlock note=new TextBlock { Text="状态提示根据窗口检测显示；Esc 可随时跳过。",Margin=new Thickness(0,20,0,14),TextWrapping=TextWrapping.Wrap };content.Children.Add(note);
   StackPanel buttons=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right };
   Action save=delegate { cfg.SelectedClip=(string)selection.SelectedItem;cfg.RandomPlayback=random.IsChecked==true;cfg.SkipWhenRunning=skip.IsChecked==true;cfg.Sound=sound.IsChecked==true;cfg.ShowHint=hint.IsChecked==true;cfg.Volume=volume.Value;cfg.FadeSeconds=(double)fade.SelectedItem;cfg.Save(root); };
   Button preview=new Button { Content="预览",IsEnabled=videos.Length>0,Padding=new Thickness(18,8,18,8),Margin=new Thickness(0,0,12,0) };
   preview.Click+=delegate { try { save();Process.Start(new ProcessStartInfo(System.Reflection.Assembly.GetExecutingAssembly().Location,"--preview --clip \""+cfg.SelectedClip+"\"") { UseShellExecute=true }); }catch(Exception e) { MessageBox.Show(e.Message,"无法预览"); } };
   Button apply=new Button { Content="保存",Padding=new Thickness(18,8,18,8) };apply.Click+=delegate { try { save();Close(); }catch(Exception e) { MessageBox.Show(e.Message,"无法保存"); } };
   buttons.Children.Add(preview);buttons.Children.Add(apply);content.Children.Add(buttons);
  }
  private static CheckBox Check(string text,bool value) { return new CheckBox { Content=text,IsChecked=value,Margin=new Thickness(0,0,0,12) }; }
 }
}
