# Validation

## Automated

`scripts/test.ps1`: compile using the native x64 .NET Framework compiler; run lifecycle/configuration/rotation/geometry/persistence assertions; check target-mutation invariants and the three upstream media hashes.

## Desktop acceptance matrix

Record native validation separately from automated tests:

- Each of the three clips actually shows moving decoded frames.
- Tail dissolve shows the live target window; source and destination dimensions converge without moving the target.
- Normal launch hands foreground focus to the target; preview does not deliberately focus it.
- Skip, mute and the settings controls work.
- Bad/missing media releases the player.
- Cold startup, an already-open window, minimized window, unavailable app, negative-coordinate monitor and mixed DPI.
- Listen to the real audio fade; a volume property or metadata is not proof of audible output.

Runtime measurements are local in `build/CodexOceanBoot/logs/`. No user desktop/chat captures or personal window coordinates belong in the public repository. Keep unmeasured cases explicit.

## Recorded acceptance, 2026-10-10

| Check | Result |
| --- | --- |
| Native x64 compilation | Passed on Windows 11 with .NET Framework |
| Pure tests | 552 assertions passed |
| Upstream assets | All three SHA-256 hashes matched |
| Visible video | Native player screenshots showed decoded frames from all three clips |
| Real-window dissolve | Native screenshot confirmed video blending into the existing real Codex window |
| Normal handoff | Foreground HWND matched the target after release; target bounds before/after were identical |
| Skip input | Native Esc input released the player; mid-tail continuity has a regression test |
| Bad media | A deliberately invalid MP4 released the preview in about 0.42 seconds |
| Audio ramp | Runtime volume fell from 0.35 to about 0.17 at midpoint; actual audible output still needs listening confirmation |
| Package | Explicit clean ZIP contained only executable, default config, entry templates, docs, licenses and videos |
| Cold startup / minimized / mixed DPI / multiple monitors | Not yet exercised on a live target; geometry and timing tests cover the model only |

Screenshots that contained the real conversation were observed locally and are not included in this repository or the package.
