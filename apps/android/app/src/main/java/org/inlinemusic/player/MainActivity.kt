package org.inlinemusic.player

import android.Manifest
import android.content.ComponentName
import android.content.ContentUris
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.provider.MediaStore
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import androidx.media3.common.MediaItem
import androidx.media3.common.Player
import androidx.media3.session.MediaController
import androidx.media3.session.SessionToken
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.inlinemusic.player.audio.InlineMediaService
import org.inlinemusic.player.data.TrackEntity
import org.inlinemusic.player.domain.LyricsClient
import org.inlinemusic.player.domain.LyricsLine
import org.inlinemusic.player.domain.Track
import org.inlinemusic.player.domain.WaveEngine
import org.inlinemusic.player.ui.components.MiniPlayerBar
import org.inlinemusic.player.ui.screens.*
import org.inlinemusic.player.ui.theme.InlineMusicTheme

class MainActivity : ComponentActivity() {

    private var mediaController: MediaController? = null
    private val lyricsClient = LyricsClient()

    private val permissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { permissions ->
        val granted = permissions.values.any { it }
        if (granted) {
            scanMediaStore()
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        initMediaController()
        checkAndRequestPermissions()

        val db = (application as InlineApp).database
        val waveEngine = WaveEngine(db)

        setContent {
            InlineMusicTheme {
                var selectedTab by remember { mutableIntStateOf(0) }
                var currentTrack by remember { mutableStateOf<Track?>(null) }
                var isPlaying by remember { mutableStateOf(false) }
                var currentPositionMs by remember { mutableLongStateOf(0L) }
                var isFullscreenPlayerOpen by remember { mutableStateOf(false) }

                val tracks by db.trackDao().getAllTracksFlow().collectAsState(initial = emptyList())
                val domainTracks = remember(tracks) { tracks.map { it.toDomain() } }

                var searchQuery by remember { mutableStateOf("") }
                var searchResults by remember { mutableStateOf(emptyList<Track>()) }
                var waveTracks by remember { mutableStateOf(emptyList<Track>()) }
                var lyrics by remember { mutableStateOf(emptyList<LyricsLine>()) }

                // Track player progress
                LaunchedEffect(isPlaying) {
                    while (isPlaying) {
                        mediaController?.let {
                            currentPositionMs = it.currentPosition
                        }
                        kotlinx.coroutines.delay(250)
                    }
                }

                fun playTrack(track: Track) {
                    currentTrack = track
                    mediaController?.let { controller ->
                        val mediaItem = MediaItem.Builder()
                            .setMediaId(track.id)
                            .setUri(Uri.parse(track.filePath))
                            .build()
                        controller.setMediaItem(mediaItem)
                        controller.prepare()
                        controller.play()
                        isPlaying = true
                    }

                    lifecycleScope.launch {
                        lyrics = lyricsClient.fetchLyrics(track.title, track.artist, track.durationMs / 1000)
                    }
                }

                if (isFullscreenPlayerOpen && currentTrack != null) {
                    FullscreenPlayerScreen(
                        currentTrack = currentTrack,
                        isPlaying = isPlaying,
                        onTogglePlayPause = {
                            mediaController?.let {
                                if (it.isPlaying) it.pause() else it.play()
                                isPlaying = it.isPlaying
                            }
                        },
                        onNext = { mediaController?.seekToNextMediaItem() },
                        onPrevious = { mediaController?.seekToPreviousMediaItem() },
                        onClose = { isFullscreenPlayerOpen = false },
                        lyrics = lyrics,
                        currentPosMs = currentPositionMs
                    )
                } else {
                    Scaffold(
                        bottomBar = {
                            Column {
                                MiniPlayerBar(
                                    currentTrack = currentTrack,
                                    isPlaying = isPlaying,
                                    onTogglePlayPause = {
                                        mediaController?.let {
                                            if (it.isPlaying) it.pause() else it.play()
                                            isPlaying = it.isPlaying
                                        }
                                    },
                                    onNext = { mediaController?.seekToNextMediaItem() },
                                    onClick = { isFullscreenPlayerOpen = true }
                                )

                                NavigationBar(containerColor = MaterialTheme.colorScheme.background) {
                                    NavigationBarItem(
                                        selected = selectedTab == 0,
                                        onClick = { selectedTab = 0 },
                                        icon = { Icon(Icons.Default.LibraryMusic, contentDescription = null) },
                                        label = { Text("Library") }
                                    )
                                    NavigationBarItem(
                                        selected = selectedTab == 1,
                                        onClick = { selectedTab = 1 },
                                        icon = { Icon(Icons.Default.Search, contentDescription = null) },
                                        label = { Text("Search") }
                                    )
                                    NavigationBarItem(
                                        selected = selectedTab == 2,
                                        onClick = {
                                            selectedTab = 2
                                            lifecycleScope.launch {
                                                waveTracks = waveEngine.generateWaveQueue(25)
                                            }
                                        },
                                        icon = { Icon(Icons.Default.GraphicEq, contentDescription = null) },
                                        label = { Text("Wave") }
                                    )
                                }
                            }
                        }
                    ) { innerPadding ->
                        Box(modifier = Modifier.padding(innerPadding)) {
                            when (selectedTab) {
                                0 -> LibraryScreen(
                                    tracks = domainTracks,
                                    onTrackClick = { playTrack(it) },
                                    onScanClick = { scanMediaStore() }
                                )
                                1 -> SearchScreen(
                                    query = searchQuery,
                                    onQueryChange = { q ->
                                        searchQuery = q
                                        lifecycleScope.launch {
                                            val found = db.trackDao().searchTracks(q)
                                            searchResults = found.map { it.toDomain() }
                                        }
                                    },
                                    results = searchResults,
                                    onTrackClick = { playTrack(it) }
                                )
                                2 -> WaveScreen(
                                    waveTracks = waveTracks,
                                    onLaunchWave = {
                                        lifecycleScope.launch {
                                            waveTracks = waveEngine.generateWaveQueue(25)
                                            waveTracks.firstOrNull()?.let { playTrack(it) }
                                        }
                                    },
                                    onTrackClick = { playTrack(it) }
                                )
                            }
                        }
                    }
                }
            }
        }
    }

    private fun initMediaController() {
        val sessionToken = SessionToken(this, ComponentName(this, InlineMediaService::class.java))
        val controllerFuture = MediaController.Builder(this, sessionToken).buildAsync()
        controllerFuture.addListener({
            mediaController = controllerFuture.get()
        }, ContextCompat.getMainExecutor(this))
    }

    private fun checkAndRequestPermissions() {
        val permissions = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            arrayOf(Manifest.permission.READ_MEDIA_AUDIO, Manifest.permission.POST_NOTIFICATIONS)
        } else {
            arrayOf(Manifest.permission.READ_EXTERNAL_STORAGE)
        }

        val needed = permissions.filter {
            ContextCompat.checkSelfPermission(this, it) != PackageManager.PERMISSION_GRANTED
        }

        if (needed.isNotEmpty()) {
            permissionLauncher.launch(needed.toTypedArray())
        } else {
            scanMediaStore()
        }
    }

