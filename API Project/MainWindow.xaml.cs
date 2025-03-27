using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Net.Http;
using System.IO;
using System.Linq;
using API_Project.Models;
using API_Project.Helpers;
using LiveCharts;
using LiveCharts.Wpf;

namespace API_Project
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new();

        public MainWindow()
        {
            InitializeComponent(); // das wird am anfang gemacht damit alles aufgebaut wird
            DatabaseHelper.InitializeDatabase(); // erstellt datenbank und läd csv dateien rein
        }

        // Benötigt für die Suche 
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text.Trim(); // Speichern + trim

            if (string.IsNullOrWhiteSpace(query)) // überprüfung ob was eingegeben wurde
            {
                MessageBox.Show("Bitte gib einen Titel ein.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string dbFile = "database.db"; // pfad zur datenbank
            if (!File.Exists(dbFile)) // datei vorhanden?
            {
                MessageBox.Show("Datenbank nicht gefunden.");
                return;
            }

            using var connection = new SQLiteConnection($"Data Source={dbFile};Version=3;");
            connection.Open(); // verbindung

            // sucht den tag der passt
            using var tagCmd = new SQLiteCommand("SELECT tag, MIN(tag_id), description FROM Tag WHERE tag LIKE @tag GROUP BY tag ORDER BY MIN(tag_id) ASC LIMIT 1", connection);
            tagCmd.Parameters.AddWithValue("@tag", $"%{query}%");
            using var reader = tagCmd.ExecuteReader();

            if (!reader.Read()) // wenn kein tag gefunden
            {
                MessageBox.Show("Kein passender Tag gefunden.");
                return;
            }

            // tag infos auslesen
            string tagName = reader.GetString(0);
            int tagId = reader.GetInt32(1);
            string tagDescription = reader.GetString(2);

            TagTitle.Text = tagName; // text setzen
            TagDescription.Text = tagDescription;

            // daten aus datenbank holen
            var books = QueryBooksByTagId(tagId, connection);
            var movies = QueryMoviesByTagId(tagId, connection);
            var games = QueryGamesByTagId(tagId, connection);

            // daten anzeigen
            BooksTable.ItemsSource = books;
            MoviesTable.ItemsSource = movies;
            GamesTable.ItemsSource = games;
            LoadPieChart(books.Count, movies.Count, games.Count);

            TimelineHelper.DrawTimeline(TimelineCanvas, books, movies, games); // zeigt die zeitleiste
        }

        // zeigt wie viele bücher/filme/spiele als tortendiagramm
        private void LoadPieChart(int bookCount, int movieCount, int gameCount)
        {
            PieChart.Series = new SeriesCollection
            {
                new PieSeries { Title = "Bücher", Values = new ChartValues<int> { bookCount }, DataLabels = true },
                new PieSeries { Title = "Filme", Values = new ChartValues<int> { movieCount }, DataLabels = true },
                new PieSeries { Title = "Spiele", Values = new ChartValues<int> { gameCount }, DataLabels = true }
            };
        }

        // Diese Funktion zeigt alle Franchises die es gibt aus der Datenbank an (Überarbeiten ?)
        private void ShowFranchiseList_Click(object sender, RoutedEventArgs e)
        {
            string dbFile = "database.db";
            if (!File.Exists(dbFile))
            {
                MessageBox.Show("Datenbank nicht gefunden.");
                return;
            }

            List<string> franchiseTags = new List<string>();

            using var connection = new SQLiteConnection($"Data Source={dbFile};Version=3;");
            connection.Open();

            using var cmd = new SQLiteCommand("SELECT tag FROM Tag ORDER BY tag ASC", connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string tag = reader["tag"].ToString();
                if (!string.IsNullOrWhiteSpace(tag))
                    franchiseTags.Add(tag);
            }

            string message = string.Join("\n - ", franchiseTags);
            MessageBox.Show("Das sind alle Franchises die du sehen kannst:\n\n - " + message, "Franchise Liste", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Holt Bücher aus der datenbank
        private List<BookEntry> QueryBooksByTagId(int tagId, SQLiteConnection connection)
        {
            var results = new List<BookEntry>();
            string query = "SELECT english_title, german_title, authors, release_date, rating, number_of_reviews FROM Books WHERE tag_id = @tagId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@tagId", tagId);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new BookEntry
                {
                    english_title = reader["english_title"].ToString(),
                    german_title = reader["german_title"].ToString(),
                    authors = reader["authors"].ToString(),
                    release_date = reader["release_date"].ToString(),
                    rating = reader["rating"].ToString(),
                    number_of_reviews = reader["number_of_reviews"].ToString()
                });
            }

            return results;
        }

        // Holt filme aus datenbank
        private List<MovieEntry> QueryMoviesByTagId(int tagId, SQLiteConnection connection)
        {
            var results = new List<MovieEntry>();
            string query = "SELECT english_title, german_title, rated, released, genre, director, writer, imdb_rating FROM Movies WHERE tag_id = @tagId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@tagId", tagId);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new MovieEntry
                {
                    english_title = reader["english_title"].ToString(),
                    german_title = reader["german_title"].ToString(),
                    rated = reader["rated"].ToString(),
                    released = reader["released"].ToString(),
                    genre = reader["genre"].ToString(),
                    director = reader["director"].ToString(),
                    writer = reader["writer"].ToString(),
                    imdb_rating = reader["imdb_rating"].ToString()
                });
            }

            return results;
        }

        // Holt spiele aus datenbank
        private List<GameEntry> QueryGamesByTagId(int tagId, SQLiteConnection connection)
        {
            var results = new List<GameEntry>();
            string query = "SELECT title, involved_companies, release_date, genres, platforms, rating FROM Games WHERE tag_id = @tagId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@tagId", tagId);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new GameEntry
                {
                    title = reader["title"].ToString(),
                    involved_companies = reader["involved_companies"].ToString(),
                    release_date = reader["release_date"].ToString(),
                    genres = reader["genres"].ToString(),
                    platforms = reader["platforms"].ToString(),
                    rating = reader["rating"].ToString()
                });
            }

            return results;
        }
    }
}
