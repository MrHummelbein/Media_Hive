using System;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;

namespace API_Project.Helpers
{
    // Diese klasse kümmert sich um die datenbank und csv dateien
    public static class DatabaseHelper
    {
        public static void InitializeDatabase()
        {
            string databaseFile = "database.db";

            if (!File.Exists(databaseFile))
            {
                SQLiteConnection.CreateFile(databaseFile); // macht neue leere datei
            }

            using var connection = new SQLiteConnection($"Data Source={databaseFile};Version=3;");
            connection.Open();

            CreateTables(connection); // erstellt tabellen
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\csv");

            // überprüft ob ordner da ist
            if (!Directory.Exists(basePath))
            {
                MessageBox.Show($"CSV-Ordner nicht gefunden: {basePath}");
                return;
            }

            // csv dateien laden
            ImportTagCSV(Path.Combine(basePath, "tag.csv"), connection);
            ImportCSV(Path.Combine(basePath, "books.csv"), "Books", connection);
            ImportCSV(Path.Combine(basePath, "games.csv"), "Games", connection);
            ImportCSV(Path.Combine(basePath, "movie.csv"), "Movies", connection);
        }

        // hier werden tabellen erstellt für alle sachen
        static void CreateTables(SQLiteConnection connection)
        {
            string dropTables = @"
                DROP TABLE IF EXISTS Games;
                DROP TABLE IF EXISTS Books;
                DROP TABLE IF EXISTS Movies;
                DROP TABLE IF EXISTS Tag;";

            using var dropCommand = new SQLiteCommand(dropTables, connection);
            dropCommand.ExecuteNonQuery();

            // neue tabellen machen
            string tagTable = @"CREATE TABLE Tag (
                tag_id INTEGER PRIMARY KEY AUTOINCREMENT,
                tag TEXT UNIQUE,
                description TEXT
            );";

            string books = @"CREATE TABLE Books (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                english_title TEXT,
                german_title TEXT,
                authors TEXT,
                release_date TEXT,
                rating TEXT,
                number_of_reviews INTEGER,
                genre TEXT,
                subjects TEXT,
                cover_link TEXT,
                tag_id INTEGER,
                FOREIGN KEY(tag_id) REFERENCES Tag(tag_id)
            );";

            string movies = @"CREATE TABLE Movies (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                english_title TEXT,
                german_title TEXT,
                rated TEXT,
                released TEXT,
                genre TEXT,
                director TEXT,
                writer TEXT,
                type TEXT,
                imdb_rating REAL,
                tag_id INTEGER,
                FOREIGN KEY(tag_id) REFERENCES Tag(tag_id)
            );";

            string games = @"CREATE TABLE Games (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT,
                genres TEXT,
                release_date TEXT,
                platforms TEXT,
                involved_companies TEXT,
                rating TEXT,
                tag_id INTEGER,
                FOREIGN KEY(tag_id) REFERENCES Tag(tag_id)
            );";

            using var command = new SQLiteCommand(connection);
            command.CommandText = tagTable;
            command.ExecuteNonQuery();
            command.CommandText = books;
            command.ExecuteNonQuery();
            command.CommandText = movies;
            command.ExecuteNonQuery();
            command.CommandText = games;
            command.ExecuteNonQuery();
        }

        // die datei mit den tags laden
        static void ImportTagCSV(string filePath, SQLiteConnection connection)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Datei {filePath} nicht gefunden.");
                return;
            }

            try
            {
                using var reader = new StreamReader(filePath);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    MissingFieldFound = null,
                };

                using var csv = new CsvReader(reader, config);
                var records = csv.GetRecords<dynamic>().ToList();

                foreach (var record in records)
                {
                    var dict = (IDictionary<string, object>)record;
                    string tag = dict.ContainsKey("tag") ? dict["tag"]?.ToString() ?? "" : "";
                    string description = dict.ContainsKey("description") ? dict["description"]?.ToString() ?? "" : "";

                    if (string.IsNullOrWhiteSpace(tag)) continue;

                    using var insert = new SQLiteCommand("INSERT OR IGNORE INTO Tag (tag, description) VALUES (@tag, @desc);", connection);
                    insert.Parameters.AddWithValue("@tag", tag);
                    insert.Parameters.AddWithValue("@desc", description);
                    insert.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Importieren von tag.csv: {ex.Message}");
            }
        }

        // andere csv dateien laden (bücher, filme, spiele)
        static void ImportCSV(string filePath, string tableName, SQLiteConnection connection)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Datei {filePath} nicht gefunden.");
                return;
            }

            try
            {
                using var reader = new StreamReader(filePath);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    MissingFieldFound = null,
                };

                using var csv = new CsvReader(reader, config);
                var records = csv.GetRecords<dynamic>().ToList();

                foreach (var record in records)
                {
                    var dict = (IDictionary<string, object>)record;
                    string tag = dict.ContainsKey("tag") ? dict["tag"]?.ToString() ?? "" : "";
                    int tagId = GetOrCreateTag(tag, connection);

                    dict["tag_id"] = tagId;
                    dict.Remove("tag");

                    var columns = string.Join(",", dict.Keys);
                    var parameterNames = string.Join(",", dict.Keys.Select((k, i) => $"@param{i}"));
                    var values = dict.Values.Select((v, i) => new SQLiteParameter($"@param{i}", v ?? DBNull.Value)).ToArray();

                    string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameterNames});";

                    using var command = new SQLiteCommand(query, connection);
                    command.Parameters.AddRange(values);
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show($"Fehler beim Importieren von {filePath}: {ex.Message}");
            }
        }

        // tag id suchen oder neu machen
        static int GetOrCreateTag(string tag, SQLiteConnection connection)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return -1;

            using var checkCmd = new SQLiteCommand("SELECT tag_id FROM Tag WHERE tag = @tag", connection);
            checkCmd.Parameters.AddWithValue("@tag", tag);
            var existingId = checkCmd.ExecuteScalar();

            if (existingId != null) return Convert.ToInt32(existingId);

            using var insertCmd = new SQLiteCommand("INSERT INTO Tag (tag) VALUES (@tag); SELECT last_insert_rowid();", connection);
            insertCmd.Parameters.AddWithValue("@tag", tag);
            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }
    }
}
