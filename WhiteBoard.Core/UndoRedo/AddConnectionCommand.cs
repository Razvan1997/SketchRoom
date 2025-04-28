using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.UndoRedo
{
    public class AddConnectionCommand : IUndoableCommand
    {
        private readonly Canvas _canvas;
        private readonly BPMNConnection _connection;
        private readonly IList<BPMNConnection> _connections;

        public AddConnectionCommand(Canvas canvas, BPMNConnection connection, IList<BPMNConnection> connections)
        {
            _canvas = canvas;
            _connection = connection;
            _connections = connections;
        }

        public void Execute()
        {
            _connections.Add(_connection);

            if (_connection.Visual is FrameworkElement fe)
                fe.Tag = "Connector";

            _canvas.Children.Remove(_connection.Visual);
            _canvas.Children.Add(_connection.Visual);

            if (_connection.ConnectionDot != null)
            {
                _canvas.Children.Remove(_connection.ConnectionDot);
                _canvas.Children.Add(_connection.ConnectionDot);
            }
        }

        public void Undo()
        {
            _connections.Remove(_connection);
            _canvas.Children.Remove(_connection.Visual);

            if (_connection.ConnectionDot != null)
                _canvas.Children.Remove(_connection.ConnectionDot);
        }
    }
}
