package org.inlinemusic.player.ui.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import org.inlinemusic.player.ui.theme.BorderSubtle
import org.inlinemusic.player.ui.theme.SurfaceGlass

@Composable
fun GlassCard(
    modifier: Modifier = Modifier,
    shape: Shape = RoundedCornerShape(16.dp),
    backgroundColor: Color = SurfaceGlass,
    borderColor: Color = BorderSubtle,
    borderWidth: Dp = 1.dp,
    content: @Composable BoxScope.() -> Unit
) {
    Surface(
        modifier = modifier,
        shape = shape,
        color = backgroundColor,
        border = BorderStroke(borderWidth, borderColor)
    ) {
        Box(content = content)
    }
}
