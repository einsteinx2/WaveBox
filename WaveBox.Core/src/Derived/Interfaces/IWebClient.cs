using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WaveBox.Core.Derived {
    public interface IWebClient {
        // Download a string
        string DownloadString (string address);

        // Download a file to a specified path
        void DownloadFile (string address, string fileName);
    }
}
