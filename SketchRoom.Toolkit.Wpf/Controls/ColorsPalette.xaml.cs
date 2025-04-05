using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
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
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ApplySelectionEffects();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        //private void ColorsPalette_Loaded(object sender, RoutedEventArgs e)
        //{
        //    ApplySelectionEffects();
        //}

        public ObservableCollection<Brush> AvailableColors
        {
            get => (ObservableCollection<Brush>)GetValue(AvailableColorsProperty);
            set => SetValue(AvailableColorsProperty, value);
        }

        public static readonly DependencyProperty AvailableColorsProperty =
            DependencyProperty.Register(nameof(AvailableColors), typeof(ObservableCollection<Brush>), typeof(ColorsPalette), new PropertyMetadata(null));

        public Brush SelectedColor
        {
            get => (Brush)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(Brush), typeof(ColorsPalette), new PropertyMetadata(null, OnSelectedColorChanged));

        public ICommand SelectColorCommand
        {
            get => (ICommand)GetValue(SelectColorCommandProperty);
            set => SetValue(SelectColorCommandProperty, value);
        }

        public static readonly DependencyProperty SelectColorCommandProperty =
            DependencyProperty.Register(nameof(SelectColorCommand), typeof(ICommand), typeof(ColorsPalette), new PropertyMetadata(null));

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorsPalette palette)
            {
                palette.ApplySelectionEffects();
            }
        }

        private void ApplySelectionEffects()
        {
            foreach (var child in FindVisualChildren<Ellipse>(this))
            {
                if (child.Tag is Brush brush)
                {
                    var scale = (ScaleTransform)child.RenderTransform;

                    bool isSelected = brush.Equals(SelectedColor);

                    AnimateScale(scale, isSelected ? 1.5 : 1.0);
                    child.Stroke = isSelected ? Brushes.Black : Brushes.Gray;
                    child.StrokeThickness = isSelected ? 1 : 1;

                    // 👇 curățăm handler-ele anterioare (ca să nu le adunăm la infinit)
                    child.MouseEnter -= Ellipse_MouseEnter;
                    child.MouseLeave -= Ellipse_MouseLeave;

                    // 👇 adăugăm noi handler-e cu logica
                    child.MouseEnter += Ellipse_MouseEnter;
                    child.MouseLeave += Ellipse_MouseLeave;
                }
            }
        }

        private void AnimateScale(ScaleTransform scale, double target, Ellipse ellipse = null)
        {
            if (scale.IsFrozen || scale == null)
            {
                scale = new ScaleTransform(1.0, 1.0);
                if (ellipse != null)
                    ellipse.RenderTransform = scale;
            }

            var duration = new Duration(TimeSpan.FromMilliseconds(100));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(target, duration));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(target, duration));
        }

        private void Ellipse_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                var scale = ellipse.RenderTransform as ScaleTransform;
                bool isSelected = ellipse.Tag is Brush brush && brush.Equals(SelectedColor);
                AnimateScale(scale, isSelected ? 1.6 : 1.2, ellipse);
            }
        }

        private void Ellipse_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                var scale = ellipse.RenderTransform as ScaleTransform;
                bool isSelected = ellipse.Tag is Brush brush && brush.Equals(SelectedColor);
                AnimateScale(scale, isSelected ? 1.5 : 1.0, ellipse);
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
