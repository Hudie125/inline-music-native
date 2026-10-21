# Testing inline Music

inline Music incorporates comprehensive unit, integration, and audio DSP tests across all three native applications.

---

## 1. Windows Application Tests

The test suite covers:
- **Audio State Machine**: Verifies transitions, seek handling, pause/resume, and auto-next logic.
- **10-Band Equalizer DSP**: Verifies biquad filter calculations, frequency gain adjustments, and sample clipping prevention.
- **Wave Recommendation Engine**: Verifies tag affinity, skip dampening, favorite weighting, and non-repeating track selection.
- **LRC Parser**: Verifies synchronized millisecond timestamp parsing, multi-timestamp line expansion, and malformed LRC resilience.
- **SQLite Database**: Verifies schema initialization, transaction safety, playlist operations, and listening history retention.

### Running Windows Tests:
```powershell
cd apps/windows
dotnet test tests/InlineMusic.Tests/InlineMusic.Tests.csproj -v normal
```

---

## 2. Android Application Tests

- **MediaSession & Service Lifecycle**: Ensures `InlineMediaService` handles audio focus loss (`AUDIOFOCUS_LOSS_TRANSIENT`) and headphone detachment (`ACTION_AUDIO_BECOMING_NOISY`).
- **Room DAOs**: Validates database queries for library filtering, likes toggling, and history logging.
- **Wave Recommendation Algorithm**: Validates Android Kotlin implementation of local recommendation heuristics.

### Running Android Tests:
```bash
cd apps/android
./gradlew testDebugUnitTest
```

---

## 3. Arch Linux Application Tests

- **GStreamer Pipeline Verification**: Validates pipeline state transitions (`READY`, `PAUSED`, `PLAYING`) and equalizer element linkage.
- **MPRIS2 D-Bus Interface**: Verifies D-Bus property getters and methods (`PlayPause`, `Next`, `Previous`, `Seek`, `Metadata`).
- **LRC Parser & Wave Algorithm**: C++20 unit tests for recommendation heuristics and lyrics synchronization.

### Running Arch Linux Tests:
```bash
cd apps/arch-linux/build
ctest --output-on-failure
```
