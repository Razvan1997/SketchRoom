using System.Windows;
using System.Windows.Controls;
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
