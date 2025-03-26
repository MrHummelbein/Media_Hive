using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Animation;
using System.Data.SQLite;
using CsvHelper;
using System.Dynamic;
using CsvHelper.Configuration;

namespace API_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        // für die Darstellung in der Tabelle nötig 
        private class ResultRow
        {
            public string Book { get; set; }
            public string Movie { get; set; }
            public string Game { get; set; }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Hier wird der Title von der Searchbox gespeichert
            string query = SearchBox.Text;

            // Validierung der Eingabe
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Es wird ein Titel benötigt um die Suche zu Starten!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Ladebalken anzeigen
                LoadingBar.Visibility = Visibility.Visible;
                LoadingBar.Value = 0;

                // Parallele API-Aufrufe
                var openLibraryTask = SearchOpenLibary(query);
                var omdbTask = SearchOMDB(query);
                var igdbTask = SearchIGDB(query);

                var tasks = new List<Task> { openLibraryTask, omdbTask, igdbTask };
                int totalTasks = tasks.Count;

                // Fortschritt überwachen
                foreach (var task in tasks)
                {
                    await task;
                    LoadingBar.Value += 100 / totalTasks; // Fortschritt erhöhen
                }

                // Ergebnisse sammeln
                var books = openLibraryTask.Result;
                var movies = omdbTask.Result;
                var games = igdbTask.Result;

                // Füge die Daten zu einem tabellarischen Layout zusammen
                var maxRows = Math.Max(books.Count, Math.Max(movies.Count, games.Count));
                var tableData = new List<ResultRow>();

                for (int i = 0; i < maxRows; i++)
                {
                    tableData.Add(new ResultRow
                    {
                        Book = i < books.Count ? $"{books[i].Title}\n{books[i].Author}\n{books[i].Year}" : "",
                        Movie = i < movies.Count ? $"{movies[i].Title}\n{movies[i].Year}" : "",
                        Game = i < games.Count ? $"{games[i].Title}\n{games[i].Developer}\n{games[i].Year}" : ""
                    });
                }

                ResultsDataGrid.ItemsSource = tableData;

                // In einer .csv speichern
                SaveResultsToCsv(books, movies, games);
                MessageBox.Show("Ergebnisse wurden erfolgreich gespeichert!", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der API-Abfrage: {ex.Message}", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Ladebalken ausblenden
                LoadingBar.Visibility = Visibility.Hidden;
                LoadingBar.Value = 0;
            }
        }

        // API abfrage mit der OpenLibary API
        private async Task<List<(string Title, string Author, string Year)>> SearchOpenLibary(string query)
        {
            using var client = new HttpClient();
            string url = $"https://openlibrary.org/search.json?title={Uri.EscapeDataString(query)}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            var books = new List<(string Title, string Author, string Year)>();

            foreach (var book in data.RootElement.GetProperty("docs").EnumerateArray())
            {
                string title = book.GetProperty("title").GetString() ?? "Unbekannt";
                string year = book.TryGetProperty("first_publish_year", out var yearProp) ? yearProp.GetInt32().ToString() : "Unbekannt";
                string author = book.TryGetProperty("author_name", out var authorProp) ? string.Join(", ", authorProp.EnumerateArray().Select(a => a.GetString())) : "Unbekannt";
                var languages = book.TryGetProperty("language", out var langProp) ? langProp.EnumerateArray().Select(l => l.GetString()).ToList() : new List<string>();

                // Filter: Originalversion und deutsche Version
                if (languages.Contains("eng") || languages.Contains("ger"))
                {
                    // Gruppiere nur Serien und wichtige Werke
                    if (book.TryGetProperty("series", out var seriesProp) || book.TryGetProperty("works", out var worksProp))
                    {
                        books.Add((title, author, year));
                    }
                    else if (books.All(b => b.Title != title)) // Doppelte vermeiden
                    {
                        books.Add((title, author, year));
                    }
                }
            }

            // Sortiere nach Jahr, um die Originalversion zuerst zu zeigen
            return books.OrderBy(b => b.Year).ToList();
        }

        // API abfrage bei OMDB
        private async Task<List<(string Title, string Year)>> SearchOMDB(string query)
        {
            using var client = new HttpClient();
            string apiKey = "fc4dd297";
            string url = $"http://www.omdbapi.com/?s={Uri.EscapeDataString(query)}&apikey={apiKey}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            var results = new List<(string Title, string Year)>();
            if (data.RootElement.TryGetProperty("Search", out var movies))
            {
                foreach (var movie in movies.EnumerateArray())
                {
                    string title = movie.GetProperty("Title").GetString() ?? "Unbekannt";
                    string year = movie.GetProperty("Year").GetString() ?? "Unbekannt";
                    results.Add((title, year));
                }
            }

            return results;
        }

        // Hier wird der Token generiert für IGDB
        private async Task<string> GetIGDBAccessToken()
        {
            using var client = new HttpClient();
            string clientId = "8vt4l50oswhbbtz935u1k039bknlrm";
            string clientSecret = "p4dd9egfrriz79xybm8lozclvzvjwt";
            string url = $"https://id.twitch.tv/oauth2/token?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials";

            var response = await client.PostAsync(url, null);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            return data.RootElement.GetProperty("access_token").GetString();
        }

        // API abfrage bei IGDB
        private async Task<List<(string Title, string Developer, string Year)>> SearchIGDB(string query)
        {
            string accessToken = await GetIGDBAccessToken();

            using var client = new HttpClient();
            string clientId = "8vt4l50oswhbbtz935u1k039bknlrm";

            client.DefaultRequestHeaders.Add("Client-ID", clientId);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            string url = "https://api.igdb.com/v4/games";
            var requestBody = $"search \"{query}\"; fields name,first_release_date,involved_companies.company.name; limit 10;";

            var content = new StringContent(requestBody);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            var results = new List<(string Title, string Developer, string Year)>();

            foreach (var game in data.RootElement.EnumerateArray())
            {
                string title = game.GetProperty("name").GetString() ?? "Unbekannt";
                string year = game.TryGetProperty("first_release_date", out var yearProp) ? DateTimeOffset.FromUnixTimeSeconds(yearProp.GetInt64()).Year.ToString() : "Unbekannt";
                string developer = game.TryGetProperty("involved_companies", out var companies) ? string.Join(", ", companies.EnumerateArray().Select(c => c.GetProperty("company").GetProperty("name").GetString())) : "Unbekannt";
                results.Add((title, developer, year));
            }

            return results;
        }

        private void SaveResultsToCsv(
            List<(string Title, string Author, string Year)> books,
            List<(string Title, string Year)> movies,
            List<(string Title, string Developer, string Year)> games)
        {
            string filePath = "results.csv";
            var lines = new List<string> { "Kategorie,Titel,Autor/Entwickler,Erscheinungsjahr" };

            // Bücher in CSV-Format umwandeln
            lines.AddRange(books.Select(b => $"Buch,{b.Title},{b.Author},{b.Year}"));

            // Filme in CSV-Format umwandeln
            lines.AddRange(movies.Select(m => $"Film,{m.Title},,{m.Year}"));

            // Spiele in CSV-Format umwandeln
            lines.AddRange(games.Select(g => $"Spiel,{g.Title},{g.Developer},{g.Year}"));

            // CSV-Datei schreiben
            File.WriteAllLines(filePath, lines);
        }

        private static void ReadCsvFiles()
        {
            string databaseFile = "database.db";
            if (!File.Exists(databaseFile))
            {
                SQLiteConnection.CreateFile(databaseFile);
            }

            using var connection = new SQLiteConnection($"Data Source={databaseFile};Version=3;");
            connection.Open();
            CreateTables(connection);
            ImportCSV("../../../csv/games.csv", "Games", connection);
            ImportCSV("../../../csv/books.csv", "Books", connection);
            ImportCSV("../../../csv/movies.csv", "Movies", connection);
        }

        static void CreateTables(SQLiteConnection connection)
        {
            string dropTables = @"
                                DROP TABLE IF EXISTS Games;
                                DROP TABLE IF EXISTS Books;
                                DROP TABLE IF EXISTS Movies;
                                DROP TABLE IF EXISTS Tags;";

            using var dropCommand = new SQLiteCommand(dropTables, connection);
            dropCommand.ExecuteNonQuery();

            string tags = @"CREATE TABLE Tags (
                            tag_id INTEGER PRIMARY KEY AUTOINCREMENT,
                            tag TEXT UNIQUE);";

            string games = @"CREATE TABLE Games (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            title TEXT,
                            genres TEXT,
                            release_date TEXT,
                            platforms TEXT,
                            involved_companies TEXT,
                            rating TEXT,
                            tag_id INTEGER,
                            tag TEXT,
                            FOREIGN KEY(tag_id) REFERENCES Tags(tag_id));";

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
                            tag_id INTEGER,
                            isbn TEXT,
                            cover_link TEXT,
                            cover_id TEXT,
                            shop_link TEXT,
                            tag TEXT,
                            FOREIGN KEY(tag_id) REFERENCES Tags(tag_id));";

            string movies = @"CREATE TABLE Movies (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            english_title TEXT,
                            german_title TEXT,
                            year INTEGER,
                            rated TEXT,
                            released TEXT,
                            genre TEXT,
                            director TEXT,
                            writer TEXT,
                            type TEXT,
                            imdb_rating REAL,
                            tag_id INTEGER,
                            website TEXT,
                            tag TEXT,
                            FOREIGN KEY(tag_id) REFERENCES Tags(tag_id));";

            using var command = new SQLiteCommand(connection);
            command.CommandText = tags;
            command.ExecuteNonQuery();
            command.CommandText = games;
            command.ExecuteNonQuery();
            command.CommandText = books;
            command.ExecuteNonQuery();
            command.CommandText = movies;
            command.ExecuteNonQuery();
        }

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
                    Delimiter = (tableName == "Games") ? "," : ";",     //löschen wenn die csv semikolon nutzt
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
                MessageBox.Show($"Error importing CSV-File: {ex.Message}");
            }
        }

        static int GetOrCreateTag(string tag, SQLiteConnection connection)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                tag = "Sonstige"; 
            }

            using var checkCmd = new SQLiteCommand("SELECT tag_id FROM Tags WHERE tag = @tag", connection);
            checkCmd.Parameters.AddWithValue("@tag", tag);
            var existingId = checkCmd.ExecuteScalar();

            if (existingId != null) return Convert.ToInt32(existingId);

            using var insertCmd = new SQLiteCommand("INSERT INTO Tags (tag) VALUES (@tag); SELECT last_insert_rowid();", connection);
            insertCmd.Parameters.AddWithValue("@tag", tag);
            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ReadCsvFiles();
        }
    }
}