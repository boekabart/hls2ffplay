using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace hls2ffPlay
{
    class MyWebClient : WebClient
    {
        Uri _responseUri;

        public Uri ResponseUri
        {
            get { return _responseUri; }
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);
            _responseUri = response.ResponseUri;
            return response;
        }
    }

    class Program
    {
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        static void Main(string[] args)
        {
            string url = args.FirstOrDefault();
            if (url == null)
            {
                Console.Error.WriteLine("Must provide URL to manifest");
                return;
            }
            var uri = new Uri(url);
            var myWebClient = new MyWebClient();
            var m3u8 = myWebClient.DownloadString(uri);
            Console.WriteLine(myWebClient.ResponseUri);
            Console.WriteLine(m3u8);
            var relUri = m3u8.Split('\r', '\n').FirstOrDefault(s => s.Contains(".m3u8"));
            var oldUri = myWebClient.ResponseUri ?? uri;
            var streamUri = relUri == null ? oldUri : new Uri(oldUri, relUri);
            Console.WriteLine(streamUri);
            var realm3u8 = myWebClient.DownloadString(streamUri);
            Console.WriteLine(realm3u8);
            if (realm3u8.Contains("EXT-X-KEY"))
            {
                Console.WriteLine("Not even trying to play encrypted stream");
                return;
            }
            var ffPlay = Path.Combine(AssemblyDirectory, "ffplay.exe");
            if (File.Exists(ffPlay))
                Process.Start(ffPlay, streamUri.ToString());
            else
                Console.WriteLine("No ffplay.exe found");
        }
    }
}
