using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using API_Project.Models;

namespace API_Project.Helpers
{
    // Diese klasse macht die zeitleiste mit den Punkten ...
    public static class TimelineHelper
    {
        // Das ist die haupt funktion die alles zeichnet
        public static void DrawTimeline(Canvas canvas, List<BookEntry> books, List<MovieEntry> movies, List<GameEntry> games)
        {
            canvas.Children.Clear(); // canvas leer machen damit nix doppelt ist

            List<int> years = new(); // liste mit jahren

            // wir versuchen die jahre zu holen aus allen listen
            years.AddRange(books.Select(b => ParseYear(b.release_date)));
            years.AddRange(movies.Select(m => ParseYear(m.released)));
            years.AddRange(games.Select(g => ParseYear(g.release_date)));

            // wir wollen nur gültige jahre > 0
            years = years.Where(y => y > 0).Distinct().OrderBy(y => y).ToList();
            if (years.Count == 0) return;

            int minYear = years.Min();
            int maxYear = years.Max();
            int range = maxYear - minYear + 1;

            double spacing = 60; // abstand zwichen den jahren
            int maxVisibleYears = 20;
            double width = System.Math.Max(range * spacing, maxVisibleYears * spacing);
            canvas.Width = width;
            canvas.Margin = new System.Windows.Thickness(40, 0, 40, 0); // links + rechts platz

            // abstand nochmal aktualisieren
            spacing = width / range;

            // horizontale linie (die zeitleiste selber)
            var axis = new Line
            {
                X1 = 0,
                X2 = width,
                Y1 = 60,
                Y2 = 60,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvas.Children.Add(axis);

            // jedes jahr anzeigen
            for (int year = minYear; year <= maxYear; year++)
            {
                double x = (year - minYear) * spacing;

                var label = new TextBlock
                {
                    Text = year.ToString(),
                    FontSize = 12
                };

                Canvas.SetLeft(label, x - 15);
                Canvas.SetTop(label, 65);
                canvas.Children.Add(label);
            }

            // kleine runde punkte anzeigen
            void DrawMarker(int year, Brush color, string tooltip, int level = 0)
            {
                if (year < minYear || year > maxYear) return;

                double x = (year - minYear) * spacing;
                double offsetY = 50 - (level * 14); // die höhe ein bisschen variieren wenn nötig

                var marker = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = color,
                    ToolTip = tooltip
                };

                Canvas.SetLeft(marker, x - 5);
                Canvas.SetTop(marker, offsetY);
                canvas.Children.Add(marker);
            }

            // für alle bücher
            foreach (var b in books)
                DrawMarker(ParseYear(b.release_date), Brushes.Blue, b.english_title, 0);

            // für alle filme
            foreach (var m in movies)
                DrawMarker(ParseYear(m.released), Brushes.Red, m.english_title, 1);

            // für alle spiele
            foreach (var g in games)
                DrawMarker(ParseYear(g.release_date), Brushes.Green, g.title, 2);
        }

        // versucht das jahr aus nem string zu lesen
        private static int ParseYear(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return -1;

            if (int.TryParse(input.Substring(0, 4), out int year))
                return year;

            return -1;
        }
    }
}
