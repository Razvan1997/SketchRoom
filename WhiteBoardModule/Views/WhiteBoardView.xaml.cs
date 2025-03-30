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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WhiteBoardModule.ViewModels;

namespace WhiteBoardModule.Views
{
    /// <summary>
    /// Interaction logic for WhiteBoardView.xaml
    /// </summary>
    public partial class WhiteBoardView : UserControl
    {
        public WhiteBoardView()
        {
            InitializeComponent();

            this.Loaded += WhiteBoardView_Loaded;
        }

        private void WhiteBoardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not WhiteBoardViewModel vm)
                return;

            vm.SetControlAdapter(Whiteboard);

            Whiteboard.LineDrawn += vm.OnLineDrawn;
            Whiteboard.MouseMoved += vm.OnMouseMoved;
            Whiteboard.LivePointDrawn += vm.OnDrawPointLive;
        }
    }
}
