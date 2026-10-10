# Upstream attribution

This is an independent community launcher, not an OpenAI or DeepSeek product.

## Dragon Codex Boot

- Source: https://github.com/yushua0808-cpu/dragon-codex-boot
- Inspected commit: `d3b7bb84b75134f8250491c86d72996a70d58165`
- License: MIT, preserved in `licenses/dragon-codex-boot.txt`.
- Window discovery, process-path filtering and native DWM interop are adapted from its C# implementation.
- Its dragon video, frame-specific screen coordinates, shortcut replacement and disk scanning are not included.

## dsh-boot-animation

- Source: https://github.com/lxj5820/dsh-boot-animation
- Inspected commit: `b8bfa8d9f879f14917033a7135e3964776865cad`
- License: MIT, preserved in `licenses/dsh-boot-animation.txt`.
- The upstream license explicitly distributes `assets/videos/1.mp4`, `2.mp4`, and `3.mp4` under the same MIT terms. They are copied unchanged to `media/`.
- Visual colors, the feathered presentation, recent-clip avoidance and media-time tail dissolve inform the native implementation; no DSH plugin runtime is installed.
- SHA-256 hashes and container metadata are recorded in `media/manifest.json`.

The video art remains the upstream author's work. Product names and depicted marks remain attributable to their owners; the launcher does not claim endorsement.
