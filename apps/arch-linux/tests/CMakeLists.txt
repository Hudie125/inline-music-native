find_package(Qt6 REQUIRED COMPONENTS Core Sql Network)

add_executable(TestLrcParser
    TestLrcParser.cpp
    ../src/lyrics/LyricsService.cpp
    ../src/db/DatabaseManager.cpp
)

target_include_directories(TestLrcParser PRIVATE ../src)
target_link_libraries(TestLrcParser PRIVATE Qt6::Core Qt6::Sql Qt6::Network)

add_test(NAME LrcParserTest COMMAND TestLrcParser)
