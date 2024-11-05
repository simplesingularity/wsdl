using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using wsdl;

namespace wsdl_gui
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        private void SteamAPI_ErrorDownloading(object sender, DownloadRequest e)
        {
            SteamAPI.logger.Log("Failed to download {0}", e.Id);
        }

        private void SteamAPI_FileDownloaded(object sender, DownloadRequest e)
        {
            SteamAPI.logger.Log("Successfully downloaded {0}: {1}", e.Id,e.Path);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            SteamAPI.DownloadItem(SteamAPI.FetchInformation(textBox1.Text));
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            SteamAPI.logger = new textboxlogger(textBox2);
            SteamAPI.FileDownloaded += SteamAPI_FileDownloaded; 
            SteamAPI.ErrorDownloading += SteamAPI_ErrorDownloading; 
            SteamAPI.EnsureSteamCMDInstalled();
            SteamAPI.UpdateSteamCMD();
        }
    }
}
