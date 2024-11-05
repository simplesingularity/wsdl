using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace wsdl
{
    public static class SteamAPI
    {

        public static ILogger logger { get; set; }
        public static bool IsUpdated { get; set; } = false;

        static DirectoryInfo cur_dir;
        static DirectoryInfo steamcmd_dir;
        static Queue<DownloadRequest> downloads = new Queue<DownloadRequest>();
        static Regex quote_pattern = new Regex("\\\"(.*?)\\\"");

        public static event EventHandler<DownloadRequest> FileDownloaded;
        public static event EventHandler<DownloadRequest> ErrorDownloading;

        const string steamcmd_url = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";

        static SteamAPI()
        {
            cur_dir = new DirectoryInfo(Assembly.GetEntryAssembly().Location).Parent;
            steamcmd_dir = cur_dir.CreateSubdirectory("steamcmd");
        }

        private static bool IsInitialized()
        {
            string steamclient = Path.Combine(steamcmd_dir.FullName, "steamclient.dll");
            string steamclient64 = Path.Combine(steamcmd_dir.FullName, "steamclient64.dll");
            return File.Exists(steamclient) && File.Exists(steamclient64);
        }

        public static void EnsureSteamCMDInstalled()
        {
            if (File.Exists(Path.Combine(steamcmd_dir.FullName, "steamcmd.exe")))
            {
                WriteLine("SteamCMD detected in folder");
                return;
            }

            WriteLine("SteamCMD not detected! Retrieving...");

            string archive = Path.Combine(steamcmd_dir.FullName, "steamcmd.zip");
            if (!File.Exists(archive))
            {
                Console.WriteLine("SteamCMD zip archive not detected, fetching..");
                DownloadFile(steamcmd_url, archive);
            }

            WriteLine("Extracting archive..");
            ZipFile.ExtractToDirectory(archive, steamcmd_dir.FullName);

        }

        public static void UpdateSteamCMD()
        {
            WriteLine("Ensuring we're up-to-date and got all the required files");

            if (IsInitialized())
            {
                IsUpdated = true;
                WriteLine("Skipping updates because we have some files");
            }

            string steamcmd = Path.Combine(steamcmd_dir.FullName, "steamcmd.exe");
            Process p = new Process();
            p.StartInfo.FileName = steamcmd;
            p.StartInfo.Arguments = "+quit";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.OutputDataReceived += P_OutputDataReceived;
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();

            IsUpdated = true;
            WriteLine("Updated!");
        }

        public static void DownloadItem(DownloadRequest request )
        {
            DownloadItem(request.Id, request.GameId);
        }

        public static void DownloadItem(string id, string gameid)
        {
            WriteLine("Downloading {0} {1}", id, gameid);

            if (!IsUpdated) return;

            downloads.Enqueue(new DownloadRequest()
            {
                GameId = gameid,
                Id = id
            });

            string steamcmd = Path.Combine(steamcmd_dir.FullName, "steamcmd.exe");
            Process p = new Process();
            p.StartInfo.FileName = steamcmd;
            p.StartInfo.Arguments = string.Format("+login anonymous +workshop_download_item {1} {0} +quit", id, gameid);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.OutputDataReceived += P_OutputDataReceived;
            p.ErrorDataReceived += P_ErrorDataReceived;
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
        }

        private static void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                WriteLine("Internal SteamCmd error: {0}", e.Data);
            }
        }

        private static void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                if (e.Data.StartsWith("Success"))
                {
                    Match m = quote_pattern.Match(e.Data);
                    if (m.Success)
                    {
                        DownloadRequest request = downloads.Dequeue();
                        request.RawData = e.Data;
                        request.Path = m.Groups[1].Value;
                        FileDownloaded?.Invoke(null, request);
                    }

                }
                else if (e.Data.StartsWith("ERROR"))
                {
                    DownloadRequest request = downloads.Dequeue();
                    request.RawData = e.Data;
                    ErrorDownloading?.Invoke(null, request);
                }
                WriteLine(e.Data);
            }
        }

        private static void DownloadFile(string url, string filename)
        {
            WriteLine("Downloading: {0}, to {1}", url, filename);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            HttpWebResponse rsp = (HttpWebResponse)req.GetResponse();
            if (rsp.StatusCode == HttpStatusCode.OK)
            {
                using (FileStream fs = File.Create(filename))
                using (Stream str = rsp.GetResponseStream())
                {

                    byte[] buffer = new byte[4096];
                    int r = 0;
                    while ((r = str.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, r);
                    }

                    fs.Flush();
                }
            }
            rsp.Close();

        }

        public static DownloadRequest FetchInformation(string url)
        {
            HttpWebRequest req = (HttpWebRequest) WebRequest.Create(url);
            HttpWebResponse resp= (HttpWebResponse)req.GetResponse();
            if(resp.StatusCode == HttpStatusCode.OK )
            {
                string html = "";
                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                {
                   html= sr.ReadToEnd();
                }
                DownloadRequest dreq = new DownloadRequest();
                dreq.GameId = Regex.Match(html, "app\\/(\\d+)").Groups[1].Value;
                dreq.Id = Regex.Match(url, "\\?id=(\\d+)").Groups[1].Value;
                return dreq;
            }
            return null;
        }

        private static void WriteLine(string line, params string[] pars)
        {
            WriteLine(string.Format(line, pars));
        }

        private static void WriteLine(string line)
        {
            if (logger != null)
            {
                logger.Log(line);
            }
        }
    }
}
