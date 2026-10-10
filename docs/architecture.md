# Architecture

## Runtime boundary

The official application is launched using its Windows AppsFolder ID. No app package, registry, PATH or existing shortcut is edited. `Native.SetWindowPos` is used only on the player's HWND. No target process is terminated.

## Components

- `Core.cs`: validated portable configuration, atomic persistence, clip selection, geometry and the playback/transition state machine. It has no desktop dependency.
- `Native.cs`: narrowly scoped process matching, main-window selection, WM_NULL responsiveness checks and DWM interop adapted from Dragon Codex Boot.
- `BootWindow.cs`: WPF media player, feathered deep-sea presentation, activity indication, input handling and native handoff.
- `SettingsWindow.cs`: a separate settings/preview UI.
- `Program.cs`: CLI modes, the single-player mutex, local run log and per-directory clip history.

## Timing

The window is probed at most every 250 ms and must remain the same responsive visible window for 600 ms. Metadata alone is not called successful playback: natural video dimensions and advancing media position are logged separately, and visible-frame validation is a native desktop check.

For ordinary clips, when `duration - position <= FadeSeconds` and the window is available, the transition captures the actual remaining span. Opacity and audio volume follow media time. Late-window and skip transitions use wall time because the final frame may be held. A total wall-clock budget bounds every path.

## Geometry and composition

When the target already exists, the player starts at the target's physical window bounds. Otherwise it fits a 16:9 player into the selected monitor's working area. At handoff only the player converges to the target. A full-window DWM thumbnail is blended over the video using the same eased transition curve as the audio fade. At completion the thumbnail is unregistered, media is stopped and the player is removed; normal launch requests focus for the real app. Preview does not request focus for the target.

Per-monitor DPI awareness is declared in the manifest. WPF layout is logical; physical HWND placement and thumbnail destinations use measured Win32 pixels. Mixed-DPI/native edge alignment still needs a real desktop acceptance run.

## Failure boundaries

Empty pool, decode failure, non-advancing media, disappeared/minimized target, stalled tail and total timeout are bounded. DWM failures use a player-opacity fade. A preview can finish without an available application. Settings or log write failure does not justify editing other directories or introducing credentials.
