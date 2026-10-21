# Contributing to inline Music

Thank you for your interest in contributing to **inline Music**!

## Code of Conduct
We are committed to providing a friendly, safe, and welcoming environment for all contributors, regardless of background or identity.

## Development Workflow
1. Fork the repository on GitHub.
2. Clone your fork locally:
   ```bash
   git clone https://github.com/Hudie125/inline-music-native.git
   ```
3. Create a feature branch:
   ```bash
   git checkout -b feat/your-feature-name
   ```
4. Ensure all unit and integration tests pass on your target platform.
5. Adhere to the platform-specific code style:
   - **Windows**: C# conventions, nullable reference types enabled, MVVM pattern.
   - **Android**: Kotlin idioms, Jetpack Compose best practices, unidirectional data flow (UDF).
   - **Arch Linux**: C++20 conventions, modern Qt 6 QML architecture, memory-safe RAII.
6. Commit with clear, descriptive commit messages:
   ```
   feat(audio): implement sample-accurate crossfade curve
   fix(lyrics): handle malformed LRC timestamp frames
   ```
7. Open a Pull Request against the `main` branch.
