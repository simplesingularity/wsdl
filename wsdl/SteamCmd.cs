using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/*
 * https://developer.valvesoftware.com/wiki/Command_line_options
 * 
 * This was an attempt at changing the coding style and keeping a process alive
 * but it failed because steamcmd does some funny stuff with the console
 * and we cannot parse output or provide input
 */

namespace wsdl
{
    public class SteamCmd
    {
        public bool IsReady { get; private set; }
        public bool IsInstalled { get; private set; }
        public bool IsArchiveDownloaded { get; private set; }
        public bool IsServiceRunning { get; private set; }
        public ILogger LoggingService { get; set; }
        public Process SteamCmdProcess { get; private set; }
        public Queue<DownloadRequest> Downloads { get; private set; } = new Queue<DownloadRequest>();

        public event EventHandler<DownloadRequest> DownloadComplete;
        public event EventHandler<DownloadRequest> DownloadFailed;

        private DirectoryInfo cur_dir;
        private DirectoryInfo steamcmd_dir;
        private const string steamcmd_url = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        private int okCount;

        public SteamCmd(ILogger logger)
        {
            LoggingService = logger;
            cur_dir = new DirectoryInfo(Assembly.GetEntryAssembly().Location).Parent;
            steamcmd_dir = cur_dir.CreateSubdirectory("steamcmd");
        }

        public void DownloadItem(string gameId, string packageId)
        {
            if(!WaitForReady())
            {
                return;
            }

            Downloads.Enqueue(new DownloadRequest()
            {
                GameId = gameId,
                Id = packageId
            });
            LoggingService.Log("Requesting to download: {0}, {1}", gameId, packageId);
            SteamCmdProcess.StandardInput.WriteLine(string.Format("workshop_download_item {0} {1}\r\n", gameId, packageId));
            SteamCmdProcess.StandardInput.Flush();
            //SteamCmdProcess.BeginOutputReadLine();
        }

        private bool WaitForReady()
        {
            DateTime now = DateTime.Now;
            while (!IsReady)
            {
                if ((DateTime.Now - now).TotalSeconds > 20)
                {
                    return false;
                }
                Thread.Sleep(300);
            }
            return true;
        }

        public void Wait()
        {
            LoggingService.Log("Blocking execution on current thread ");

            if (!IsServiceRunning)
            {
                return;
            }

            SteamCmdProcess.WaitForExit();
            //SteamCmdProcess.WaitForInputIdle();
        }

        public void StopService()
        {
            LoggingService.Log("Stopping SteamCmd service..");
            if (!IsServiceRunning)
            {
                return;
            }
            SteamCmdProcess.Kill();
            IsServiceRunning = false;
        }

        public void StartService()
        {
            LoggingService.Log("Starting SteamCmd service..");

            SetupPaths();

            if (!IsCmdAvailable())
            {
                if (!ArchiveExists())
                {
                    DownloadArchive();
                }
                else
                {
                    ExtractArchive();
                }
            }

            if (IsServiceRunning)
            {
                LoggingService.Log("Already running, killing process..");
                SteamCmdProcess.Kill();
            }

            SteamCmdProcess = new Process();
            SteamCmdProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            SteamCmdProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;

            SteamCmdProcess.StartInfo.FileName = Path.Combine(steamcmd_dir.FullName, "steamcmd.exe");
            //SteamCmdProcess.StartInfo.Arguments = "+login anonymous -validate";
            SteamCmdProcess.StartInfo.UseShellExecute = false;
            SteamCmdProcess.StartInfo.RedirectStandardOutput = true;
            SteamCmdProcess.StartInfo.RedirectStandardInput = true;
            SteamCmdProcess.StartInfo.RedirectStandardError = true;
            SteamCmdProcess.OutputDataReceived += SteamCmdProcess_OutputDataReceived;
            SteamCmdProcess.ErrorDataReceived += SteamCmdProcess_ErrorDataReceived;
            SteamCmdProcess.Start();
            SteamCmdProcess.BeginOutputReadLine();

            IsServiceRunning = true;
        }

        private void SteamCmdProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            LoggingService.Log("Had an oopsie: {0}", e.Data);
        }

        private void SteamCmdProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            LoggingService.Log(e.Data);
            if (e.Data.StartsWith("Success"))
            {
                DownloadRequest request = Downloads.Dequeue();
                request.RawData = e.Data;
                DownloadComplete(this, request);
            }
            else if (e.Data.StartsWith("ERROR"))
            {
                DownloadRequest request = Downloads.Dequeue();
                request.RawData = e.Data;
                DownloadFailed(this, request);
            }
            else if (e.Data.EndsWith("...OK"))
            {
                okCount++;
                if (okCount == 4)
                {
                    IsReady = true;
                }
            }
        }

        private void SetupPaths()
        {
            LoggingService.Log("Setting up steamcmd paths: {0}", steamcmd_dir.FullName);
            if (!steamcmd_dir.Exists)
            {
                steamcmd_dir.Create();
            }

        }

        private bool IsCmdAvailable()
        {
            LoggingService.Log("Checking if steamcmd.exe available");
            string path = Path.Combine(steamcmd_dir.FullName, "steamcmd.exe");
            return File.Exists(path);
        }

        private bool IsInitialized()
        {
            LoggingService.Log("Checking if steamcmd.exe has been initialized");
            string path = Path.Combine(steamcmd_dir.FullName, "steamclient.dll");
            string path64 = Path.Combine(steamcmd_dir.FullName, "steamclient64.dll");
            return File.Exists(path64) && File.Exists(path);
        }

        private bool ArchiveExists()
        {
            LoggingService.Log("Checking if archive exists");
            string path = Path.Combine(steamcmd_dir.FullName, "steamcmd.zip");
            return File.Exists(path);
        }

        private void DownloadArchive()
        {
            LoggingService.Log("Downloading the steamcmd.zip archive");
            string path = Path.Combine(steamcmd_dir.FullName, "steamcmd.zip");
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(steamcmd_url);
            req.Method = "GET";
            HttpWebResponse rsp = (HttpWebResponse)req.GetResponse();
            if (rsp.StatusCode == HttpStatusCode.OK)
            {
                using (FileStream fs = File.Create(path))
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

        private void ExtractArchive()
        {
            LoggingService.Log("Extracting the archive");
            string path = Path.Combine(steamcmd_dir.FullName, "steamcmd.zip");
            if (File.Exists(path))
            {
                ZipFile.ExtractToDirectory(path, steamcmd_dir.FullName);
            }
            else
            {
                LoggingService.Log("Error, cannot extract archive, archive file doesn't exist");
            }

        }

    }
}
