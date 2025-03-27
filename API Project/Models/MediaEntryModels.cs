using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Project.Models
{
    public class BookEntry
    {
        public string english_title { get; set; }
        public string german_title { get; set; }
        public string authors { get; set; }
        public string release_date { get; set; }
        public string rating { get; set; }
        public string number_of_reviews { get; set; }
    }

    public class MovieEntry
    {
        public string english_title { get; set; }
        public string german_title { get; set; }
        public string rated { get; set; }
        public string released { get; set; }
        public string genre { get; set; }
        public string director { get; set; }
        public string writer { get; set; }
        public string imdb_rating { get; set; }
    }

    public class GameEntry
    {
        public string title { get; set; }
        public string involved_companies { get; set; }
        public string release_date { get; set; }
        public string genres { get; set; }
        public string platforms { get; set; }
        public string rating { get; set; }
    }
}

