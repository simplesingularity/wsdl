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
        static Regex quote_pattern = new Regex("\\\"(.*?)\\\"");
        static void Main(string[] args)
        {
            SteamAPI.logger = new ConLog();
            SteamAPI.FileDownloaded += SteamAPI_FileDownloaded;
            SteamAPI.EnsureSteamCMDInstalled();
            SteamAPI.UpdateSteamCMD();
            SteamAPI.DownloadItem("2853616849", "4000");
            Console.Read();
        }

        private static void SteamAPI_FileDownloaded(object sender, string e)
        {
            Match m = quote_pattern.Match(e);
            if(m.Success)
            {
                string filename = m.Groups[1].Value;
                Console.WriteLine("Downloaded file: {0}", filename);
            }
        }
    }
}
