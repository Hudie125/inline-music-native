package org.inlinemusic.player

import android.app.Application
import org.inlinemusic.player.data.AppDatabase

class InlineApp : Application() {
    lateinit var database: AppDatabase
        private set

    override fun onCreate() {
        super.onCreate()
        database = AppDatabase.getInstance(this)
    }
}
