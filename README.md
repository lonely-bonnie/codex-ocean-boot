# Codex Ocean Boot

A portable Windows launcher that plays the whale-girl clips from **dsh-boot-animation**, then dissolves into the real Codex desktop window. It combines a native WPF player with DWM live-window handoff inspired by **Dragon Codex Boot**.

独立 Windows 启动器：鲸鱼娘短片、深海渐变背景、多片随机、片尾溶解、声音同步淡出。影片结束后，操作交给真实 Codex 窗口。

## Use

Windows 10/11 x64, .NET Framework 4.8 or newer, native Windows media support, and an installed Codex desktop app are required. The default application ID is `OpenAI.Codex_2p2nqsd0c76g0!App`; adjust `launcher.json` if your installation differs.

In an extracted package:

- `Start.cmd` or `CodexOceanBoot.exe`: play an animation and open/focus Codex.
- `Preview.cmd`: preview without launching Codex. An available existing window can still be used for the live dissolve.
- `Settings.cmd`: choose random/fixed playback, volume, sound, hints, fade duration, and whether an already-running app should skip the animation.
- Esc, Space, Enter, the skip button, or a click on the video skips the animation.

There is no installer, administrator requirement, service, network request, whole-disk scan or automatic shortcut replacement. Settings, clip history and diagnostics remain beside the executable. The launcher preserves the target window's size and position; minimized windows are restored when opening the app normally.

## Build and verify

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package.ps1
```

The Windows .NET Framework compiler is used directly. There is no Node, Python, NuGet or API-key runtime dependency. Output: `build/CodexOceanBoot/`. Packages: `dist/`.

The tests cover configuration validation, early/late window readiness, short clips, stalled playback, missing frames, skips, bounded waits, recent-clip avoidance, atomic settings and negative-monitor geometry. Pure tests do not prove native video output, actual audio output or DWM appearance; those require a desktop run.

## Behavior

- The video and application launch run concurrently.
- If a responsive, visible, stable window exists, the final configured seconds of video dissolve into a live view of that window.
- Tail fades follow the media position, so a playback stall cannot race the visual transition ahead using wall time.
- A short video plays fully before fading. If Codex starts late, the last frame waits and then fades.
- The window check is a heuristic, not an official Codex content-ready event. The small animated bar is activity, not a loading percentage.
- Bad/missing media and total timeout release the animation; the target app continues its own startup.
- DWM failure uses a whole-player fade as fallback. The launcher then exits, leaving no overlay or background process.

## Configuration and media

The portable `launcher.json` starts from `config/launcher.example.json`. The settings window covers everyday options. Advanced options include application matching, time budgets, media exclusions and the cold-start player dimensions. `DisabledClips` excludes file names from the pool. Files under `media/` may be `.mp4`, `.m4v` or `.mov`; use H.264 + AAC MP4 for compatibility.

The three unchanged upstream clips are 1280 × 720, approximately 8.06, 15.10 and 12.42 seconds. Their source and hashes are in `media/manifest.json`. Any logo/text baked into footage remains part of that footage; the launcher does not automatically edit videos.

Run `CodexOceanBoot.exe --diagnostics` for a read-only local `qa/diagnostics.json`. Runtime events are in `logs/run-*.jsonl`; the launcher records media names, timings, errors and window bounds, not chat text or screenshots. Logs, state, QA captures and local tools are excluded from Git and distributable ZIPs.

## Development

See [architecture](docs/architecture.md) and [validation](docs/validation.md). Releases must use an explicit clean package, not a ZIP of the working build directory. A source checkout's root CMD files are package templates; run them from the build directory after compilation.

MIT. Preserve [upstream notices](THIRD_PARTY_NOTICES.md) and `licenses/` when redistributing.
