using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace NodeMcu_Updataer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public float percent;
        public long totalDownloadedByte;
        public int PGB_Maximum;
        public string LblStr = "Loading Information from Server......";
        public bool FnsUpd = false;
        private DispatcherTimer timer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();

           percent = 0;

           string URL = "http://nodemcu-studio-2015.coding.io/pre_build/NodeMCU%20Studio%202015.exe";
           Binding bind = new Binding();
           timer.Interval = TimeSpan.FromMilliseconds(100);
           timer.Tick += new EventHandler(ui_udp);
           timer.Start();
            
            
             new Task(() =>
            {
            try
            {
                System.Net.HttpWebRequest Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URL);
                System.Net.HttpWebResponse myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();
                long totalBytes = myrp.ContentLength;
                PGB_Maximum = (int)totalBytes;
                
                System.IO.Stream st = myrp.GetResponseStream();
                System.IO.Stream so = new System.IO.FileStream(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "/UpdBuf.exe_", System.IO.FileMode.Create);
                totalDownloadedByte = 0;
                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, (int)by.Length);
                while (osize > 0)
                {
                    totalDownloadedByte = osize + totalDownloadedByte;                   
                    so.Write(by, 0, osize);                 
                   
                    osize = st.Read(by, 0, (int)by.Length);
                    LblStr = "Downloading...       " + ((int)totalDownloadedByte / 1000).ToString() + "KB/" + ((int)PGB_Maximum / 1000).ToString() + "KB";
                    percent = (float)totalDownloadedByte / (float)totalBytes * 100;                 
                }
                so.Close();
                st.Close();
            }
            catch (System.Exception)
            {
                throw;
            }
            LblStr = "Installing Update......";
            try
            {
                string req = "taskkill /T /F /IM \"NodeMCU Studio 2015.exe\"";
                Process UpdPcs = new Process();
                UpdPcs.StartInfo.FileName = "cmd.exe";
                UpdPcs.StartInfo.UseShellExecute = false;
                UpdPcs.StartInfo.CreateNoWindow = true;
                UpdPcs.StartInfo.RedirectStandardInput = true;
                UpdPcs.StartInfo.RedirectStandardOutput = true;
                UpdPcs.Start();
                UpdPcs.StandardInput.WriteLine(req);
                
                string FlLoc = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                File.Delete(@FlLoc + "/NodeMCU Studio 2015.exe");
                UpdPcs.StandardInput.WriteLine("rename \"" + FlLoc + "/UpdBuf.exe_\"  \"NodeMCU Studio 2015.exe\"");
                UpdPcs.StandardInput.WriteLine("\"NodeMCU Studio 2015.exe\"");
                FnsUpd = true;
            }
            catch { }

            }).Start();
        }

        public void ui_udp(object sender, EventArgs e)
        {
            PGBar.Maximum = 100;
            PGBar.Value = (int)percent;

            LblStatus.Content = LblStr;

            if (FnsUpd)
            {
                System.Environment.Exit(0);
            }
            
        }

    }
}
