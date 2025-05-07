using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Media;
using WhiteBoard.Core.Helpers;
using SketchRoom.Models.Enums;

namespace WhiteBoard.Core.Models
{
    public class BpmnWhiteBoardElementXaml : WhiteBoardElement, ISerializableShape
    {
        private readonly IInteractiveShape _shape;

        public event MouseButtonEventHandler? Clicked;

        public BpmnWhiteBoardElementXaml(IInteractiveShape shape)
        {
            _shape = shape ?? throw new ArgumentNullException(nameof(shape));

            _shape.ShapeClicked += (s, e) =>
            {
                Clicked?.Invoke(s, e);
            };
        }

        public override UIElement Visual => _shape.Visual;

        public override Rect Bounds
        {
            get
            {
                if (_shape.Visual is FrameworkElement fe &&
                    VisualTreeHelper.GetParent(fe) is Visual parent)
                {
                    GeneralTransform transform = fe.TransformToAncestor(parent);
                    return transform.TransformBounds(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight));
                }
                return Rect.Empty;
            }
        }

        public override void SetPosition(Point position)
        {
            Canvas.SetLeft(_shape.Visual, position.X);
            Canvas.SetTop(_shape.Visual, position.Y);
        }

        public BPMNShapeModelWithPosition ExportData()
        {
            if (_shape is IShapeAddedXaml shapeWithRenderer)
            {
                return shapeWithRenderer.ExportData();
            }

            throw new InvalidOperationException("Unsupported shape for export.");
        }



        public override string? Label { get; set; }
    }
}
