using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

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
                var results = new List<string>();

                // OpenLibary API 
                var bookResult = await SearchOpenLibary(query);
                if (!string.IsNullOrEmpty(bookResult))
                    results.Add(bookResult);
                
                // OMDB API
                var movieResults = await SearchOMDB(query);
                results.AddRange(movieResults);
                
                // IGDB API
                var gameResults = await SearchIGDB(query);
                results.AddRange(gameResults);

                // Ergebnisse Anzeigen
                SearchResults.Text = string.Join("\n\n", results);

                // In einer .csv speichern
                SaveResultsToCsv(results);
                MessageBox.Show("Ergebnisse wurden erfolgreich gespeichert!", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der API-Abfrage: {ex.Message}", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // API abfrage mit der OpenLibary API
        private async Task<string> SearchOpenLibary(string query)
        {
            using var client = new HttpClient();
            string url = $"https://openlibrary.org/search.json?title={Uri.EscapeDataString(query)}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            var book = data.RootElement.GetProperty("docs").EnumerateArray().FirstOrDefault();
            if (book.ValueKind != JsonValueKind.Undefined)
            {
                string title = book.GetProperty("title").GetString() ?? "Der Title existiert nicht!";
                string year = book.TryGetProperty("first_publish_year", out var yearProp) ? yearProp.GetInt32().ToString() : "Veröffentlichungsjahr ist unbekannt";
                string author = book.TryGetProperty("author_name", out var authorProp) ? string.Join(", ", authorProp.EnumerateArray().Select(a => a.GetString())) : "Der Author ist Unbekannt!";
                return $"Buch: \nTitel: {title} \nErscheinungsjahr: {year} \nAutor: {author}";
            }

            return $"Keine Bücher mit dem Titel '{query}' gefunden";
        }

        // API abfrage bei OMDB
        private async Task<List<string>> SearchOMDB(string query)
        {
            using var client = new HttpClient();
            string apiKey = "fc4dd297";
            string url = $"http://www.omdbapi.com/?s={Uri.EscapeDataString(query)}&apikey={apiKey}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            var results = new List<string>();
            if (data.RootElement.TryGetProperty("Search", out var movies))
            {
                foreach (var movie in movies.EnumerateArray())
                {
                    string title = movie.GetProperty("Title").GetString() ?? "Der Title existiert nicht!";
                    string year = movie.GetProperty("Year").GetString() ?? "Veröffentlichungsjahr ist unbekannt";
                    results.Add($"Film:\nTitel: {title}\nErscheinungsjahr: {year}");
                }
            }
            else
            {
                results.Add("Keine Filme gefunden.");
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
        private async Task<List<string>> SearchIGDB(string query)
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

            var results = new List<string>();
            foreach (var game in data.RootElement.EnumerateArray())
            {
                string title = game.GetProperty("name").GetString() ?? "Unbekannt";
                string year = game.TryGetProperty("first_release_date", out var yearProp) ? DateTimeOffset.FromUnixTimeSeconds(yearProp.GetInt64()).Year.ToString() : "Unbekannt";
                string developer = "Entwickler unbekannt"; // Erweiterung erforderlich für genauere Informationen
                string publisher = "Publisher unbekannt"; // Erweiterung erforderlich
                results.Add($"Spiel:\nTitel: {title}\nErscheinungsjahr: {year}\nEntwickler: {developer}\nPublisher: {publisher}");
            }

            return results;
        }

        private void SaveResultsToCsv(List<string> results)
        {
            string filePath = "results.csv";
            File.WriteAllLines(filePath, results);
        }
    }
}