# Security, Privacy & Local Storage

## 1. Zero-Backend Architecture

inline Music operates under a strict **Zero-Backend Architecture**:
- **No Central Server**: There is no central tracking server, analytics collection, or remote user database.
- **Local Data Sovereign**: All user preferences, listening history, favorites, equalizer presets, and cached lyrics reside entirely on the local device.
- **No Telemetry**: No tracking pings, crash reporting telemetry, or fingerprinting scripts are embedded.

---

## 2. Secure Local Storage

- **Database**: SQLite with parameterized queries prevents SQL injection risks across all platforms.
- **File System Sandboxing**:
  - Android adheres to Scoped Storage and MediaStore APIs.
  - Windows and Linux restrict file access exclusively to user-selected folders.
- **Path Traversal Protection**: M3U playlist imports and archive unpackers validate all relative paths, disallowing `../` directory traversal vulnerabilities.

---

## 3. Secret Management & Credentials

- No API secret keys or user passwords are hardcoded in the codebase.
- User authentication tokens (for optional lyrics or metadata services) are stored via OS-native credential vaults (Windows Credential Manager, Android KeyStore, Linux Secret Service).
