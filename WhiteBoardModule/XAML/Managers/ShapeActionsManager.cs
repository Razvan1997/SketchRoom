using System.Windows.Controls;
using System.Windows.Media;
using WhiteBoard.Core.Events;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Managers
{
    public class ShapeActionsManager
    {
        private readonly GenericShapeControl _shapeControl;

        public ShapeActionsManager(GenericShapeControl shapeControl)
        {
            _shapeControl = shapeControl;
        }

        public void HandleAction(object sender, ShapeActionEventArgs e)
        {
            switch (e.ActionType)
            {
                case ShapeActionType.ChangeBackgroundColor:
                    if (e.Parameter is Brush background)
                        SetBackground(background);
                    break;

                case ShapeActionType.ChangeStrokeColor:
                    if (e.Parameter is Brush stroke)
                        SetStroke(stroke);
                    break;

                case ShapeActionType.ChangeBorderThickness:
                    if (e.Parameter is double thickness)
                        SetBorderThickness(thickness);
                    break;

                case ShapeActionType.Rotate:
                    if (e.Parameter is double angle)
                        Rotate(angle);
                    break;

                case ShapeActionType.ChangeForegroundColor:
                    if (e.Parameter is Brush fore)
                        SetForeground(fore);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetBackground(Brush brush)
        {
            if (_shapeControl._renderer is IBackgroundChangable changable)
            {
                changable.SetBackground(brush);
            }
        }

        private void SetStroke(Brush brush)
        {
            if (_shapeControl._renderer is IStrokeChangable stroke)
            {
                stroke.SetStroke(brush);
            }
        }

        private void SetBorderThickness(double thickness)
        {
            // Ex: rectangleRenderer.SetBorderThickness(thickness);
        }

        private void Rotate(double angleDegrees)
        {
            // Ex: rotesti forma
        }

        private void SetForeground(Brush brush)
        {
            if (_shapeControl._renderer is IForegroundChangable changable)
            {
                changable.SetForeground(brush);
            }

            // 2. Schimbăm foreground-ul textului adăugat manual
            if (_shapeControl.Content is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is TextBox textBox && (string?)textBox.Tag == "interactive")
                    {
                        textBox.Foreground = brush;
                    }
                }
            }
        }
    }
}
