// Window discovery and DWM interop adapted from Dragon Codex Boot (MIT).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CodexOceanBoot {
 internal static class Native {
  internal delegate bool EnumWindowsProc(IntPtr h,IntPtr p);
  [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left,Top,Right,Bottom; }
  [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X,Y; }
  [StructLayout(LayoutKind.Sequential)] internal struct Size { public int Width,Height; }
  [StructLayout(LayoutKind.Sequential)] internal struct Thumbnail {
   public uint Flags;public Rect Destination,Source;public byte Opacity;
   [MarshalAs(UnmanagedType.Bool)]public bool Visible;
   [MarshalAs(UnmanagedType.Bool)]public bool ClientOnly;
  }
  [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowsProc cb,IntPtr p);
  [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr h);
  [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr h);
  [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr h,out Rect r);
  [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr h,out Rect r);
  [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32.dll")] internal static extern IntPtr GetWindow(IntPtr h,uint command);
  [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr h,int index);
  [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr h,int command);
  [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
  [DllImport("user32.dll")] internal static extern IntPtr SendMessageTimeout(IntPtr h,uint msg,IntPtr w,IntPtr l,uint flags,uint milliseconds,out IntPtr result);
  [DllImport("kernel32.dll")] private static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern bool QueryFullProcessImageName(IntPtr h,uint flags,StringBuilder path,ref int length);
  [DllImport("kernel32.dll")]private static extern bool CloseHandle(IntPtr h);
  [DllImport("dwmapi.dll")]internal static extern int DwmRegisterThumbnail(IntPtr destination,IntPtr source,out IntPtr thumbnail);
  [DllImport("dwmapi.dll")]internal static extern int DwmUnregisterThumbnail(IntPtr thumbnail);
  [DllImport("dwmapi.dll")]internal static extern int DwmUpdateThumbnailProperties(IntPtr thumbnail,ref Thumbnail properties);
  [DllImport("dwmapi.dll")]internal static extern int DwmGetWindowAttribute(IntPtr h,int attribute,out int value,int size);
  [DllImport("dwmapi.dll")]internal static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int size);
  internal static Bounds WindowBounds(IntPtr h) { Rect r;return GetWindowRect(h,out r)?new Bounds(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top):new Bounds(); }
  internal static Bounds ClientSize(IntPtr h) { Rect r;return GetClientRect(h,out r)?new Bounds(0,0,r.Right-r.Left,r.Bottom-r.Top):new Bounds(); }
  internal static bool Responsive(IntPtr h) { IntPtr result;return h!=IntPtr.Zero&&SendMessageTimeout(h,0,IntPtr.Zero,IntPtr.Zero,2,80,out result)!=IntPtr.Zero; }
  internal static IntPtr FindApp(Config cfg) {
   HashSet<int> ids=new HashSet<int>();
   foreach(string name in cfg.ProcessNames)foreach(Process p in Process.GetProcessesByName(name)) {
    try {
     IntPtr process=OpenProcess(0x1000,false,p.Id);if(process==IntPtr.Zero)continue;
     try {
      StringBuilder path=new StringBuilder(4096);int length=path.Capacity;
      if(QueryFullProcessImageName(process,0,path,ref length)&&path.ToString().IndexOf(cfg.ProcessPathContains,StringComparison.OrdinalIgnoreCase)>=0)ids.Add(p.Id);
     } finally { CloseHandle(process); }
    } catch {} finally { p.Dispose(); }
   }
   IntPtr foreground=GetForegroundWindow(),first=IntPtr.Zero;
   EnumWindows(delegate(IntPtr h,IntPtr p) {
    uint pid;GetWindowThreadProcessId(h,out pid);
    if(!ids.Contains((int)pid)||!IsWindowVisible(h)||GetWindow(h,4)!=IntPtr.Zero||(GetWindowLong(h,-20)&0x80)!=0)return true;
    int cloaked;if(DwmGetWindowAttribute(h,14,out cloaked,4)==0&&cloaked!=0)return true;
    Bounds b=WindowBounds(h);if(!IsIconic(h)&&(b.Width<450||b.Height<280))return true;
    if(first==IntPtr.Zero)first=h;if(h==foreground) { first=h;return false; }return true;
   },IntPtr.Zero);
   return first;
  }
  internal static void Launch(Config cfg) { Process.Start(new ProcessStartInfo("explorer.exe","shell:AppsFolder\\"+cfg.AppId) { UseShellExecute=true }); }
  internal static bool Focus(IntPtr h) {
   if(h==IntPtr.Zero||!IsWindow(h))return false;
   if(IsIconic(h))ShowWindow(h,9);return SetForegroundWindow(h);
  }
 }
}
