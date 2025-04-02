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

namespace WalkthroughDemo
{
    /// <summary>
    /// Interaction logic for ArrowedPopup.xaml
    /// </summary>
    public partial class ArrowedPopup : UserControl
    {
        public event Action NextClicked;
        public event Action SkipAllClicked;

        public ArrowedPopup()
        {
            InitializeComponent();
        }

        public void SetArrowRotation(double angle)
        {
            ArrowPolygon.RenderTransform = new RotateTransform(angle, 10, 5);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            NextClicked?.Invoke();
        }

        private void SkipAll_Click(object sender, RoutedEventArgs e)
        {
            SkipAllClicked?.Invoke();
        }
    }
}
