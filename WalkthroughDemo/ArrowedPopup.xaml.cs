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
        private CancellationTokenSource _cts;
        public ArrowedPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(DescriptionText.Text))
                {
                    await AnimateText(DescriptionText.Text);
                }
            };
        }

        private async Task AnimateText(string text, TimeSpan? delayPerChar = null)
        {
            delayPerChar ??= TimeSpan.FromMilliseconds(1);
            DescriptionText.Text = "";

            foreach (char c in text)
            {
                DescriptionText.Text += c;
                await Task.Delay(delayPerChar.Value);
            }
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
