# Project instructions

- Keep this a portable native Windows launcher. Do not change global Python, Node, Codex, PATH, registry, official app files, or existing shortcuts.
- Never terminate, move, or resize the target application's window. Only the player may be moved.
- Preserve MIT notices for upstream code and video assets. No credentials, personal machine paths, screenshots of chats, runtime state, or logs in Git.
- Window readiness means visible, responsive and stable; never claim an official content-ready signal or show invented loading percentages.
- Source and scripts use UTF-8; read/write text explicitly as UTF-8. PowerShell scripts remain ASCII for Windows PowerShell 5.1 compatibility.
- Build with scripts/build.ps1 and run scripts/test.ps1 after logic changes. Runtime/native checks must be reported separately from pure tests.
- Keep the public docs honest about tested and untested behavior. Do not add automatic shortcut replacement or whole-disk scanning.
