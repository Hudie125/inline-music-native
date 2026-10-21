package org.inlinemusic.player.ui.components

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Pause
import androidx.compose.material.icons.filled.SkipNext
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import org.inlinemusic.player.domain.Track
import org.inlinemusic.player.ui.theme.BorderHighlight
import org.inlinemusic.player.ui.theme.SurfaceGlassElevated
import org.inlinemusic.player.ui.theme.TextPrimary
import org.inlinemusic.player.ui.theme.TextSecondary

@Composable
fun MiniPlayerBar(
    currentTrack: Track?,
    isPlaying: Boolean,
    onTogglePlayPause: () -> Unit,
    onNext: () -> Unit,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    if (currentTrack == null) return

    GlassCard(
        modifier = modifier
            .fillMaxWidth()
            .height(68.dp)
            .padding(horizontal = 12.dp, vertical = 4.dp)
            .clickable { onClick() },
        shape = RoundedCornerShape(18.dp),
        backgroundColor = SurfaceGlassElevated,
        borderColor = BorderHighlight
    ) {
        Row(
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = 12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Album Artwork thumbnail
            AsyncImage(
                model = currentTrack.coverUri,
                contentDescription = null,
                contentScale = ContentScale.Crop,
                modifier = Modifier
                    .size(44.dp)
                    .clip(RoundedCornerShape(10.dp))
            )

            // Title & Artist
            Column(
                modifier = Modifier
                    .weight(1f)
                    .padding(start = 12.dp, end = 8.dp)
            ) {
                Text(
                    text = currentTrack.title,
                    color = TextPrimary,
                    fontSize = 14.sp,
                    fontWeight = FontWeight.SemiBold,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
                Text(
                    text = currentTrack.artist,
                    color = TextSecondary,
                    fontSize = 12.sp,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }

            // Controls
            IconButton(onClick = onTogglePlayPause) {
                Icon(
                    imageVector = if (isPlaying) Icons.Default.Pause else Icons.Default.PlayArrow,
                    contentDescription = "Play/Pause",
                    tint = Color.White,
                    modifier = Modifier.size(28.dp)
                )
            }

            IconButton(onClick = onNext) {
                Icon(
                    imageVector = Icons.Default.SkipNext,
                    contentDescription = "Next",
                    tint = Color.White,
                    modifier = Modifier.size(24.dp)
                )
            }
        }
    }
}
