import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import "components"
import "views"

ApplicationWindow {
    id: window
    visible: true
    width: 1180
    height: 760
    minimumWidth: 960
    minimumHeight: 620
    title: "inline"
    color: "#000000"

    property string currentView: "library"

    // Root Container
    Item {
        anchors.fill: parent

        // Main Layout
        Row {
            anchors.fill: parent
            anchors.bottomMargin: 84

            // Left Sidebar
            Rectangle {
                width: 220
                height: parent.height
                color: "#0C0C0E"
                border.color: "#15FFFFFF"
                border.width: 1

                Column {
                    anchors.fill: parent
                    anchors.margins: 16
                    spacing: 24

                    // Brand Logo
                    Row {
                        spacing: 12
                        Rectangle {
                            width: 32
                            height: 32
                            radius: 8
                            color: "#1C1C22"
                            border.color: "#30FFFFFF"
                            Text {
                                anchors.centerIn: parent
                                text: "..."
                                color: "#FFFFFF"
                                font.bold: true
                                font.pixelSize: 18
                            }
                        }
                        Text {
                            anchors.verticalCenter: parent.verticalCenter
                            text: "inline"
                            color: "#FFFFFF"
                            font.bold: true
                            font.pixelSize: 18
                        }
                    }

                    // Nav Buttons
                    Column {
                        width: parent.width
                        spacing: 6

                        Button {
                            width: parent.width
                            text: "🎵  Library"
                            onClicked: currentView = "library"
                        }
                        Button {
                            width: parent.width
                            text: "🔍  Search"
                            onClicked: currentView = "search"
                        }
                        Button {
                            width: parent.width
                            text: "🌊  My Wave"
                            onClicked: currentView = "wave"
                        }
                        Button {
                            width: parent.width
                            text: "🎚️  Equalizer"
                            onClicked: currentView = "equalizer"
                        }
                        Button {
                            width: parent.width
                            text: "⚙️  Settings"
                            onClicked: currentView = "settings"
                        }
                    }
                }
            }

            // Center Dynamic View
            Item {
                width: parent.width - 220
                height: parent.height

                LibraryView {
                    anchors.fill: parent
                    visible: currentView === "library"
                }

                SearchView {
                    anchors.fill: parent
                    visible: currentView === "search"
                }

                WaveView {
                    anchors.fill: parent
                    visible: currentView === "wave"
                }

                EqualizerView {
                    anchors.fill: parent
                    visible: currentView === "equalizer"
                }

                SettingsView {
                    anchors.fill: parent
                    visible: currentView === "settings"
                }

                FullPlayerView {
                    anchors.fill: parent
                    visible: currentView === "player"
                }
            }
        }

        // Bottom Liquid Glass Player Bar
        Rectangle {
            anchors.bottom: parent.bottom
            width: parent.width
            height: 84
            color: "#141418"
            border.color: "#20FFFFFF"
            border.width: 1

            Row {
                anchors.fill: parent
                anchors.margins: 16
                spacing: 20

                // Left: Artwork & Info
                Row {
                    width: 280
                    spacing: 12
                    anchors.verticalCenter: parent.verticalCenter

                    Rectangle {
                        width: 52
                        height: 52
                        radius: 10
                        color: "#28282E"
                        Image {
                            anchors.fill: parent
                            source: audioEngine.currentTrack.coverPath || ""
                            fillMode: Image.PreserveAspectCrop
                        }
                    }

                    Column {
                        anchors.verticalCenter: parent.verticalCenter
                        width: 210
                        Text {
                            text: audioEngine.currentTrack.title || "inline Music"
                            color: "#FFFFFF"
                            font.bold: true
                            font.pixelSize: 14
                            elide: Text.ElideRight
                            width: parent.width
                        }
                        Text {
                            text: audioEngine.currentTrack.artist || "Ready"
                            color: "#8E8E93"
                            font.pixelSize: 12
                            elide: Text.ElideRight
                            width: parent.width
                        }
                    }
                }

                // Center: Playback Controls & Timeline Scrubber
                Column {
                    width: parent.width - 580
                    anchors.verticalCenter: parent.verticalCenter
                    spacing: 4

                    Row {
                        anchors.horizontalCenter: parent.horizontalCenter
                        spacing: 12

                        Button {
                            text: "⏮"
                            onClicked: audioEngine.previous()
                        }
                        Button {
                            text: audioEngine.state === 3 ? "⏸" : "▶"
                            font.bold: true
                            onClicked: audioEngine.togglePlayPause()
                        }
                        Button {
                            text: "⏭"
                            onClicked: audioEngine.next()
                        }
                    }

                    Row {
                        width: parent.width
                        spacing: 8

                        Text {
                            text: formatTime(audioEngine.positionMs)
                            color: "#8E8E93"
                            font.pixelSize: 11
                        }

                        Slider {
                            width: parent.width - 80
                            from: 0
                            to: Math.max(1, audioEngine.durationMs)
                            value: audioEngine.positionMs
                            onMoved: audioEngine.seek(value)
                        }

                        Text {
                            text: formatTime(audioEngine.durationMs)
                            color: "#8E8E93"
                            font.pixelSize: 11
                        }
                    }
                }

                // Right: Volume & Fullscreen Button
                Row {
                    width: 220
                    anchors.verticalCenter: parent.verticalCenter
                    spacing: 8
                    layoutDirection: Qt.RightToLeft

                    Button {
                        text: "⛶"
                        onClicked: currentView = (currentView === "player" ? "library" : "player")
                    }

                    Slider {
                        width: 90
                        from: 0
                        to: 1
                        value: audioEngine.volume
                        onValueChanged: audioEngine.volume = value
                    }

                    Text {
                        text: "🔊"
                        color: "#8E8E93"
                        anchors.verticalCenter: parent.verticalCenter
                    }
                }
            }
        }

        // Welcome Overlay Sequence
        Rectangle {
            id: welcomeOverlay
            anchors.fill: parent
            color: "#000000"
            visible: true
            opacity: 1

            Column {
                anchors.centerIn: parent
                spacing: 24

                Text {
                    id: welcomeText
                    text: "welcome"
                    font.pixelSize: 44
                    font.bold: true
                    color: "#FFFFFF"
                    opacity: 0

                    NumberAnimation on opacity {
                        from: 0
                        to: 1
                        duration: 800
                    }
                }

                Button {
                    text: "Enter inline"
                    onClicked: {
                        welcomeOverlay.visible = false
                    }
                }
            }
        }
    }

    function formatTime(ms) {
        var totalSec = Math.floor(ms / 1000)
        var min = Math.floor(totalSec / 60)
        var sec = totalSec % 60
        return min + ":" + (sec < 10 ? "0" : "") + sec
    }
}
