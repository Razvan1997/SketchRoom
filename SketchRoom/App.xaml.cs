using SketchRoom.Database;
using SketchRoom.Toolkit.Wpf.Controls;
using SketchRoom.Toolkit.Wpf.Services;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace SketchRoom
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = SettingsStorage.Load();

            if (string.IsNullOrWhiteSpace(settings.GhostPreviewPath))
            {
                settings.GhostPreviewPath = GetDefaultGhostPreviewPath();
                settings.Hotkey1 = "TAB";
                settings.Hotkey2 = "S";

                SettingsStorage.Save(settings);
            }

            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }

        private string GetDefaultGhostPreviewPath()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "SketchRoomPreviews");

            Directory.CreateDirectory(path);
            return path;
        }
    }

}
