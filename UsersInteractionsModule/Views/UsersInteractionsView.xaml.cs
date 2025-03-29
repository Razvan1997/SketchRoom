using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using UsersInteractionsModule.ViewModels;

namespace UsersInteractionsModule.Views
{
    /// <summary>
    /// Interaction logic for UsersInteractionsView.xaml
    /// </summary>
    public partial class UsersInteractionsView : UserControl
    {
        public UsersInteractionsView()
        {
            InitializeComponent();
            if (DataContext is UsersInteractionsViewModel vm)
            {
                //vm.MenuBehavior = MenuBehavior;
            }
        }
    }
}
