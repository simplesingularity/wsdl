using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using wsdl;

namespace wsdl_console
{
    internal class Program
    {
        static void Main(string[] args)
        {

            DownloadRequest r = SteamAPI.FetchInformation("https://steamcommunity.com/sharedfiles/filedetails/?id=3359071387");
            Console.WriteLine("Game ID: {0}, Workshop ID: {1}", r.GameId, r.Id);

            AttemptDownload(r);
            Console.WriteLine("Completed all operations!");

            Console.Read();
        }
        static void AttemptDownload(DownloadRequest request)
        {
            SteamAPI.logger = new ConLog();
            SteamAPI.FileDownloaded += SteamAPI_FileDownloaded;
            SteamAPI.ErrorDownloading += SteamAPI_ErrorDownloading;
            SteamAPI.EnsureSteamCMDInstalled();
            SteamAPI.UpdateSteamCMD();
            SteamAPI.DownloadItem(request);
        }

        private static void SteamAPI_ErrorDownloading(object sender, DownloadRequest e)
        {
            Console.WriteLine("Error downloading {0}", e.Id);
        }

        private static void SteamAPI_FileDownloaded(object sender, DownloadRequest e)
        {
            Console.WriteLine("File {1} downloaded to: {0}", e.Path, e.Id);
        }
    }
}
