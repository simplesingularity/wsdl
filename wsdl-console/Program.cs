using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using wsdl;

namespace wsdl_console
{
    internal class Program
    {
      
        static void Main(string[] args)
        {
            SteamAPI.logger = new ConLog();
            SteamAPI.FileDownloaded += SteamAPI_FileDownloaded;
            SteamAPI.ErrorDownloading += SteamAPI_ErrorDownloading;
            SteamAPI.EnsureSteamCMDInstalled();
            SteamAPI.UpdateSteamCMD();
            SteamAPI.DownloadItem("260413571", "4000");
            Console.Read();
        }

        private static void SteamAPI_ErrorDownloading(object sender, string e)
        {
            Console.WriteLine("Error downloading");
        }

        private static void SteamAPI_FileDownloaded(object sender, string e)
        {
            Console.WriteLine("File downloaded to: {0}");
        }
    }
}
