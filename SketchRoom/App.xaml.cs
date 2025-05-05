using SketchRoom.Toolkit.Wpf.Controls;
using SketchRoom.Toolkit.Wpf.Services;
using System.Configuration;
using System.Data;
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
            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }
    }

}
