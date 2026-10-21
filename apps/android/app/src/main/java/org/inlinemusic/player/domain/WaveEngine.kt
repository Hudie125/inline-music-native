package org.inlinemusic.player.domain

import org.inlinemusic.player.data.AppDatabase
import kotlin.random.Random

class WaveEngine(private val db: AppDatabase) {

    suspend fun generateWaveQueue(count: Int = 25): List<Track> {
        val allEntities = db.trackDao().getAllTracks()
        if (allEntities.isEmpty()) return emptyList()
        if (allEntities.size <= count) return allEntities.shuffled().map { it.toDomain() }

        val history = db.historyDao().getRecentHistory(150)
        val historyMap = history.groupBy { it.trackId }
        val now = System.currentTimeMillis()

        // Calculate top artists and genres
        val artistPlayCounts = history.groupBy { it.trackId }
            .mapNotNull { (trackId, logs) ->
                val trk = allEntities.find { it.id == trackId }
                trk?.let { it.artist to logs.size }
            }
            .groupBy({ it.first }, { it.second })
            .mapValues { (_, counts) -> counts.sum() }

        val topArtists = artistPlayCounts.entries.sortedByDescending { it.value }
            .take(6).map { it.key }.toSet()

        val scored = allEntities.map { entity ->
            var score = 50.0

            if (entity.isLiked) {
                score += 35.0
            }

            if (topArtists.contains(entity.artist)) {
                score += 20.0
            }

            historyMap[entity.id]?.let { logs ->
                score += (logs.size * 4.0).coerceAtMost(24.0)

                val avgCompletion = logs.map { it.completionRate }.average()
                if (avgCompletion >= 0.8) {
                    score += 20.0
                } else if (avgCompletion < 0.25) {
                    score -= 30.0
                }

                val mostRecent = logs.maxOf { it.playedAt }
                val hoursSince = (now - mostRecent) / (1000.0 * 3600.0)
                if (hoursSince < 2.0) {
                    score -= 45.0
                } else if (hoursSince < 6.0) {
                    score -= 20.0
                }
            }

            // Small exploratory variance
            score += Random.nextDouble(-7.0, 7.0)

            entity.toDomain() to score
        }

        return scored.sortedByDescending { it.second }
            .take(count * 2)
            .shuffled()
            .take(count)
            .map { it.first }
    }
}
