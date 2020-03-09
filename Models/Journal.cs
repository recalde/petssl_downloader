using System.Collections.Generic;

namespace petssl_downloader.Models
{
    public class Journal
    {
        public string Date { get; set; }
        public string Service { get; set; }
        public string Pet { get; set; }
        public string PetSitter { get; set; }
        public string Poop { get; set; }
        public string Pee { get; set; }
        public string Meal { get; set; }
        public string OtherActions { get; set; }
        public List<Comment> Comments { get; set; }
        public List<string> Images{ get; set; }

        public override string ToString()
        {
            return string.Format("{0}-{1}-{2}-{3}-{4}-{5}-{6}", Date, Service, Pet, PetSitter, Poop, Pee, OtherActions);
        }
    }
}
