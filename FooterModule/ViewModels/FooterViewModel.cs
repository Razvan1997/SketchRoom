using SketchRoom.Toolkit.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WhiteBoard.Core.Colaboration.Interfaces;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;
using WhiteBoardModule.ViewModels;
using WhiteBoardModule.Views;

namespace FooterModule.ViewModels
{
    public class FooterViewModel : BindableBase
    {
        private readonly IWhiteBoardTabService _tabService;
        private readonly IRegionManager _regionManager;
        public ObservableCollection<FooterTabModel> Tabs { get; } = new();

        public DelegateCommand AddTabCommand { get; }
        public DelegateCommand<FooterTabModel> DeleteTabCommand { get; }
        public DelegateCommand<FooterTabModel> RenameTabCommand { get; }

        public FooterViewModel(IWhiteBoardTabService tabService, IRegionManager regionManager)
        {
            _tabService = tabService;
            _regionManager = regionManager;

            AddTabCommand = new DelegateCommand(AddTab);
            DeleteTabCommand = new DelegateCommand<FooterTabModel>(DeleteTab);
            RenameTabCommand = new DelegateCommand<FooterTabModel>(RenameTab);

            AddTab();
        }

        private void AddTab()
        {
            var newTab = _tabService.CreateNewTab(Tabs.Count + 1);
            var toolManager = new ToolManager();
            _tabService.AssociateToolManager(newTab.Id, toolManager);
            _tabService.SetCurrent(newTab);
            var whiteBoard = new WhiteBoardControl();
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
    }
}
