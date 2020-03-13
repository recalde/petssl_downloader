using System.Net.Http;
using System.Collections.Generic;
using System.Net;
using System;
using System.Linq;
using System.IO;

namespace petssl_downloader
{
    public class Configuration
    {
        public string JournalDirectory { get; set; }
        public string ImageDirectory { get; set; }
        public int JournalThreads { get; set; }
        public int ImageThreads { get; set; }
        public Uri WebsiteUri { get; set; }
    }
}