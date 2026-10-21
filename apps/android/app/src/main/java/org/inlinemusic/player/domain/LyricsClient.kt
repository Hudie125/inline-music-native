package org.inlinemusic.player.domain

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import org.json.JSONObject
import java.net.URLEncoder
import java.util.concurrent.TimeUnit
import java.util.regex.Pattern

class LyricsClient {

    private val httpClient = OkHttpClient.Builder()
        .connectTimeout(8, TimeUnit.SECONDS)
        .readTimeout(8, TimeUnit.SECONDS)
        .build()

    private val timestampPattern = Pattern.compile("\\[(\\d{1,2}):(\\d{2})(?:\\.(\\d{2,3}))?\\]")

    suspend fun fetchLyrics(title: String, artist: String, durationSec: Long = 0): List<LyricsLine> = withContext(Dispatchers.IO) {
        try {
            val encodedTitle = URLEncoder.encode(title, "UTF-8")
            val encodedArtist = URLEncoder.encode(artist, "UTF-8")
            var url = "https://lrclib.net/api/get?track_name=$encodedTitle&artist_name=$encodedArtist"
            if (durationSec > 0) {
                url += "&duration=$durationSec"
            }

            val request = Request.Builder()
                .url(url)
                .header("User-Agent", "inline-Music-Android/0.1.0")
                .build()

            val response = httpClient.newCall(request).execute()
            if (response.isSuccessful) {
                val jsonStr = response.body?.string() ?: return@withContext emptyList()
                val json = JSONObject(jsonStr)
                if (json.has("syncedLyrics") && !json.isNull("syncedLyrics")) {
                    val synced = json.getString("syncedLyrics")
                    return@withContext parseLrc(synced)
                } else if (json.has("plainLyrics") && !json.isNull("plainLyrics")) {
                    val plain = json.getString("plainLyrics")
                    return@withContext plain.lines().map { LyricsLine(0, 0, it.trim()) }
                }
            }
        } catch (_: Exception) {
            // Network fallback
        }
        emptyList()
    }

    fun parseLrc(content: String): List<LyricsLine> {
        if (content.isBlank()) return emptyList()

        val rawLines = mutableListOf<Pair<Long, String>>()

        content.lines().forEach { line ->
            val matcher = timestampPattern.matcher(line)
            val matches = mutableListOf<Long>()

            while (matcher.find()) {
                val min = matcher.group(1)?.toLongOrNull() ?: 0L
                val sec = matcher.group(2)?.toLongOrNull() ?: 0L
                val msStr = matcher.group(3)
                val ms = when (msStr?.length) {
                    2 -> (msStr.toLongOrNull() ?: 0L) * 10L
                    3 -> msStr.toLongOrNull() ?: 0L
                    else -> 0L
                }
                matches.add((min * 60 + sec) * 1000 + ms)
            }

            if (matches.isNotEmpty()) {
                val cleanText = timestampPattern.matcher(line).replaceAll("").trim()
                matches.forEach { ts ->
                    rawLines.add(ts to cleanText)
                }
            }
        }

        val sorted = rawLines.sortedBy { it.first }
        val result = mutableListOf<LyricsLine>()

        for (i in sorted.indices) {
            val currentTs = sorted[i].first
            val nextTs = if (i < sorted.size - 1) sorted[i + 1].first else currentTs + 3000L
            val duration = (nextTs - currentTs).coerceIn(500L, 10000L)
            result.add(LyricsLine(currentTs, duration, sorted[i].second))
        }

        return result
    }
}
