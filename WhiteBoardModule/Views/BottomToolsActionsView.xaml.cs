using SketchRoom.Models.Enums;
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
        private WhiteBoardTool _previousTool = WhiteBoardTool.None;
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

        private void FreeDrawButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is BottomToolsActionsViewModel vm)
            {
                if (_previousTool != WhiteBoardTool.FreeDraw && vm.SelectedTool == WhiteBoardTool.FreeDraw)
                {
                    ThicknessPopup.IsOpen = true;
                }

                _previousTool = vm.SelectedTool;
            }
        }

        private void Dot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse dot && dot.Tag is string tagStr && int.TryParse(tagStr, out int thickness))
            {
                // Setează grosimea în ViewModel sau serviciu
                var viewModel = (BottomToolsActionsViewModel)DataContext;
                viewModel.SetFreeDrawThickness(thickness);
                ThicknessPopup.IsOpen = false;
            }
        }
        private void RemoveStrokeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is BottomToolsActionsViewModel vm)
            {
                if (_previousTool != WhiteBoardTool.RemoveStroke && vm.SelectedTool == WhiteBoardTool.RemoveStroke)
                {
                    EraserPopup.IsOpen = true;
                }

                _previousTool = vm.SelectedTool;
            }
        }

        private void EraserDot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse dot && dot.Tag is string tagStr && int.TryParse(tagStr, out int radius))
            {
                var vm = (BottomToolsActionsViewModel)DataContext;
                vm.SetEraseRadius(radius);
                EraserPopup.IsOpen = false;
            }
        }
    }
}
