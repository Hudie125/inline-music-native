#include <QCoreApplication>
#include <QVariantList>
#include <cassert>
#include <iostream>
#include "lyrics/LyricsService.hpp"

int main(int argc, char* argv[]) {
    QCoreApplication app(argc, argv);

    QString lrc = "[00:10.50]First line\n[00:20.00]Second line\n";
    QVariantList parsed = inline_music::LyricsService::parseLrc(lrc);

    assert(parsed.size() == 2);
    assert(parsed[0].toMap()["timestampMs"].toLongLong() == 10500);
    assert(parsed[0].toMap()["text"].toString() == "First line");
    assert(parsed[1].toMap()["timestampMs"].toLongLong() == 20000);
    assert(parsed[1].toMap()["text"].toString() == "Second line");

    std::cout << "All Arch Linux LRC parser tests passed successfully!" << std::endl;
    return 0;
}
