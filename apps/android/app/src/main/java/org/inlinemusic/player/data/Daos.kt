package org.inlinemusic.player.data

import androidx.room.*
import kotlinx.coroutines.flow.Flow

@Dao
interface TrackDao {
    @Query("SELECT * FROM tracks ORDER BY artist, album, track_number, title")
    fun getAllTracksFlow(): Flow<List<TrackEntity>>

    @Query("SELECT * FROM tracks ORDER BY artist, album, track_number, title")
    suspend fun getAllTracks(): List<TrackEntity>

    @Query("SELECT * FROM tracks WHERE id = :id")
    suspend fun getTrackById(id: String): TrackEntity?

    @Query("SELECT * FROM tracks WHERE is_liked = 1 ORDER BY artist, title")
    fun getLikedTracksFlow(): Flow<List<TrackEntity>>

    @Query("SELECT * FROM tracks WHERE is_liked = 1 ORDER BY artist, title")
    suspend fun getLikedTracks(): List<TrackEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertOrUpdateTrack(track: TrackEntity)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertOrUpdateTracks(tracks: List<TrackEntity>)

    @Query("UPDATE tracks SET is_liked = :isLiked WHERE id = :id")
    suspend fun updateLike(id: String, isLiked: Boolean)

    @Query("DELETE FROM tracks WHERE id = :id")
    suspend fun deleteTrack(id: String)

    @Query("SELECT * FROM tracks WHERE title LIKE '%' || :query || '%' OR artist LIKE '%' || :query || '%' OR album LIKE '%' || :query || '%'")
    suspend fun searchTracks(query: String): List<TrackEntity>
}

@Dao
interface PlaylistDao {
    @Query("SELECT * FROM playlists ORDER BY name")
    fun getPlaylistsFlow(): Flow<List<PlaylistEntity>>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertPlaylist(playlist: PlaylistEntity)

    @Query("DELETE FROM playlists WHERE id = :id")
    suspend fun deletePlaylist(id: String)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun addTrackToPlaylist(crossRef: PlaylistTrackCrossRef)

    @Query("DELETE FROM playlist_tracks WHERE playlist_id = :playlistId AND track_id = :trackId")
    suspend fun removeTrackFromPlaylist(playlistId: String, trackId: String)

    @Query("""
        SELECT t.* FROM tracks t
        INNER JOIN playlist_tracks pt ON t.id = pt.track_id
        WHERE pt.playlist_id = :playlistId
        ORDER BY pt.position
    """)
    fun getPlaylistTracksFlow(playlistId: String): Flow<List<TrackEntity>>
}

@Dao
interface HistoryDao {
    @Query("SELECT * FROM history ORDER BY played_at DESC LIMIT :limit")
    suspend fun getRecentHistory(limit: Int = 100): List<HistoryEntity>

    @Insert
    suspend fun insertHistory(history: HistoryEntity)

    @Query("DELETE FROM history")
    suspend fun clearHistory()
}

@Dao
interface LyricsDao {
    @Query("SELECT * FROM lyrics_cache WHERE trackId = :trackId")
    suspend fun getCachedLyrics(trackId: String): LyricsCacheEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertLyrics(lyrics: LyricsCacheEntity)
}
