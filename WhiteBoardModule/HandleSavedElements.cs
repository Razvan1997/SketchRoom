using SketchRoom.Toolkit.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WhiteBoard.Core.Models;

namespace WhiteBoardModule
{
    public static class HandleSavedElements
    {
        public static void RestoreShapes(
            List<BPMNShapeModelWithPosition> shapes,
            WhiteBoardControl whiteboard,
            IGenericShapeFactory shapeFactory)
        {
            var dropService = whiteboard._dropService;

            foreach (var shape in shapes)
            {
                var shapeControl = shapeFactory.Create(shape.Type.Value);
                var visual = dropService.HandleDropSavedElements(shape, new Point(shape.Left, shape.Top), shapeControl);

                if (visual != null)
                {
                    dropService.PlaceElementOnCanvas(visual, new Point(shape.Left, shape.Top));
                    dropService.RegisterNodeWhenReady(visual);
                    dropService.SetupConnectorButton(visual);
                }
            }
        }
    }
}
