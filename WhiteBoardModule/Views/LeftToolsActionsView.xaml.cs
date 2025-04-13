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
using WhiteBoardModule.XAML;

namespace WhiteBoardModule.Views
{
    /// <summary>
    /// Interaction logic for LeftToolsActionsView.xaml
    /// </summary>
    public partial class LeftToolsActionsView : UserControl
    {
        private readonly IShapeRendererFactory _rendererFactory = new ShapeRendererFactory();
        public LeftToolsActionsView()
        {
            InitializeComponent();

            ShapesControl.ShapeDragStarted += shape =>
            {
                if (DataContext is LeftToolsActionsViewModel vm)
                    vm.OnShapeDragStarted(shape);
            };

            ShapesControl.PreviewFactory = type =>
            {
                var renderer = _rendererFactory.CreateRenderer(type, withBindings: false);
                return renderer.CreatePreview();
            };
        }
    }
}
