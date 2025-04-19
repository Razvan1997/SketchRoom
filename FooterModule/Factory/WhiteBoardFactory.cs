using SketchRoom.Toolkit.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WhiteBoard.Core.Factory.Interfaces;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.Tools;

namespace FooterModule.Factory
{
    //public class WhiteBoardFactory : IWhiteBoardFactory
    //{
    //    //public UserControl CreateNewWhiteBoard(out IToolManager toolManager)
    //    //{
    //    //    var drawingService = ContainerLocator.Container.Resolve<IDrawingService>();
    //    //    var canvasRenderer = ContainerLocator.Container.Resolve<ICanvasRenderer>();
    //    //    var snapService = ContainerLocator.Container.Resolve<ISnapService>();
    //    //    var toolSelector = ContainerLocator.Container.Resolve<SelectedToolService>();
    //    //    var drawingPrefs = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
    //    //    var factory = ContainerLocator.Container.Resolve<IBpmnShapeFactory>();
    //    //    var selectionService = new SelectionService(); // nou pentru fiecare
    //    //    toolManager = new ToolManager();

    //    //    var dropService = new DropService(null!, factory, toolManager, null!, null!, new(), toolSelector);

    //    //    var whiteboard = new WhiteBoardControl(
    //    //        toolManager,
    //    //        snapService,
    //    //        toolSelector,
    //    //        drawingPrefs,
    //    //        dropService,
    //    //        factory,
    //    //        selectionService
    //    //    );

    //    //    return whiteboard;
    //    //}
    //}
}
