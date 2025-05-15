using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SketchRoom.Windows
{
    /// <summary>
    /// Interaction logic for OverlayLoadingWindow.xaml
    /// </summary>
    public partial class OverlayLoadingWindow : Window
    {
        public OverlayLoadingWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Forțează animațiile să ruleze
            this.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        }
    }
}
