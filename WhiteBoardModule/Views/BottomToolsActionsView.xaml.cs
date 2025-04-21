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
    /// Interaction logic for BottomToolsActionsView.xaml
    /// </summary>
    public partial class BottomToolsActionsView : UserControl
    {
        public BottomToolsActionsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BottomToolsActionsViewModel vm)
            {
                vm.InitializeAfterLoad();
            }
        }
    }
}
