using System.Net.Http;
using System.Collections.Generic;
using System.Net;
using System;
using System.Linq;
using System.IO;

namespace petssl_downloader
{
    public class WebsiteDriver
    {
        private readonly HttpClient httpClient;
        private readonly CookieContainer cookieContainer;
        private readonly Uri websiteUri;
    
        public WebsiteDriver()
        {
            cookieContainer = new CookieContainer();
            websiteUri = new Uri("https://hshpetcare.petssl.com");
            var httpClientHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            };
            httpClient = new HttpClient(httpClientHandler); 
            httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public Cookie[] GetCookies()
        {
            return cookieContainer.GetCookies(websiteUri).Cast<Cookie>().ToArray();
        }

        public void Login(string username, string password)
        {
            var url = "https://hshpetcare.petssl.com/login";
            Console.WriteLine("[POST] {0}", url);

            var userCookie = new Cookie("USER", username);
            cookieContainer.Add(websiteUri, userCookie);

            // Get Cookie PHPSESSID, AWSELB, AWSELBCORS
            var postValues = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password },
                { "btn_submit", "true" }
            };

            var postContent = new FormUrlEncodedContent(postValues);

            var responseTask = httpClient.PostAsync(url, postContent);
            responseTask.Wait();
            var response = responseTask.Result;

            var readContentTask = response.Content.ReadAsStringAsync();
            readContentTask.Wait();
            var content = readContentTask.Result;
        }

        public void Home()
        {
            var url = "https://hshpetcare.petssl.com/";
            Console.WriteLine("[GET] {0}", url);
            var responseTask = httpClient.GetAsync(url);
            responseTask.Wait();
        }

        public void Client()
        {
            // Get Cookie -  ADMINLOGIN
            var url = "https://hshpetcare.petssl.com/clients/";
            Console.WriteLine("[GET] {0}", url);
            var responseTask = httpClient.GetAsync(url);
            responseTask.Wait();
        }

        public string Images(int pageNumber)
        {
            var url = "https://hshpetcare.petssl.com/admin/images";
            if (pageNumber != 0)
            {
                int start = pageNumber * 96;
                int end = start + 96;
                url += string.Format("?v=gallery&s={0}&s={1}", end, start);
            }
            Console.WriteLine("[GET] {0}", url);
            var responseTask = httpClient.GetAsync(url);
            responseTask.Wait();
            var response = responseTask.Result;

            var readContentTask = response.Content.ReadAsStringAsync();
            readContentTask.Wait();
            var content = readContentTask.Result;
            return content;
        }


        public string Schedule(DateTime startDate)
        {
            var url = "https://hshpetcare.petssl.com/admin/my-schedule?v=cal&l=5day&d=";
            url = url + startDate.ToString("yyyy-MM-dd");
            Console.WriteLine("[GET] {0}", url);

            var responseTask = httpClient.GetAsync(url);
            responseTask.Wait();
            var response = responseTask.Result;

            var readContentTask = response.Content.ReadAsStringAsync();
            readContentTask.Wait();
            var content = readContentTask.Result;
            return content;
        }

        public string Journal(string href)
        {
            var url = "https://hshpetcare.petssl.com" + href;
            Console.WriteLine("[GET] {0}", url);

            var responseTask = httpClient.GetAsync(url);
            responseTask.Wait();
            var response = responseTask.Result;

            var readContentTask = response.Content.ReadAsStringAsync();
            readContentTask.Wait();
            var content = readContentTask.Result;
            return content;
        }

        

        public string DownloadImage(string src)
        {
            string url = "https://s3.amazonaws.com/petssl.com/hshpetcare/images/uploads/Content/" + src;
            Console.WriteLine("[GET] {0}", url);

            string localPath = @"C:\git\petssl-downloader\download\" + src;
            if (!File.Exists(localPath))
            {
                var responseTask = httpClient.GetAsync(url);
                responseTask.Wait();
                var response = responseTask.Result;

                var readContentTask = response.Content.ReadAsByteArrayAsync();
                readContentTask.Wait();
                var content = readContentTask.Result;
                File.WriteAllBytes(localPath, content);
            }

            return localPath;
        }

    
    }
}