#include <QGuiApplication>
#include <QQmlApplicationEngine>
#include <QQmlContext>
#include <QIcon>
#include "audio/GStreamerAudioEngine.hpp"
#include "db/DatabaseManager.hpp"
#include "mpris/MprisPlayer.hpp"
#include "wave/WaveEngine.hpp"
#include "lyrics/LyricsService.hpp"

int main(int argc, char* argv[]) {
    QGuiApplication app(argc, argv);
    app.setApplicationName("inline");
    app.setOrganizationName("inlineMusic");
    app.setDesktopFileName("inline");
    app.setWindowIcon(QIcon(":/assets/icon/inline.png"));

    // 1. Initialize Database
    auto db = std::make_unique<inline_music::DatabaseManager>();
    db->initialize();

    // 2. Initialize Audio Engine & MPRIS
    auto audioEngine = std::make_unique<inline_music::GStreamerAudioEngine>();
    audioEngine->initialize();

    auto mpris = std::make_unique<inline_music::MprisManager>(audioEngine.get());
    mpris->registerService();

    // 3. Initialize Wave and Lyrics
    auto waveEngine = std::make_unique<inline_music::WaveEngine>(db.get());
    auto lyricsService = std::make_unique<inline_music::LyricsService>(db.get());

    // 4. Setup QML Engine
    QQmlApplicationEngine engine;
    auto rootContext = engine.rootContext();

    rootContext->setContextProperty("audioEngine", audioEngine.get());
    rootContext->setContextProperty("waveEngine", waveEngine.get());
    rootContext->setContextProperty("lyricsService", lyricsService.get());
    rootContext->setContextProperty("dbManager", db.get());

    const QUrl url(QStringLiteral("qrc:/main.qml"));
    QObject::connect(
        &engine,
        &QQmlApplicationEngine::objectCreated,
        &app,
        [url](QObject* obj, const QUrl& objUrl) {
            if (!obj && url == objUrl)
                QCoreApplication::exit(-1);
        },
        Qt::QueuedConnection
    );

    engine.load(url);

    return app.exec();
}
