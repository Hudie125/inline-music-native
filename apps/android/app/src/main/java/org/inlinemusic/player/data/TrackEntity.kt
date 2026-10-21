package org.inlinemusic.player.data

import androidx.room.*
import kotlinx.coroutines.flow.Flow
import org.inlinemusic.player.domain.Track

@Entity(tableName = "tracks")
data class TrackEntity(
    @PrimaryKey val id: String,
    val title: String,
    val artist: String,
    val album: String,
    @ColumnInfo(name = "duration_ms") val durationMs: Long,
    @ColumnInfo(name = "track_number") val trackNumber: Int = 0,
    val year: Int = 0,
    val genre: String = "Unknown",
    @ColumnInfo(name = "file_path") val filePath: String,
    @ColumnInfo(name = "cover_uri") val coverUri: String? = null,
    @ColumnInfo(name = "is_liked") val isLiked: Boolean = false,
    @ColumnInfo(name = "is_offline") val isOffline: Boolean = true,
    @ColumnInfo(name = "created_at") val createdAt: Long = System.currentTimeMillis()
) {
    fun toDomain(): Track = Track(
        id = id,
        title = title,
        artist = artist,
        album = album,
        durationMs = durationMs,
        trackNumber = trackNumber,
        year = year,
        genre = genre,
        filePath = filePath,
        coverUri = coverUri,
        isLiked = isLiked,
        isOffline = isOffline,
        createdAt = createdAt
    )

    companion object {
        fun fromDomain(track: Track): TrackEntity = TrackEntity(
            id = track.id,
            title = track.title,
            artist = track.artist,
            album = track.album,
            durationMs = track.durationMs,
            trackNumber = track.trackNumber,
            year = track.year,
            genre = track.genre,
            filePath = track.filePath,
            coverUri = track.coverUri,
            isLiked = track.isLiked,
            isOffline = track.isOffline,
            createdAt = track.createdAt
        )
    }
}

@Entity(tableName = "playlists")
data class PlaylistEntity(
    @PrimaryKey val id: String,
    val name: String,
    val description: String? = null,
    @ColumnInfo(name = "cover_uri") val coverUri: String? = null,
    @ColumnInfo(name = "created_at") val createdAt: Long = System.currentTimeMillis()
)

@Entity(
    tableName = "playlist_tracks",
    primaryKeys = ["playlist_id", "track_id"],
    foreignKeys = [
        ForeignKey(entity = PlaylistEntity::class, parentColumns = ["id"], childColumns = ["playlist_id"], onDelete = ForeignKey.CASCADE),
        ForeignKey(entity = TrackEntity::class, parentColumns = ["id"], childColumns = ["track_id"], onDelete = ForeignKey.CASCADE)
    ],
    indices = [Index("track_id")]
)
data class PlaylistTrackCrossRef(
    @ColumnInfo(name = "playlist_id") val playlistId: String,
    @ColumnInfo(name = "track_id") val trackId: String,
    val position: Int,
    @ColumnInfo(name = "added_at") val addedAt: Long = System.currentTimeMillis()
)

@Entity(tableName = "history")
data class HistoryEntity(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    @ColumnInfo(name = "track_id") val trackId: String,
    @ColumnInfo(name = "played_at") val playedAt: Long = System.currentTimeMillis(),
    @ColumnInfo(name = "duration_listened_ms") val durationListenedMs: Long,
    @ColumnInfo(name = "completion_rate") val completionRate: Double,
    @ColumnInfo(name = "skipped_early") val skippedEarly: Boolean
)

@Entity(tableName = "lyrics_cache")
data class LyricsCacheEntity(
    @PrimaryKey val trackId: String,
    val plainLyrics: String,
    val syncedLyrics: String,
    val source: String,
    val cachedAt: Long = System.currentTimeMillis()
)
