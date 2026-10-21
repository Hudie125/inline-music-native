# inline Music

<div align="center">
  <img src="assets/icon/inline-256.png" width="128" height="128" alt="inline Music Logo" />
  <br />
  <strong>Free, open-source, native music player for Windows, Android, and Arch Linux.</strong>
  <p>Liquid Glass monochrome aesthetics × Apple Music elegance × Google Pixel UI expressiveness.</p>

  [![CI](https://github.com/Hudie125/inline-music-native/actions/workflows/ci.yml/badge.svg)](https://github.com/Hudie125/inline-music-native/actions/workflows/ci.yml)
  [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
  [![Release](https://img.shields.io/github/v/release/Hudie125/inline-music-native?include_prereleases)](https://github.com/Hudie125/inline-music-native/releases)
  [![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android%20%7C%20Arch%20Linux-lightgrey.svg)]()
</div>

---

## 🌟 Highlights

- **Three 100% Native Applications**: Zero Electron, zero WebViews, zero bloated cross-platform wrappers.
  - **Windows**: Modern C# & .NET 8 LTS with WinUI 3 Fluent Liquid Glass, Windows Media Transport Controls (SMTC), floating mini-player, and NAudio 10-band DSP engine.
  - **Android**: Modern Kotlin & Jetpack Compose (Material 3 Expressive), AndroidX Media3 / ExoPlayer foreground MediaSessionService, notification shade controls, and Room SQLite.
  - **Arch Linux**: Modern C++20 & Qt 6 (Qt Quick / QML), GStreamer pipeline with 10-band equalizer, MPRIS2 D-Bus interface, and native pacman package.
- **Privacy-First Zero-Backend Architecture**: All tracks, playlists, favorites, playback history, and settings remain 100% on your device in a local SQLite database.
- **Audiophile-Grade Audio Engine**:
  - Sample-accurate seeking and smooth state machine.
  - Real 10-Band Biquad IIR Equalizer (32 Hz to 16 kHz) with built-in and customizable presets.
  - Smooth sample-level crossfade (0–12s).
  - Sleep timer with countdown and auto-stop at track or queue end.
- **Offline & Local Library**:
  - Comprehensive tag extraction (ID3v1, ID3v2, FLAC Vorbis, MP4/AAC) and embedded artwork caching.
  - Non-repeating Fisher-Yates shuffle, repeat one/all, and drag-and-drop queue management.
- **Wave Intelligence**:
  - Smart on-device recommendation algorithm analyzing play duration, skip rates, repeat counts, and genre affinities.
- **Synchronized Lyrics & Karaoke**:
  - Real-time LRCLIB integration with millisecond timestamp synchronization, glowing active line, and offline caching.

---

## 📐 Architecture Overview

```
inline-music/
├── apps/
│   ├── windows/          # Native Windows (.NET 8 LTS, WPF-UI, NAudio DSP, SQLite)
│   ├── android/          # Native Android (Kotlin, Jetpack Compose, Media3, Room)
│   └── arch-linux/       # Native Arch Linux (C++20, Qt 6 QML, GStreamer, MPRIS)
├── assets/               # Scalable SVG, PNGs, and ICO application icons
├── docs/                 # In-depth architectural & design documentation
│   ├── architecture.md   # Unified layer structure & state machines
│   ├── design-system.md  # Liquid Glass & monochrome typography specifications
│   ├── feature-matrix.md # Cross-platform parity matrix
│   ├── providers.md      # Modular source & metadata provider interfaces
│   ├── security.md       # Zero-backend privacy & credential safety
│   ├── building.md       # Comprehensive build instructions
│   └── testing.md        # Test suite guides
└── .github/workflows/    # Automated CI/CD and release packaging pipelines
```

---

## 🚀 Quick Start & Installation

### Windows
Download the latest `InlineMusic-win-x64.zip` or standalone executable from [GitHub Releases](https://github.com/Hudie125/inline-music-native/releases), extract, and run `InlineMusic.exe`.

### Android
Download the signed APK (`inline-music-v0.1.0.apk`) from [GitHub Releases](https://github.com/Hudie125/inline-music-native/releases) and install directly on Android 8.0+ (API 26+).

### Arch Linux
Install via pacman:
```bash
sudo pacman -U inline-music-0.1.0-1-x86_64.pkg.tar.zst
```
Or build locally via `makepkg`:
```bash
cd apps/arch-linux && makepkg -sfc
```

---

## 🛠️ Building From Source

Refer to [`docs/building.md`](docs/building.md) for full system prerequisites and compilation instructions for all three platforms.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
Third-party notices are detailed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
