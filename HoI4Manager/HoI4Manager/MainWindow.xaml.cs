using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
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
        string HoI4Path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod");

        public MainWindow()
        {
            InitializeComponent();

            Title = "HoI4Manager";

            addEntry("\n\n");
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
                addEntry($"ID is {ID}\n");
            }
            catch
            {
                addEntry("Wrong workshop link!\n");
                return;
            }


            var downloadLink = $"http://workshop.abcvg.info/archive/394360/{ID}.zip";
            addEntry($"Downloading from {downloadLink} to {HoI4Path}...\n");
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
            addEntry("Download complete, extracting file...\n", Colors.Orange);
            ExtractZIP();
        }

        void ExtractZIP()
        {

            // ExtractZIPAsync
            BackgroundWorker bw = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            bw.DoWork += BackgroundWorkerJob;
            bw.RunWorkerCompleted += BackgroundWorkerCompleted;

            bw.RunWorkerAsync();


            
        }

        void BackgroundWorkerJob(object sender, DoWorkEventArgs e)
        {
            ExtractZIPAsync();
        }

        void ExtractZIPAsync()
        {
            // Extract outer archive

            System.IO.Compression.ZipFile.ExtractToDirectory(
                Path.Combine(HoI4Path, $"{ID}.zip"),
                Path.Combine(HoI4Path, $"_{ID}_tmp"));

            var innerArchiveName = Directory.GetFiles(Path.Combine(HoI4Path, $"_{ID}_tmp", $"{ID}"))[0];

            // Extract inner archive

            ZipFile zip = ZipFile.Read(innerArchiveName);
            zip.ExtractProgress += ZIPProgressBarUpdater;
            Directory.CreateDirectory(Path.Combine(HoI4Path, $"{ID}"));
            zip.ExtractAll(Path.Combine(HoI4Path, $"{ID}"), ExtractExistingFileAction.OverwriteSilently);
            zip.Dispose();
        }

        private void ZIPProgressBarUpdater(object sender, ExtractProgressEventArgs e)
        {
            double totalPercentage;
            if (e.EventType == ZipProgressEventType.Extracting_AfterExtractEntry)
            {
                totalPercentage = (double)e.EntriesExtracted / (double)e.EntriesTotal * 100d;
                Dispatcher.Invoke(() => (ProgressBar.Value = totalPercentage));

            }
            if (e.EventType == ZipProgressEventType.Extracting_AfterExtractAll)
            {
                Dispatcher.Invoke(() => addEntry("Extraction complete.\n", Colors.Orange));
            }
        }

        void BackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ContinueExtractingZIP();
        }

        void ContinueExtractingZIP()
        {
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

            addEntry("Completed downloading mod.\n", Colors.Red);

            ActuallyClean();
        }

        

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        int GetID(string link)
        {
            int pos = link.IndexOf("id");
            if (pos == -1)
            {
                return int.Parse(link);
            }
            return int.Parse(link.Substring(pos + 3));
        }

        void addEntry(string text)
        {
            ScrollBar.ScrollToBottom();
            Debug.Inlines.Add(text);
        }

        void addEntry(string text, Color color)
        {
            ScrollBar.ScrollToBottom();
            var run = new Run();
            run.Text = text;
            run.Foreground = new SolidColorBrush(color);

            Debug.Inlines.Add(run);
        }

        private void Clean(object sender, RoutedEventArgs e)
        {
            ActuallyClean();
        }

        void ActuallyClean()
        {
            // cleaning leftup
            foreach (var file in Directory.GetFiles(HoI4Path))
            {
                if (Path.GetFileName(file).StartsWith("_") || Path.GetFileName(file).Contains("_mod") || Path.GetFileName(file).EndsWith(".zip"))
                {
                    addEntry($"Deleted junk file {Path.GetFileName(file)}\n");
                    File.Delete(file);
                }
            }

            foreach (var dir in Directory.GetDirectories(HoI4Path))
            {
                if (Path.GetFileName(dir).StartsWith("_")) // yes, it works this way... a directory is just a special file
                {
                    addEntry($"Deleted junk directory {Path.GetFileName(dir)}\n");
                    Directory.Delete(dir, true); // recursive
                }
            }
            addEntry("Cleared junk.\n\n");
        }

        private void CleanAllButton_Click(object sender, RoutedEventArgs e)
        {
            CleanAll();
        }

        void CleanAll()
        {
            // cleaning leftup
            foreach (var file in Directory.GetFiles(HoI4Path))
            {
                addEntry($"Deleted file {Path.GetFileName(file)}\n");
                File.Delete(file);
            }

            foreach (var dir in Directory.GetDirectories(HoI4Path))
            {
                // yes, it works this way... a directory is just a special file
                addEntry($"Deleted directory {Path.GetFileName(dir)}\n");
                Directory.Delete(dir, true); // recursive
            }

            addEntry("Wiped everything.\n");
        }
    }
}