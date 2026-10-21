# Building inline Music

This guide covers building the three native applications of **inline Music** from source.

---

## 1. Prerequisites

### Windows Native App
- **OS**: Windows 10 (version 19041+) or Windows 11
- **SDK**: .NET 8.0 SDK (LTS) or higher
- **Build Tools**: Visual Studio 2022 / Build Tools with Desktop development with C#
- **Dependencies**: Restored automatically via NuGet

### Android Native App
- **OS**: Linux, macOS, or Windows
- **JDK**: OpenJDK 17 or higher
- **Android SDK**: Android API Level 34 / 35 (Android 14 / 15)
- **Build Tool**: Gradle (wrapper included)

### Arch Linux Native App
- **OS**: Arch Linux (or compatible distribution with Qt 6)
- **Compiler**: GCC 13+ or Clang 16+ supporting C++20
- **Build System**: CMake 3.25+, Ninja or GNU Make
- **Libraries**:
  - `qt6-base`, `qt6-declarative`, `qt6-shadertools`, `qt6-multimedia`
  - `gstreamer`, `gst-plugins-base`, `gst-plugins-good`, `gst-plugins-bad`
  - `sqlite3`, `pkgconf`

---

## 2. Windows Build Instructions

```powershell
# Navigate to Windows application directory
cd apps/windows

# Restore dependencies
dotnet restore

# Build release binary (standalone, framework-dependent)
dotnet build -c Release

# Publish self-contained executable for x64
dotnet publish src/InlineMusic/InlineMusic.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../../build_artifacts/windows
```

The resulting executable will be available in `build_artifacts/windows/InlineMusic.exe`.

---

## 3. Android Build Instructions

```bash
# Navigate to Android application directory
cd apps/android

# Make gradlew executable (on Linux / macOS)
chmod +x gradlew

# Build Debug APK
./gradlew assembleDebug

# Build Release APK
./gradlew assembleRelease

# Build Android App Bundle (AAB)
./gradlew bundleRelease
```

Output APK will be generated at `apps/android/app/build/outputs/apk/release/app-release-unsigned.apk`.

---

## 4. Arch Linux Build Instructions

### Method A: makepkg (Pacman Package)
```bash
# Navigate to Arch Linux packaging directory
cd apps/arch-linux

# Build pacman package using Arch build system
makepkg -sfc

# Install generated package
sudo pacman -U inline-music-*.pkg.tar.zst
```

### Method B: CMake Direct Build
```bash
cd apps/arch-linux
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . -j$(nproc)
```
