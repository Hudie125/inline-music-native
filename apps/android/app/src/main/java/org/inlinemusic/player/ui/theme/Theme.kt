package org.inlinemusic.player.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable

private val DarkColorScheme = darkColorScheme(
    primary = PureWhite,
    onPrimary = PureBlack,
    secondary = TextSecondary,
    onSecondary = PureWhite,
    background = PureBlack,
    onBackground = PureWhite,
    surface = SurfaceGlass,
    onSurface = PureWhite,
    surfaceVariant = SurfaceGlassElevated,
    outline = BorderSubtle
)

@Composable
fun InlineMusicTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = DarkColorScheme,
        typography = Typography,
        content = content
    )
}
