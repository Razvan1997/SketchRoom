using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;

namespace WhiteBoard.Core.Services
{
    public class WhiteBoardTabService : IWhiteBoardTabService
    {
        private readonly Dictionary<Guid, object> _whiteBoards = new();
        private readonly Dictionary<Guid, IToolManager> _toolManagers = new();
        public readonly List<FooterTabModel> _tabs = new();
        public event Action<FooterTabModel>? TabChanged;
        public FooterTabModel? CurrentTab { get; private set; }
        private readonly Dictionary<Guid, IDrawingService> _drawingServices = new();
        public IEnumerable<FooterTabModel> AllTabs => _tabs;
        public FooterTabModel CreateNewTab(int index)
        {
            var tab = new FooterTabModel
            {
                Name = $"Sketch-{index}",
                IsSelected = false
            };

            _tabs.Add(tab);
            return tab;
        }

        public void AssociateWhiteBoard(Guid tabId, object whiteBoard)
        {
            _whiteBoards[tabId] = whiteBoard;
        }

        public void AssociateToolManager(Guid tabId, IToolManager toolManager)
        {
            _toolManagers[tabId] = toolManager;
        }

        public object? GetWhiteBoard(Guid tabId)
        {
            return _whiteBoards.TryGetValue(tabId, out var wb) ? wb : null;
        }

        public IToolManager? GetToolManager(Guid tabId)
        {
            return _toolManagers.TryGetValue(tabId, out var tm) ? tm : null;
        }

        public IToolManager? GetCurrentToolManager()
        {
            if (CurrentTab == null)
                return null;

            return GetToolManager(CurrentTab.Id);
        }

        public void UpdateTabOrder(IList<FooterTabModel> reorderedTabs)
        {
            _tabs.Clear();
            _tabs.AddRange(reorderedTabs);
        }

        public void SetCurrent(FooterTabModel tab)
        {
            CurrentTab = tab;
            TabChanged?.Invoke(tab);
        }

        public void AssociateDrawingService(Guid tabId, IDrawingService drawingService)
        {
            _drawingServices[tabId] = drawingService;
        }

        public IDrawingService? GetDrawingService(Guid tabId)
        {
            return _drawingServices.TryGetValue(tabId, out var ds) ? ds : null;
        }

        public string FolderName { get; private set; } = string.Empty;

        public void SetFolderName(string folderName)
        {
            FolderName = folderName;
        }

        public string GetFolderName()
        {
            return FolderName;
        }

        public void RemoveTab(FooterTabModel tab)
        {
            _tabs.Remove(tab);
            _whiteBoards.Remove(tab.Id);
            _toolManagers.Remove(tab.Id);
            _drawingServices.Remove(tab.Id);

            if (CurrentTab == tab)
                CurrentTab = null;
        }
    }
}
