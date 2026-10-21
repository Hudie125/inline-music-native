# Keep Media3 components
-keep class androidx.media3.** { *; }

# Keep Room generated implementations
-keep class * extends androidx.room.RoomDatabase
-dontwarn androidx.room.paging.**

# Keep models and entities
-keep class org.inlinemusic.player.data.** { *; }
-keep class org.inlinemusic.player.domain.** { *; }
