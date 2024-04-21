using Octokit;
using System.IO;
using System.Windows;
using Application = System.Windows.Forms.Application;

namespace TerhaPatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string OldFilesDirectory { get; set; }

        public string NewFilesDirectory { get; set; }

        public string OutputPatchDirectory { get; set; }

        public PatchCreator patchCreator;

        public ILogger logger;

        private GithubApi githubApi;

        public MainWindow()
        {
            InitializeComponent();
            this.logger = new FileLogger("patcher_log.txt");
            this.patchCreator = new PatchCreator(logger);
            this.githubApi = new GithubApi(logger);
            UploadPatchButton.IsEnabled = false;
        }

        private void OldFilesDirButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string folder = dialog.SelectedPath;
                OldFilesDirectory = folder;
                // get list of files of the selected folder and display them in the listbox
                string[] files = Directory.GetFiles(folder);
                //get only the file name and add it to the listbox items
                foreach (string file in files)
                {
                    OldFilesList.Items.Add(Path.GetFileName(file));
                }

            }
        }

        private void NewFilesDirButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string folder = dialog.SelectedPath;
                NewFilesDirectory = folder;
                // get list of files of the selected folder and display them in the listbox
                string[] files = Directory.GetFiles(folder);
                //get only the file name and add it to the listbox items
                foreach (string file in files)
                {
                    NewFilesList.Items.Add(Path.GetFileName(file));
                }

            }
        }

        private async void CreatePatchButton_Click(object sender, RoutedEventArgs e)
        {
            CreatePatchButton.IsEnabled = false;
            var OutputFileZip = OutputPatchDirectory + "\\" + VersionText.Text + ".zip";
            patchCreator.CreatePatch(OldFilesDirectory, NewFilesDirectory, OutputFileZip, ProgressBarPatcher);
            CreatePatchButton.IsEnabled = true;
            UploadPatchButton.IsEnabled = true;
        }

        private void OutputPatchDirButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string folder = dialog.SelectedPath;
                OutputPatchDirectory = folder;
                patchDirLabel.Content = patchDirLabel.Content + " " + folder;
            }
        }

        private async void VersionText_Loaded(object sender, RoutedEventArgs e)
        {
            var release = await githubApi.GetLatestRelease();
            VersionText.Text = release;
        }

        private async void UploadPatchButton_Click(object sender, RoutedEventArgs e)
        {
            var OutputFileZip = OutputPatchDirectory + "\\" + VersionText.Text + ".zip";
            string[] mods = new string[NewFilesList.Items.Count];
            for (int i = 0; i < NewFilesList.Items.Count; i++)
            {
                mods[i] = NewFilesList.Items[i].ToString();
            }
            if (File.Exists(OutputFileZip))
            {
                await githubApi.CreateReleaseUploadAsset(VersionText.Text, OutputFileZip, mods);
            }
            else
            {
                System.Windows.MessageBox.Show("Patch file not found");
            }
                
        }
    }

}