using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WaveBox.Core.Derived {
    public interface IWebClient {
        string DownloadString(string uri);

        void DownloadFile(string address, string path);
    }
}
