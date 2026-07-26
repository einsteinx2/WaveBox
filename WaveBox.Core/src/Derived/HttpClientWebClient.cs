using System;
using System.IO;
using System.Net.Http;

namespace WaveBox.Core.Derived {
    // Replacement for the Mono-era TimedWebClient/LinuxWebClient pair.
    public class HttpClientWebClient : IWebClient {
        private readonly HttpClient client;

        public HttpClientWebClient(int timeoutMilliseconds) {
            this.client = new HttpClient() {
                Timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds)
            };
        }

        public string DownloadString(string address) {
            return this.client.GetStringAsync(address).GetAwaiter().GetResult();
        }

        public void DownloadFile(string address, string fileName) {
            using (HttpResponseMessage response = this.client.GetAsync(address).GetAwaiter().GetResult()) {
                response.EnsureSuccessStatusCode();
                using (FileStream fs = File.Create(fileName))
                using (Stream stream = response.Content.ReadAsStream()) {
                    stream.CopyTo(fs);
                }
            }
        }
    }
}
