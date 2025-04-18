using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using UserControl = System.Windows.Controls.UserControl;

namespace SketchRoom.Toolkit.Wpf.Controls
{
    /// <summary>
    /// Interaction logic for ColorsPalette.xaml
    /// </summary>
    public partial class ColorsPalette : UserControl
    {
        public ColorsPalette()
        {
            InitializeComponent();
        }

        public Brush SelectedColor
        {
            get => (Brush)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(Brush), typeof(ColorsPalette),
                new PropertyMetadata(Brushes.Black));

        public ICommand SelectColorCommand
        {
            get => (ICommand)GetValue(SelectColorCommandProperty);
            set => SetValue(SelectColorCommandProperty, value);
        }

        public static readonly DependencyProperty SelectColorCommandProperty =
            DependencyProperty.Register(nameof(SelectColorCommand), typeof(ICommand), typeof(ColorsPalette),
                new PropertyMetadata(null));

        private void ColorButton_Click(object sender, MouseButtonEventArgs e)
        {
            var picker = new ColorPickerWindow(SelectedColor)
            {
                Owner = Application.Current.MainWindow
            };

            if (picker.ShowDialog() == true)
            {
                SelectedColor = picker.SelectedColor;
                SelectColorCommand?.Execute(SelectedColor);
            }
        }
    }
}
