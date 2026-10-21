cmake_minimum_required(VERSION 3.25)
project(inline-music VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)

# Find Qt 6 Modules
find_package(Qt6 REQUIRED COMPONENTS Core Gui Qml Quick QuickControls2 Sql DBus Network)

# Find GStreamer 1.0
find_package(PkgConfig REQUIRED)
pkg_check_modules(GST REQUIRED gstreamer-1.0 gstreamer-audio-1.0 gstreamer-pbutils-1.0)

# Application Sources
set(SOURCES
    src/main.cpp
    src/audio/GStreamerAudioEngine.hpp
    src/audio/GStreamerAudioEngine.cpp
    src/audio/EqualizerDsp.hpp
    src/audio/EqualizerDsp.cpp
    src/mpris/MprisPlayer.hpp
    src/mpris/MprisPlayer.cpp
    src/db/DatabaseManager.hpp
    src/db/DatabaseManager.cpp
    src/db/Models.hpp
    src/wave/WaveEngine.hpp
    src/wave/WaveEngine.cpp
    src/lyrics/LyricsService.hpp
    src/lyrics/LyricsService.cpp
)

set(QML_RESOURCES
    qml/qml.qrc
)

add_executable(inline-music ${SOURCES} ${QML_RESOURCES})

target_include_directories(inline-music PRIVATE
    src
    ${GST_INCLUDE_DIRS}
)

target_link_libraries(inline-music PRIVATE
    Qt6::Core
    Qt6::Gui
    Qt6::Qml
    Qt6::Quick
    Qt6::QuickControls2
    Qt6::Sql
    Qt6::DBus
    Qt6::Network
    ${GST_LIBRARIES}
)

target_compile_definitions(inline-music PRIVATE
    APPLICATION_VERSION="${PROJECT_VERSION}"
)

# Installation targets for Arch Linux package
include(GNUInstallDirs)
install(TARGETS inline-music DESTINATION ${CMAKE_INSTALL_BINDIR})
install(FILES inline.desktop DESTINATION ${CMAKE_INSTALL_DATADIR}/applications)
install(FILES ../../assets/icon/inline-256.png DESTINATION ${CMAKE_INSTALL_DATADIR}/icons/hicolor/256x256/apps RENAME inline.png)
install(FILES ../../assets/icon/inline.svg DESTINATION ${CMAKE_INSTALL_DATADIR}/icons/hicolor/scalable/apps RENAME inline.svg)

# Unit Testing
enable_testing()
add_subdirectory(tests)
