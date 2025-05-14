using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteBoard.Core.Models;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IWhiteBoardTabService
    {
        FooterTabModel CreateNewTab(int index);
        void UpdateTabOrder(IList<FooterTabModel> reorderedTabs);
        void AssociateWhiteBoard(Guid tabId, object whiteBoard);
        object? GetWhiteBoard(Guid tabId);
        void SetCurrent(FooterTabModel tab);
        FooterTabModel? CurrentTab { get; }

        IToolManager? GetToolManager(Guid tabId);

        IToolManager? GetCurrentToolManager();
        void AssociateToolManager(Guid tabId, IToolManager toolManager);
        event Action<FooterTabModel>? TabChanged;
        void AssociateDrawingService(Guid tabId, IDrawingService drawingService);
        IEnumerable<FooterTabModel> AllTabs { get; }
        void SetFolderName(string name);
        string GetFolderName();
        void RemoveTab(FooterTabModel tab);
    }
}
