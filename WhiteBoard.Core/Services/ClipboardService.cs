using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using WhiteBoard.Core.Services.Interfaces;
using SketchRoom.Models.DTO;
using SketchRoom.Models.Shapes;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Helpers;

namespace WhiteBoard.Core.Services
{
    public class ClipboardService : IClipboardService
    {
        private BPMNShapeModel? _copiedShapeModel;
        private readonly IDropService _dropService;

        public ClipboardService(IDropService dropService)
        {
            _dropService = dropService;
        }

        public void Copy(IInteractiveShape shape)
        {
            if (shape is IShapeAddedXaml xamlShape)
            {
                _copiedShapeModel = new BPMNShapeModel
                {
                    Type = xamlShape.GetShapeType(),
                    ShapeContent = shape
                };
            }
            else
            {
                if (shape.Visual is FrameworkElement fe)
                {
                    var uri = ShapeMetadata.GetSvgUri(fe);
                    if (uri != null)
                    {
                        _copiedShapeModel = new BPMNShapeModel
                        {
                            SvgUri = uri
                        };
                    }
                }
            }
        }

        public IInteractiveShape? Paste(Point position)
        {
            if (_copiedShapeModel == null)
                return null;

            var element = _dropService.HandleDrop(_copiedShapeModel, position);

            return (element as IInteractiveShape)
                   ?? (element is FrameworkElement fe && fe is IInteractiveShape ish ? ish : null);
        }
    }
}
