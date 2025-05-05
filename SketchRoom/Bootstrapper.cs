using DrawingStateService.States;
using FooterModule.Factory;
using Prism.Ioc;
using SketchRoom.Dialogs;
using SketchRoom.Services;
using SketchRoom.Toolkit.Wpf.Factory;
using SketchRoom.Toolkit.Wpf.Services;
using SketchRoom.ViewModels;
using System.Windows;
using System.Windows.Input;
using WhiteBoard.Core.Colaboration.Interfaces;
using WhiteBoard.Core.Colaboration.Services;
using WhiteBoard.Core.Factory.Interfaces;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;
using WhiteBoardModule;
using WhiteBoardModule.XAML;

namespace SketchRoom
{
    public class Bootstrapper : PrismBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            var window = new MainWindow
            {
                DataContext = Container.Resolve<MainViewModel>()
            };
            return window;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<MainViewModel>();
            ViewModelLocationProvider.Register<RegistrationDialog, RegistrationDialogViewModel>();
            ViewModelLocationProvider.Register<SaveSketchDialog, SaveSketchDialogViewModel>();

            containerRegistry.RegisterSingleton<WhiteboardHubClient>();

            //containerRegistry.RegisterSingleton<IToolManager, ToolManager>();
            containerRegistry.RegisterSingleton<IDrawingService, WhiteBoard.Core.Services.DrawingService>();
            containerRegistry.RegisterSingleton<ICanvasRenderer, CanvasRenderer>();
            containerRegistry.RegisterSingleton<ICommandManager, WhiteBoard.Core.Services.CommandManager>();
            containerRegistry.RegisterSingleton<ISnapService, SnapService>();
            containerRegistry.RegisterSingleton<ICollaborationService, CollaborationService>();
            containerRegistry.RegisterSingleton<ICommandManager, WhiteBoard.Core.Services.CommandManager>();
            containerRegistry.RegisterSingleton<IZoomPanService, ZoomPanService>();

            containerRegistry.RegisterSingleton<ICollaborationService, CollaborationService>();
            containerRegistry.RegisterSingleton<SelectedToolService>();
            containerRegistry.RegisterSingleton<IBpmnShapeFactory, BpmnShapeFactory>();
            containerRegistry.RegisterSingleton<IDrawingPreferencesService, DrawingPreferencesService>();
            //containerRegistry.RegisterSingleton<IWhiteBoardFactory, WhiteBoardFactory>();
            containerRegistry.RegisterSingleton<IWhiteBoardTabService, WhiteBoardTabService>();
            containerRegistry.RegisterSingleton<IShapeSelectionService, ShapeSelectionService>();
            containerRegistry.RegisterSingleton<UndoRedoService>();
            containerRegistry.RegisterSingleton<IZOrderService, ZOrderService>();
            containerRegistry.RegisterSingleton<IContextMenuService, ContextMenuService>();
            containerRegistry.RegisterSingleton<IShapeRendererFactory, ShapeRendererFactory>();
            containerRegistry.RegisterSingleton<IGenericShapeFactory, GenericShapeFactory>();
        }

        protected override void OnInitialized()
        {
            var mainWindow = (Window)Shell;
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<TopBarModule.Modules.TopBarModule>();
            moduleCatalog.AddModule<UsersInteractionsModule.Modules.UsersInteractionsModule>();
            moduleCatalog.AddModule<WhiteBoardModule.Modules.WhiteBoardModule>();
            moduleCatalog.AddModule<LobbyHostingModule.Modules.LobbyHostingModule>();
            moduleCatalog.AddModule<ParticipationModule.Modules.ParticipationModule>();
            moduleCatalog.AddModule<FooterModule.Modules.FooterModule>();
        }
    }
}
