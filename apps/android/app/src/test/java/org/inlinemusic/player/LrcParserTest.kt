package org.inlinemusic.player

import org.inlinemusic.player.domain.LyricsClient
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class LrcParserTest {

    private val client = LyricsClient()

    @Test
    fun parseLrc_standardTwoDigitCentiseconds_parsesCorrectly() {
        val lrc = """
            [00:10.50]First line of lyrics
            [00:15.00]Second line of lyrics
        """.trimIndent()

        val lines = client.parseLrc(lrc)
        assertEquals(2, lines.size)
        assertEquals(10500L, lines[0].timestampMs)
        assertEquals("First line of lyrics", lines[0].text)
        assertEquals(15000L, lines[1].timestampMs)
        assertEquals("Second line of lyrics", lines[1].text)
    }

    @Test
    fun parseLrc_multiTimestamps_expandsToMultipleLines() {
        val lrc = "[00:10.00][00:25.00]Chorus line"
        val lines = client.parseLrc(lrc)

        assertEquals(2, lines.size)
        assertEquals(10000L, lines[0].timestampMs)
        assertEquals(25000L, lines[1].timestampMs)
        assertEquals("Chorus line", lines[0].text)
        assertEquals("Chorus line", lines[1].text)
    }

    @Test
    fun parseLrc_emptyOrMalformed_returnsEmpty() {
        assertTrue(client.parseLrc("").isEmpty())
        assertTrue(client.parseLrc("Just normal words without timestamp").isEmpty())
    }
}
