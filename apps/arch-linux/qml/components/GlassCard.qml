import QtQuick
import QtQuick.Controls

Rectangle {
    id: root
    radius: 16
    color: "#18181C"
    opacity: 0.85
    border.color: "#25FFFFFF"
    border.width: 1

    default property alias content: contentItem.data

    Item {
        id: contentItem
        anchors.fill: parent
        anchors.margins: 16
    }
}
