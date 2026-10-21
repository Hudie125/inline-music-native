package org.inlinemusic.player.domain

enum class PlaybackState {
    Idle,
    Loading,
    Ready,
    Playing,
    Paused,
    Buffering,
    Seeking,
    Error
}

data class Track(
    val id: String,
    val title: String,
    val artist: String,
    val album: String,
    val durationMs: Long,
    val trackNumber: Int = 0,
    val year: Int = 0,
    val genre: String = "Unknown",
    val filePath: String,
    val coverUri: String? = null,
    val isLiked: Boolean = false,
    val isOffline: Boolean = true,
    val createdAt: Long = System.currentTimeMillis()
) {
    val formattedDuration: String
        get() {
            val totalSeconds = durationMs / 1000
            val minutes = totalSeconds / 60
            val seconds = totalSeconds % 60
            return String.format("%d:%02d", minutes, seconds)
        }
}

data class Playlist(
    val id: String,
    val name: String,
    val description: String? = null,
    val coverUri: String? = null,
    val trackCount: Int = 0,
    val totalDurationMs: Long = 0L,
    val createdAt: Long = System.currentTimeMillis()
)

data class LyricsLine(
    val timestampMs: Long,
    val durationMs: Long,
    val text: String
)

data class EqualizerBand(
    val index: Int,
    val centerFreqHz: Int,
    var gainMilliDb: Int
)

data class EqualizerPreset(
    val name: String,
    val bandGainsMilliDb: List<Int>
)