    private fun scanMediaStore() {
        lifecycleScope.launch(Dispatchers.IO) {
            val db = (application as InlineApp).database
            val audioList = mutableListOf<TrackEntity>()

            val uri = MediaStore.Audio.Media.EXTERNAL_CONTENT_URI
            val projection = arrayOf(
                MediaStore.Audio.Media._ID,
                MediaStore.Audio.Media.TITLE,
                MediaStore.Audio.Media.ARTIST,
                MediaStore.Audio.Media.ALBUM,
                MediaStore.Audio.Media.DURATION,
                MediaStore.Audio.Media.DATA,
                MediaStore.Audio.Media.ALBUM_ID
            )
            val selection = "${MediaStore.Audio.Media.IS_MUSIC} != 0"

            contentResolver.query(uri, projection, selection, null, null)?.use { cursor ->
                val idCol = cursor.getColumnIndexOrThrow(MediaStore.Audio.Media._ID)
                val titleCol = cursor.getColumnIndexOrThrow(MediaStore.Audio.Media.TITLE)
                val artistCol = cursor.getColumnIndexOrThrow(MediaStore.Audio.Media.ARTIST)
                val albumCol = cursor.getColumnIndexOrThrow(MediaStore.Audio.Media.ALBUM)
                val durCol = cursor.getColumnIndexOrThrow(MediaStore.Audio.Media.DURATION)
                val dataCol = cursor.getColumnIndexOrThrow(MediaStore.Audio.Media.DATA)
                val albumIdCol = cursor.getColumnIndexOrThrow(MediaStore.Audio.Media.ALBUM_ID)

                while (cursor.moveToNext()) {
                    val mediaId = cursor.getLong(idCol)
                    val title = cursor.getString(titleCol) ?: "Unknown"
                    val artist = cursor.getString(artistCol) ?: "Unknown Artist"
                    val album = cursor.getString(albumCol) ?: "Unknown Album"
                    val duration = cursor.getLong(durCol)
                    val data = cursor.getString(dataCol) ?: ""
                    val albumId = cursor.getLong(albumIdCol)

                    val artworkUri = ContentUris.withAppendedId(
                        Uri.parse("content://media/external/audio/albumart"),
                        albumId
                    ).toString()

                    audioList.add(
                        TrackEntity(
                            id = mediaId.toString(),
                            title = title,
                            artist = artist,
                            album = album,
                            durationMs = duration,
                            filePath = data,
                            coverUri = artworkUri,
                            isLiked = false,
                            isOffline = true
                        )
                    )
                }
            }

            if (audioList.isNotEmpty()) {
                db.trackDao().insertOrUpdateTracks(audioList)
            }
        }
    }

    override fun onDestroy() {
        mediaController?.release()
        super.onDestroy()
    }
}
