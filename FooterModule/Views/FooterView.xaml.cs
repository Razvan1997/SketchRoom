using FooterModule.ViewModels;
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
using WhiteBoard.Core.Models;

namespace FooterModule.Views
{
    /// <summary>
    /// Interaction logic for FooterView.xaml
    /// </summary>
    public partial class FooterView : UserControl
    {
        private Point _dragStartPoint;
        public FooterView()
        {
            InitializeComponent();
        }

        private void Tab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is FooterTabModel tab &&
        DataContext is FooterViewModel vm)
            {
                vm.SelectTab(tab);
            }
        }

        private void Tab_RightClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void AddTab_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is FooterViewModel vm)
                vm.AddTabCommand.Execute();
        }

        //private void Tab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    _dragStartPoint = e.GetPosition(null);
        //}

        //private void Tab_PreviewMouseMove(object sender, MouseEventArgs e)
        //{
        //    if (e.LeftButton != MouseButtonState.Pressed)
        //        return;

        //    var currentPos = e.GetPosition(null);
        //    if (Math.Abs(currentPos.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
        //        Math.Abs(currentPos.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
        //    {
        //        if (sender is FrameworkElement element && element.DataContext is FooterTabModel tab)
        //        {
        //            DragDrop.DoDragDrop(element, new DataObject("FooterTab", tab), DragDropEffects.Move);
        //        }
        //    }
        //}

        //private void Tab_Drop(object sender, DragEventArgs e)
        //{
        //    if (!e.Data.GetDataPresent("FooterTab"))
        //        return;

        //    var droppedData = e.Data.GetData("FooterTab") as FooterTabModel;
        //    var target = ((FrameworkElement)sender).DataContext as FooterTabModel;

        //    if (droppedData == null || target == null || droppedData == target)
        //        return;

        //    if (DataContext is FooterViewModel vm)
        //    {
        //        vm.ReorderTabs(droppedData, target);
        //    }
        //}
    }
}
