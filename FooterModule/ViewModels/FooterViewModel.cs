using Prism.Events;
using SketchRoom.Toolkit.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WhiteBoard.Core.Colaboration.Interfaces;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;
using WhiteBoardModule;
using WhiteBoardModule.Events;
using WhiteBoardModule.ViewModels;
using WhiteBoardModule.Views;

namespace FooterModule.ViewModels
{
    public class FooterViewModel : BindableBase
    {
        private readonly IWhiteBoardTabService _tabService;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        public ObservableCollection<FooterTabModel> Tabs { get; } = new();

        public DelegateCommand AddTabCommand { get; }
        public DelegateCommand<FooterTabModel> DeleteTabCommand { get; }
        public DelegateCommand<FooterTabModel> RenameTabCommand { get; }

        public FooterViewModel(IWhiteBoardTabService tabService, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _tabService = tabService;
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;

            AddTabCommand = new DelegateCommand(AddTab);
            DeleteTabCommand = new DelegateCommand<FooterTabModel>(DeleteTab);
            RenameTabCommand = new DelegateCommand<FooterTabModel>(RenameTab);
            
            _eventAggregator.GetEvent<TabsRestoredEvent>().Subscribe(OnTabsRestored);

            AddTab();
        }

        private void AddTab()
        {
            var newTab = _tabService.CreateNewTab(Tabs.Count + 1);
            var toolManager = new ToolManager();
            _tabService.AssociateToolManager(newTab.Id, toolManager);
            _tabService.SetCurrent(newTab);

            var drawingService = new DrawingService();
            _tabService.AssociateDrawingService(newTab.Id, drawingService);

            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
            var whiteBoard = new WhiteBoardControl(drawingService, preferences);

            _tabService.AssociateWhiteBoard(newTab.Id, whiteBoard);
            Tabs.Add(newTab);
            SelectTab(newTab);
        }

        private void DeleteTab(FooterTabModel tab)
        {
            if (Tabs.Contains(tab))
                Tabs.Remove(tab);
        }

        private void RenameTab(FooterTabModel tab)
        {
            // poți face dialog cu input de la user
            tab.Name = "Renamed " + DateTime.Now.Ticks;
        }

        public void SelectTab(FooterTabModel tab)
        {
            foreach (var t in Tabs)
                t.IsSelected = false;

            tab.IsSelected = true;
            _tabService.SetCurrent(tab);

            var whiteBoard = _tabService.GetWhiteBoard(tab.Id);
            if (whiteBoard is UserControl control)
            {
                // Caută instanța activă de WhiteBoardView
                var wbView = _regionManager.Regions["ContentRegion"]
                             .ActiveViews
                             .OfType<WhiteBoardView>()
                             .FirstOrDefault();

                if (wbView != null)
                {
                    wbView.WhiteboardHostControl.Content = control;

                    if (control is IWhiteBoardAdapter adapter &&
                        wbView.DataContext is WhiteBoardViewModel vm)
                    {
                        vm.SetControlAdapter(adapter);

                        if (control is WhiteBoardControl whiteboard)
                        {
                            whiteboard.LineDrawn += vm.OnLineDrawn;
                            whiteboard.LivePointDrawn += vm.OnDrawPointLive;
                            whiteboard.MouseMoved += vm.OnMouseMoved;
                        }
                    }
                }
            }
        }

        public void ReorderTabs(FooterTabModel dragged, FooterTabModel target)
        {
            int oldIndex = Tabs.IndexOf(dragged);
            int newIndex = Tabs.IndexOf(target);

            if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
            {
                Tabs.Move(oldIndex, newIndex);
                _tabService.UpdateTabOrder(Tabs);
            }
        }

        private void OnTabsRestored(List<SavedWhiteBoardModel> models)
        {
            var internalTabs = _tabService.AllTabs.ToList();

            var placeholder = internalTabs.FirstOrDefault(t => t.Name == "Sketch-1");

            if (placeholder != null && internalTabs.Count == 1)
            {
                _tabService.RemoveTab(placeholder); 
                Tabs.Remove(placeholder);
            }

            // Sortează modelul după nume de tip "Sketch-1", "Sketch-2"
            var sortedModels = models
                .OrderBy(m =>
                {
                    var parts = m.TabName.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int number))
                        return number;
                    return int.MaxValue; // dacă nu are format valid, pune-l la final
                })
                .ToList();

            var addedTabs = new List<FooterTabModel>();

            foreach (var model in sortedModels)
            {
                var tab = AddTabFromModel(model);
                addedTabs.Add(tab);
            }

            if (addedTabs.Count > 0)
            {
                SelectTab(addedTabs[0]); // ✅ Selectează primul tab după sortare
            }
        }

        private FooterTabModel AddTabFromModel(SavedWhiteBoardModel model)
        {
            var newTab = _tabService.CreateNewTab(Tabs.Count + 1);
            newTab.Name = model.TabName;

            var toolManager = new ToolManager();
            _tabService.AssociateToolManager(newTab.Id, toolManager);

            // 🔥 Setează tab-ul curent ÎNAINTE de a crea WhiteBoardControl
            _tabService.SetCurrent(newTab);

            var drawingService = new DrawingService();
            _tabService.AssociateDrawingService(newTab.Id, drawingService);

            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
            var whiteBoard = new WhiteBoardControl(drawingService, preferences);

            var shapeFactory = ContainerLocator.Container.Resolve<IGenericShapeFactory>();
            HandleSavedElements.RestoreShapes(model.Shapes, whiteBoard, shapeFactory, nodeMap =>
            {
                HandleSavedElements.RestoreConnections(model.Connections, whiteBoard, nodeMap);
            });

            _tabService.AssociateWhiteBoard(newTab.Id, whiteBoard);
            Tabs.Add(newTab);

            return newTab;
        }
    }
}
