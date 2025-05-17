using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using SketchRoom.Toolkit.Wpf.Controls;
using WhiteBoard.Core.Services.Interfaces;
using System.IO;
using Microsoft.Win32;
using SketchRoom.Database;

namespace SketchRoom.ViewModels
{
    public class SaveSketchDialogViewModel : BindableBase
    {
        public SaveSketchDialogViewModel()
        {
            // Formate disponibile pentru ComboBox
            FormatTypes = new ObservableCollection<string>
            {
                "PNG",
                "JPEG",
                "BMP",
                "TIFF",
                "PDF"
            };

            var settings = SettingsStorage.Load();
            DestinationPath = string.IsNullOrWhiteSpace(settings.GhostPreviewPath)
                ? ""
                : settings.GhostPreviewPath;

            // Comenzi
            SaveCommand = new DelegateCommand(OnSave);
            CancelCommand = new DelegateCommand(OnCancel);
            BrowsePathCommand = new DelegateCommand(OnBrowsePath);
        }

        // ComboBox ItemsSource
        public ObservableCollection<string> FormatTypes { get; }

        private string _selectedFormatType;
        public string SelectedFormatType
        {
            get => _selectedFormatType;
            set => SetProperty(ref _selectedFormatType, value);
        }

        private string _destinationPath;
        public string DestinationPath
        {
            get => _destinationPath;
            set => SetProperty(ref _destinationPath, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowsePathCommand { get; }

        private void OnSave()
        {
            if (string.IsNullOrWhiteSpace(DestinationPath) || string.IsNullOrWhiteSpace(SelectedFormatType))
            {
                MessageBox.Show("Please choose a valid path and format.");
                return;
            }

            var ext = SelectedFormatType.ToLower();
            var now = DateTime.Now;
            var folderName = $"sketch_{now:yyyyMMdd_HHmmss}";

            // ✅ Folosește direct folderul selectat de utilizator
            var exportFolder = Path.Combine(DestinationPath, folderName);
            Directory.CreateDirectory(exportFolder);

            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var settings = SettingsStorage.Load();

            foreach (var tab in tabService.AllTabs)
            {
                var whiteboard = tabService.GetWhiteBoard(tab.Id) as WhiteBoardControl;
                if (whiteboard == null)
                    continue;

                var fileName = $"{tab.Name}.{ext}";
                var fullPath = Path.Combine(exportFolder, fileName);

                whiteboard.SaveToFile(fullPath, SelectedFormatType);

                // ✅ Ghost preview (creează folderul dacă lipsește)
                if (!string.IsNullOrWhiteSpace(settings.GhostPreviewPath))
                {
                    Directory.CreateDirectory(settings.GhostPreviewPath); // prevenim crash
                    var ghostName = $"ghost_{tab.Name}_{now:yyyyMMdd_HHmmss}.{ext}";
                    var ghostPath = Path.Combine(settings.GhostPreviewPath, ghostName);
                    whiteboard.SaveToFile(ghostPath, SelectedFormatType);
                }
            }

            MessageBox.Show("All tabs saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseWindow();
        }

        private void OnCancel()
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            // Închide fereastra curentă (dacă este folosită ca dialog)
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }

        private void OnBrowsePath()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
            {
                Description = "Select folder to save sketches",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                DestinationPath = dialog.SelectedPath;
            }
        }
    }
}
