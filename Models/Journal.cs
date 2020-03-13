using System.Collections.Generic;

namespace petssl_downloader.Models
{
    public class Journal
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public string Service { get; set; }
        public string Pets { get; set; }
        public string PetSitter { get; set; }
        public string Poop { get; set; }
        public string Pee { get; set; }
        public string Meal { get; set; }
        public string OtherActions { get; set; }
        public List<Comment> Comments { get; set; }
        public List<string> Images { get; set; }

        public override string ToString()
        {
            return string.Format("{0}-{1}-{2}-{3}-{4}-{5}-{6}-{7}", Date, Time, Service, Pets, PetSitter, Poop, Pee, OtherActions);
        }
    }
}
