# inline Music Feature Parity Matrix

This matrix establishes feature parity across all three native implementations (**Windows**, **Android**, and **Arch Linux**).

| Feature Domain | Feature Item | Windows (.NET/WPF) | Android (Compose/Media3) | Arch Linux (Qt 6/C++) | Notes & Technical Mechanism |
| :--- | :--- | :---: | :---: | :---: | :--- |
| **Core Playback** | Play / Pause / Stop | Yes | Yes | Yes | Native thread-safe audio engine |
| | Seek with real timecode | Yes | Yes | Yes | Sample-accurate position tracking |
| | Previous / Next / Auto-advance | Yes | Yes | Yes | Continuous queue progression |
| | Playback Queue Reordering | Yes | Yes | Yes | Drag-and-drop reordering |
| | Shuffle (non-repeating) | Yes | Yes | Yes | Fisher-Yates non-repeating cycle |
| | Repeat One / Repeat All | Yes | Yes | Yes | Granular loop controls |
| | State Machine Persistence | Yes | Yes | Yes | Restores active queue on reopen |
| **Audio DSP** | 10-Band Equalizer | Yes | Yes | Yes | Real biquad IIR filters (32Hz–16kHz) |
| | EQ Presets (Rock, Bass, etc.) | Yes | Yes | Yes | Built-in + custom user presets |
| | Real Audio Crossfade | Yes | Yes | Yes | 0–12s DSP volume curve crossfade |
| | Sleep Timer | Yes | Yes | Yes | Minutes, track end, or queue end |
| **Local Library** | Folder Scanning & Watcher | Yes | Yes | Yes | Background recursive tag scanner |
| | Embedded Cover Art Extraction | Yes | Yes | Yes | TagLib / MediaMetadataRetriever |
| | Format Support (MP3/FLAC/WAV/AAC/OGG) | Yes | Yes | Yes | Native codecs & decoders |
| | Search (tracks, artists, albums) | Yes | Yes | Yes | Instant indexed SQLite search |
| | Playlists Management | Yes | Yes | Yes | Create, rename, delete, reorder |
| | Favorites / Likes | Yes | Yes | Yes | Instant toggle & offline storage |
| | Playback History & Analytics | Yes | Yes | Yes | Tracked plays, skips & duration |
| **Music Intelligence**| Wave Personal Radio | Yes | Yes | Yes | Local smart recommendation engine |
| | LRCLIB Synced Lyrics | Yes | Yes | Yes | Millisecond timestamp parsing |
| | Karaoke Mode | Yes | Yes | Yes | Active line illumination & auto-scroll |
| | Metadata Provider Integration | Yes | Yes | Yes | MusicBrainz / Cover Art Archive |
| **Offline & Downloads**| Download Manager | Yes | Yes | Yes | Multi-threaded queued downloads |
| | Offline Only Mode | Yes | Yes | Yes | Complete local playback isolation |
| | Cache Limit Management | Yes | Yes | Yes | Disk usage quota & cleanup |
| **Import / Export** | M3U / M3U8 Import & Export | Yes | Yes | Yes | Standard playlist interchange |
| | CSV Library Export | Yes | Yes | Yes | Portable metadata backup |
| | Full JSON Backup & Restore | Yes | Yes | Yes | Local library state archive |
| **Platform Integration**| System Media Controls | SMTC | MediaSession | MPRIS2 | Title, artist, art, controls |
| | Multimedia Keys | Yes | Yes | Yes | Global hardware key listeners |
| | Background Playback | Yes | Foreground Service | Yes | Never interrupted on app minimize |
| | Audio Focus & Interruptions | Audio Endpoint | Phone/Headset | PipeWire/Pulse | Graceful pause/resume on call/unplug |
| | Mini-Player | Floating Window | Notification Shade | Compact Window | Platform-appropriate mini-player |
| | Window Backdrops & Glass | Mica / Acrylic | RenderEffect Blur | MultiEffect Blur | Liquid Glass shader fallbacks |
| | Native Packaging | Self-contained / MSIX | APK & AAB | PKGBUILD / Pacman | Official distribution targets |
