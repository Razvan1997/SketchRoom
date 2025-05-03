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

            var path = DestinationPath;

            // Adaugă extensia dacă lipsește
            var ext = SelectedFormatType.ToLower();
            if (!Path.GetExtension(path).Equals($".{ext}", StringComparison.OrdinalIgnoreCase))
            {
                path = Path.ChangeExtension(path, ext);
            }

            var whiteboard = ContainerLocator.Container.Resolve<IWhiteBoardTabService>()
                                  .GetWhiteBoard(ContainerLocator.Container.Resolve<IWhiteBoardTabService>().CurrentTab!.Id)
                                  as WhiteBoardControl;

            if (whiteboard != null)
            {
                whiteboard.SaveToFile(path, SelectedFormatType);
                MessageBox.Show("Sketch saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }

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
            if (string.IsNullOrWhiteSpace(SelectedFormatType))
            {
                MessageBox.Show("Please select a format first.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Choose where to save your sketch",
                Filter = $"{SelectedFormatType} file|*.{SelectedFormatType.ToLower()}|All files|*.*",
                DefaultExt = SelectedFormatType.ToLower(),
                FileName = "sketch"
            };

            if (dialog.ShowDialog() == true)
            {
                DestinationPath = dialog.FileName;
            }
        }
    }
}
