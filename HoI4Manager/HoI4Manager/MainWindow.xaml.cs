using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace HoI4Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void WorkshopID_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.GotFocus -= WorkshopID_GotFocus;
        }

        void Download(object sender, RoutedEventArgs e)
        {
            int ID;
            try
            {
                ID = GetID(WorkshopID.Text);
                addEntry($"ID is {ID}");
            }
            catch
            {
                addEntry("Wrong workshop link");
                return;
            }

            var HoI4Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod");

            var downloadLink = $"http://workshop.abcvg.info/archive/394360/{ID}.zip";
            addEntry($"Downloading from {downloadLink}... to {HoI4Path}");
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += DownloadProgressChanged;
                client.DownloadFileCompleted += DownloadFinished;
                client.DownloadFileAsync(new Uri(downloadLink), HoI4Path + $@"\{ID}.zip");
            }
            
            // continue in DownloadFinished
        }

        private void DownloadFinished(object sender, AsyncCompletedEventArgs e)
        {
            addEntry("Download complete!");
        }

        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        int GetID(string link)
        {
            var pos = link.IndexOf("id");
            return int.Parse(link.Substring(pos + 3));
        }

        void addEntry(string text)
        {
            Debug.Text += "\n";
            Debug.Text += text;
        }
    }
}
