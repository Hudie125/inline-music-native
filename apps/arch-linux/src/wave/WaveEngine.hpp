#pragma once

#include <QObject>
#include <QList>
#include <QVariantList>
#include "../db/DatabaseManager.hpp"
#include "../db/Models.hpp"

namespace inline_music {

class WaveEngine : public QObject {
    Q_OBJECT

public:
    explicit WaveEngine(DatabaseManager* db, QObject* parent = nullptr);

    Q_INVOKABLE QVariantList generateWave(int count = 25);

private:
    DatabaseManager* m_db;
};

} // namespace inline_music
