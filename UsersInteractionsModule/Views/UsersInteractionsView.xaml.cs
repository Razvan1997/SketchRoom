using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using UsersInteractionsModule.ViewModels;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoardModule.XAML;

namespace UsersInteractionsModule.Views
{
    /// <summary>
    /// Interaction logic for UsersInteractionsView.xaml
    /// </summary>
    public partial class UsersInteractionsView : UserControl
    {
        private readonly IShapeRendererFactory _rendererFactory = new ShapeRendererFactory();
        public UsersInteractionsView()
        {
            InitializeComponent();
            ShapesControl.ShapeDragStarted += shape =>
            {
                if (DataContext is UsersInteractionsViewModel vm)
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
