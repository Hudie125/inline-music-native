# Changelog

All notable changes to **inline Music** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-10-21

### Added
- **Core Architecture**:
  - Three distinct native applications for Windows, Android, and Arch Linux.
  - Strict Finite State Machine for audio playback (Idle, Loading, Ready, Playing, Paused, Buffering, Seeking, Error).
  - Local relational SQLite schema for tracks, playlists, favorites, playback history, and lyrics caching.
- **Windows Native App (.NET 8 LTS)**:
  - Modern Fluent UI with Liquid Glass acrylic/mica backdrops and custom caption titlebar.
  - Native NAudio DSP audio engine with real 10-band equalizer (32Hz–16kHz) and real sample-level crossfade.
  - System Media Transport Controls (SMTC) integration for lockscreen, taskbar, and hardware media keys.
  - Floating mini-player window with hover expansion and global hotkey toggle.
  - TagLibSharp metadata extractor and recursive directory watcher.
  - LRCLIB synchronized lyrics client with real-time scrolling and active line glow.
- **Android Native App (Kotlin / Jetpack Compose)**:
  - Material 3 Expressive + Liquid Glass UI surfaces with spring animations.
  - AndroidX Media3 / ExoPlayer foreground MediaSessionService.
  - System notification shade playback controls and lockscreen artwork display.
  - Audio focus management for incoming phone calls and headset disconnection.
  - Room SQLite database and DataStore preferences.
  - 10-band equalizer and sleep timer.
- **Arch Linux Native App (C++20 / Qt 6 / QML)**:
  - Qt Quick QML Liquid Glass interface with hardware-accelerated shaders.
  - GStreamer audio pipeline with `playbin3` and `equalizer-10bands`.
  - MPRIS2 D-Bus interface (`org.mpris.MediaPlayer2.inline`).
  - Native desktop notifications and media key integration.
  - Arch Linux `PKGBUILD` for pacman package distribution.
- **CI/CD & DevOps**:
  - Multi-platform GitHub Actions workflows (`ci.yml` and `release.yml`).
  - Automated package building for Windows (EXE/MSIX), Android (APK/AAB), and Arch Linux (`.pkg.tar.zst`).
  - Automated SHA-256 checksum generation for official releases.
