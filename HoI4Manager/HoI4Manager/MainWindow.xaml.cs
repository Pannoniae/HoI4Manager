using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Ionic.Zip;
using ZipFile = Ionic.Zip.ZipFile;

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

            System.IO.Compression.ZipFile.ExtractToDirectory(
                Path.Combine(HoI4Path, $"{ID}.zip"),
                Path.Combine(HoI4Path, $"_{ID}_tmp"));

            var innerArchiveName = Directory.GetFiles(Path.Combine(HoI4Path, $"_{ID}_tmp", $"{ID}"))[0];
            addEntry($"Inner archive is {innerArchiveName}");

            // Extract inner archive

            try
            {
                //ZipFile.ExtractToDirectory(
                //    innerArchiveName,
                //    Path.Combine(HoI4Path, $"{ID}"));
                ZipFile zip = ZipFile.Read(innerArchiveName);
                Directory.CreateDirectory(Path.Combine(HoI4Path, $"{ID}"));
                zip.ExtractAll(Path.Combine(HoI4Path, $"{ID}"), ExtractExistingFileAction.OverwriteSilently);
                zip.Dispose();
            }
            catch
            {
                addEntry("Something happened while extracting the mod. Please check manually");
                throw;
            }

            // copying descriptor.mod
            File.Copy(Path.Combine(HoI4Path, $"{ID}", "descriptor.mod"), Path.Combine(HoI4Path, $"{ID}._mod"));

            // modifying mod file
            string[] descriptorMod = File.ReadAllLines(Path.Combine(HoI4Path, $"{ID}._mod"));

            int i = 0;
            foreach (string line in descriptorMod)
            {
                if (line.StartsWith("archive"))
                {
                    // Bad coding practice, but I've come from Python, and C# does not allow me to redefine my variables... sorry
                    var _line = line.Replace("archive=", "path=");
                    var __line = _line.Replace(".zip", "");
                    var pos = __line.IndexOf("=");
                    var ___line = __line.Substring(0, pos + 1) + "\"mod/" + $"{ID}\"";

                    descriptorMod[i] = ___line;
                }
                i++;
            }

            // writing final .mod file
            File.WriteAllLines(Path.Combine(HoI4Path, $"{ID}.mod"), descriptorMod);

            // cleaning leftup
            foreach (var file in Directory.GetFiles(HoI4Path))
            {
                if (Path.GetFileName(file).StartsWith("_") || Path.GetFileName(file).Contains("_mod") || Path.GetFileName(file).EndsWith(".zip"))
                {
                    addEntry($"Deleted junk file {Path.GetFileName(file)}");
                    File.Delete(file);
                }
            }

            foreach (var dir in Directory.GetDirectories(HoI4Path))
            {
                if (Path.GetFileName(dir).StartsWith("_")) // yes, it works this way... a directory is just a special file
                {
                    addEntry($"Deleted junk dir {Path.GetFileName(dir)}");
                    Directory.Delete(dir, true); // recursive
                }
            }
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
            if (Debug.Text.Length > 500)
            {
                Debug.Text = "";
            }
            Debug.Text += "\n";
            Debug.Text += text;
        }
    }
}
