package org.inlinemusic.player.audio

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.media.audiofx.Equalizer
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.media3.common.AudioAttributes
import androidx.media3.common.C
import androidx.media3.common.MediaItem
import androidx.media3.common.Player
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.session.MediaSession
import androidx.media3.session.MediaSessionService
import org.inlinemusic.player.MainActivity
import org.inlinemusic.player.R

class InlineMediaService : MediaSessionService() {

    private var mediaSession: MediaSession? = null
    private lateinit var player: ExoPlayer
    private var equalizer: Equalizer? = null

    companion object {
        const val CHANNEL_ID = "inline_playback_channel"
        const val NOTIFICATION_ID = 1001
    }

    override fun onCreate() {
        super.onCreate()

        createNotificationChannel()

        // Configure ExoPlayer with automatic audio focus & noisy receiver
        val audioAttributes = AudioAttributes.Builder()
            .setContentType(C.AUDIO_CONTENT_TYPE_MUSIC)
            .setUsage(C.USAGE_MEDIA)
            .build()

        player = ExoPlayer.Builder(this)
            .setAudioAttributes(audioAttributes, true) // true = handle audio focus automatically!
            .setHandleAudioBecomingNoisy(true)        // true = pause when headphones unplugged!
            .build()

        // Attach hardware audio effects equalizer
        try {
            equalizer = Equalizer(0, player.audioSessionId).apply {
                enabled = true
            }
        } catch (_: Exception) {
            // Equalizer effect unsupported on some virtual devices
        }

        val sessionActivityIntent = PendingIntent.getActivity(
            this,
            0,
            Intent(this, MainActivity::class.java),
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )

        mediaSession = MediaSession.Builder(this, player)
            .setSessionActivity(sessionActivityIntent)
            .setCallback(CustomMediaSessionCallback())
            .build()
    }

    override fun onGetSession(controllerInfo: MediaSession.ControllerInfo): MediaSession? {
        return mediaSession
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                getString(R.string.channel_name),
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = getString(R.string.channel_description)
                setShowBadge(false)
            }
            val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            manager.createNotificationChannel(channel)
        }
    }

    override fun onDestroy() {
        mediaSession?.run {
            player.release()
            release()
            mediaSession = null
        }
        equalizer?.release()
        super.onDestroy()
    }

    private inner class CustomMediaSessionCallback : MediaSession.Callback {
        override fun onAddMediaItems(
            mediaSession: MediaSession,
            controller: MediaSession.ControllerInfo,
            mediaItems: MutableList<MediaItem>
        ): com.google.common.util.concurrent.ListenableFuture<MutableList<MediaItem>> {
            val updatedMediaItems = mediaItems.map { mediaItem ->
                mediaItem.buildUpon()
                    .setUri(mediaItem.requestMetadata.mediaUri)
                    .build()
            }.toMutableList()
            return com.google.common.util.concurrent.Futures.immediateFuture(updatedMediaItems)
        }
    }
}
