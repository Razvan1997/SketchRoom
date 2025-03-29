using SketchRoom.Models;
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

namespace SketchRoom.Toolkit.Wpf.Controls
{
    /// <summary>
    /// Interaction logic for Participants.xaml
    /// </summary>
    public partial class Participants : UserControl
    {
        public Participants()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ParticipantsSourceProperty =
            DependencyProperty.Register(nameof(ParticipantsSource), typeof(ObservableCollection<Participant>), typeof(Participants), new PropertyMetadata(null));

        public ObservableCollection<Participant> ParticipantsSource
        {
            get => (ObservableCollection<Participant>)GetValue(ParticipantsSourceProperty);
            set => SetValue(ParticipantsSourceProperty, value);
        }

        private void Border_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new QuadraticEase()
                };
                border.BeginAnimation(OpacityProperty, fadeIn);
            }
        }
    }
}
