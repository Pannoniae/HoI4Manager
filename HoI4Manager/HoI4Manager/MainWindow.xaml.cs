using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
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

        private int ID;

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

            var HoI4Path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Paradox Interactive", "Hearts of Iron IV", "mod");

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

        void ExtractZIP()
        {
            var HoI4Path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod");

        // Extract outer archive

        label1:
            try
            {
                ZipFile.ExtractToDirectory(
                    Path.Combine(HoI4Path, $"{ID}.zip"),
                    Path.Combine(HoI4Path, $"_{ID}_tmp"));
            }
            catch (IOException)
            {
                addEntry("File already exists, please remove file");
                addEntry("Experimental feature: we are trying to clean up. If that fails, please remove manually");
                Directory.Delete(Path.Combine(HoI4Path, $"_{ID}_tmp"), true); // recursive delete
                goto label1; // I know GOTO is bad, but this is a legit solution here
            }

            var innerArchiveName = Directory.GetFiles(Path.Combine(HoI4Path, $"_{ID}_tmp", $"ID"))[0];
            addEntry($"Inner archive is {innerArchiveName}");

            // Extract inner archive

            try
            {
                ZipFile.ExtractToDirectory(
                    Path.Combine(HoI4Path, $"_{ID}_tmp", $"ID", innerArchiveName),
                    Path.Combine(HoI4Path, $"{ID}"));
            }
            catch (IOException)
            {
                addEntry("Something happened while extracting the mod. Please check manually");
            }

            // copying descriptor.mod
            File.Copy(Path.Combine(HoI4Path, $"{ID}", "descriptor.mod"), Path.Combine(HoI4Path, $"{ID}._mod"));

            // modifying mod file
            string[] descriptorMod = File.ReadAllLines(Path.Combine(HoI4Path, $"_{ID}._mod"));

            int i = 0;
            foreach (string line in descriptorMod)
            {
                if (line.StartsWith("archive"))
                {
                    // Bad coding practice, but I've come from Python, and C# does not allow me to redefine my variables... sorry
                    var _line = line.Replace("archive=", "path=");
                    var __line = _line.Replace(".zip", "");
                    var pos = __line.IndexOf("=");
                    var ___line = __line.Substring(0, pos + 1) + "mod/" + __line.Substring(pos + 1);

                    descriptorMod[i] = ___line;
                }
                i++;
            }

            // writing final .mod file
            File.WriteAllLines(Path.Combine(HoI4Path, $"_{ID}.mod"), descriptorMod);
        }

        private void DownloadFinished(object sender, AsyncCompletedEventArgs e)
        {
            addEntry("Download complete!");
            ExtractZIP();
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
            if (Debug.Text.Length > 400)
            {
                Debug.Text = "";
            }
            Debug.Text += "\n";
            Debug.Text += text;
        }
    }
}
