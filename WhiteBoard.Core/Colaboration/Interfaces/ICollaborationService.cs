using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoard.Core.Colaboration.Interfaces
{
    public interface ICollaborationService
    {
        void Initialize(string sessionCode, bool isHost, bool isParticipant);
        void AttachWhiteboard(IWhiteBoardAdapter whiteboard);

        Task SendLineAsync(IEnumerable<Point> points, string colorHex, double thickness);
        Task SendLivePointAsync(Point point);
        Task SendCursorPositionAsync(Point position, string? imageBase64 = null);
    }
}
