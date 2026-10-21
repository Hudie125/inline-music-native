package org.inlinemusic.player.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import org.inlinemusic.player.domain.LyricsLine
import org.inlinemusic.player.domain.Track
import org.inlinemusic.player.ui.components.GlassCard
import org.inlinemusic.player.ui.theme.*

@Composable
fun LibraryScreen(
    tracks: List<Track>,
    onTrackClick: (Track) -> Unit,
    onScanClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxSize()
            .background(PureBlack)
            .padding(16.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 12.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column {
                Text("Library", style = MaterialTheme.typography.headlineLarge)
                Text("${tracks.size} local tracks", style = MaterialTheme.typography.bodyMedium)
            }

            Button(
                onClick = onScanClick,
                colors = ButtonDefaults.buttonColors(containerColor = SurfaceGlassElevated)
            ) {
                Icon(Icons.Default.Refresh, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(6.dp))
                Text("Scan")
            }
        }

        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            items(tracks) { track ->
                TrackListItem(track = track, onClick = { onTrackClick(track) })
            }
        }
    }
}

@Composable
fun TrackListItem(
    track: Track,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    GlassCard(
        modifier = modifier
            .fillMaxWidth()
            .clickable { onClick() },
        shape = RoundedCornerShape(12.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(10.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            AsyncImage(
                model = track.coverUri,
                contentDescription = null,
                contentScale = ContentScale.Crop,
                modifier = Modifier
                    .size(44.dp)
                    .clip(RoundedCornerShape(8.dp))
                    .background(Color(0xFF24242A))
            )

            Column(
                modifier = Modifier
                    .weight(1f)
                    .padding(horizontal = 12.dp)
            ) {
                Text(
                    text = track.title,
                    color = TextPrimary,
                    fontSize = 14.sp,
                    fontWeight = FontWeight.SemiBold,
                    maxLines = 1
                )
                Text(
                    text = "${track.artist} • ${track.album}",
                    color = TextSecondary,
                    fontSize = 12.sp,
                    maxLines = 1
                )
            }

            Text(
                text = track.formattedDuration,
                color = TextSecondary,
                fontSize = 12.sp
            )
        }
    }
}

@Composable
fun SearchScreen(
    query: String,
    onQueryChange: (String) -> Unit,
    results: List<Track>,
    onTrackClick: (Track) -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxSize()
            .background(PureBlack)
            .padding(16.dp)
    ) {
        Text("Search", style = MaterialTheme.typography.headlineLarge, modifier = Modifier.padding(bottom = 16.dp))

        OutlinedTextField(
            value = query,
            onValueChange = onQueryChange,
            placeholder = { Text("Tracks, artists, albums...", color = TextSecondary) },
            singleLine = true,
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(16.dp),
            colors = OutlinedTextFieldDefaults.colors(
                focusedContainerColor = SurfaceGlass,
                unfocusedContainerColor = SurfaceGlass,
                focusedTextColor = TextPrimary,
                unfocusedTextColor = TextPrimary,
                focusedBorderColor = BorderHighlight,
                unfocusedBorderColor = BorderSubtle
            )
        )

        Spacer(Modifier.height(16.dp))

        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            items(results) { track ->
                TrackListItem(track = track, onClick = { onTrackClick(track) })
            }
        }
    }
}

@Composable
fun WaveScreen(
    waveTracks: List<Track>,
    onLaunchWave: () -> Unit,
    onTrackClick: (Track) -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxSize()
            .background(PureBlack)
            .padding(16.dp)
    ) {
        Text("My Wave", style = MaterialTheme.typography.headlineLarge, modifier = Modifier.padding(bottom = 16.dp))

        GlassCard(
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(20.dp),
            backgroundColor = SurfaceGlassElevated
        ) {
            Column(modifier = Modifier.padding(20.dp)) {
                Text("Endless Stream", fontSize = 20.sp, fontWeight = FontWeight.Bold, color = TextPrimary)
                Text(
                    "Smart on-device recommendations computed from your listening history and likes.",
                    fontSize = 13.sp,
                    color = TextSecondary,
                    modifier = Modifier.padding(vertical = 8.dp)
                )

                Button(
                    onClick = onLaunchWave,
                    colors = ButtonDefaults.buttonColors(containerColor = PureWhite, contentColor = PureBlack),
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier.align(Alignment.End)
                ) {
                    Text("Launch Wave", fontWeight = FontWeight.Bold)
                }
            }
        }

        Spacer(Modifier.height(20.dp))

        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            items(waveTracks) { track ->
                TrackListItem(track = track, onClick = { onTrackClick(track) })
            }
        }
    }
}

