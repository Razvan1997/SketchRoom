using Microsoft.Win32;
using SketchRoom.Database;
using SketchRoom.Models;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace SketchRoom.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        public string GhostPreviewPath { get; set; } = string.Empty;
        public string Hotkey1 { get; set; } = "TAB";
        public string Hotkey2 { get; set; } = "S";

        public SettingsDialog()
        {
            InitializeComponent();
            Loaded += SettingsDialog_Loaded;
        }

        private void SettingsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = SettingsStorage.Load();

            GhostPreviewPath = string.IsNullOrWhiteSpace(settings.GhostPreviewPath)
                ? GetDefaultGhostPath()
                : settings.GhostPreviewPath;

            Hotkey1 = "CTRL"; // impus, nu din fișier
            Hotkey2 = string.IsNullOrWhiteSpace(settings.Hotkey2)
                ? "S"
                : settings.Hotkey2;

            txtPath.Text = GhostPreviewPath;
            txtHotkey1.Text = Hotkey1; // mereu "CTRL"
            txtHotkey2.Text = Hotkey2;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a file inside the desired folder",
                CheckFileExists = true,
                Filter = "All files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                GhostPreviewPath = Path.GetDirectoryName(dialog.FileName);
                txtPath.Text = GhostPreviewPath;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            GhostPreviewPath = txtPath.Text;
            Hotkey1 = "CTRL"; // forțat, nu citit din textbox
            Hotkey2 = txtHotkey2.Text;

            var settings = new SettingsData
            {
                GhostPreviewPath = GhostPreviewPath,
                Hotkey1 = Hotkey1,
                Hotkey2 = Hotkey2
            };

            SettingsStorage.Save(settings);
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void txtHotkey1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            txtHotkey1.Text = e.Key.ToString().ToUpper();
        }

        private void txtHotkey2_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            txtHotkey2.Text = e.Key.ToString().ToUpper();
        }

        private string GetDefaultGhostPath()
        {
            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "SketchRoomPreviews");

            Directory.CreateDirectory(defaultPath); // Asigură-te că există
            return defaultPath;
        }
    }
}