@Composable
fun FullscreenPlayerScreen(
    currentTrack: Track?,
    isPlaying: Boolean,
    onTogglePlayPause: () -> Unit,
    onNext: () -> Unit,
    onPrevious: () -> Unit,
    onClose: () -> Unit,
    lyrics: List<LyricsLine>,
    currentPosMs: Long,
    modifier: Modifier = Modifier
) {
    if (currentTrack == null) return

    Column(
        modifier = modifier
            .fillMaxSize()
            .background(PureBlack)
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        // Top Header
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onClose) {
                Icon(Icons.Default.KeyboardArrowDown, contentDescription = "Close", tint = PureWhite)
            }
            Text("Now Playing", color = TextSecondary, fontSize = 14.sp, fontWeight = FontWeight.Medium)
            Spacer(Modifier.size(48.dp))
        }

        Spacer(Modifier.height(16.dp))

        // Large Album Art
        AsyncImage(
            model = currentTrack.coverUri,
            contentDescription = null,
            contentScale = ContentScale.Crop,
            modifier = Modifier
                .size(280.dp)
                .clip(RoundedCornerShape(24.dp))
                .background(Color(0xFF1E1E24))
        )

        Spacer(Modifier.height(24.dp))

        // Title & Artist
        Text(
            text = currentTrack.title,
            color = TextPrimary,
            fontSize = 22.sp,
            fontWeight = FontWeight.Bold,
            textAlign = TextAlign.Center
        )
        Text(
            text = currentTrack.artist,
            color = TextSecondary,
            fontSize = 16.sp,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(top = 4.dp)
        )

        Spacer(Modifier.height(24.dp))

        // Lyrics snippet / Karaoke
        if (lyrics.isNotEmpty()) {
            val activeIndex = lyrics.indexOfLast { it.timestampMs <= currentPosMs }.coerceAtLeast(0)
            GlassCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(80.dp),
                shape = RoundedCornerShape(16.dp)
            ) {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text(
                        text = lyrics.getOrNull(activeIndex)?.text ?: "",
                        color = PureWhite,
                        fontSize = 17.sp,
                        fontWeight = FontWeight.SemiBold,
                        textAlign = TextAlign.Center,
                        modifier = Modifier.padding(horizontal = 16.dp)
                    )
                }
            }
        }

        Spacer(Modifier.weight(1f))

        // Controls
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.Center,
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onPrevious, modifier = Modifier.size(54.dp)) {
                Icon(Icons.Default.SkipPrevious, contentDescription = "Prev", tint = PureWhite, modifier = Modifier.size(32.dp))
            }
            Spacer(Modifier.width(20.dp))
            IconButton(
                onClick = onTogglePlayPause,
                modifier = Modifier
                    .size(68.dp)
                    .clip(RoundedCornerShape(34.dp))
                    .background(PureWhite)
            ) {
                Icon(
                    imageVector = if (isPlaying) Icons.Default.Pause else Icons.Default.PlayArrow,
                    contentDescription = "Play/Pause",
                    tint = PureBlack,
                    modifier = Modifier.size(36.dp)
                )
            }
            Spacer(Modifier.width(20.dp))
            IconButton(onClick = onNext, modifier = Modifier.size(54.dp)) {
                Icon(Icons.Default.SkipNext, contentDescription = "Next", tint = PureWhite, modifier = Modifier.size(32.dp))
            }
        }
    }
}
