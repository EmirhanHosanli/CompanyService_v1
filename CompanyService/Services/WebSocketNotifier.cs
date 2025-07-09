using Fleck;
using System.Collections.Concurrent;
using System.Linq;

namespace CompanyService.Services
{
    public class WebSocketNotifier
    {
        
        private static readonly ConcurrentBag<IWebSocketConnection> _sockets = new();

        public WebSocketNotifier()
        {
            var server = new WebSocketServer("ws://0.0.0.0:5001/ws/companies/updates");
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("WebSocket client connected!");
                    _sockets.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("WebSocket client disconnected!");
                  
                    RemoveSocket(socket);
                };
                socket.OnMessage = msg => Console.WriteLine("Received from client: " + msg);
            });
        }

       
        public void NotifyAll(string message)
        {
            foreach (var socket in _sockets.ToList())
            {
                if (socket.IsAvailable)
                    socket.Send(message);
            }
        }

        
        private void RemoveSocket(IWebSocketConnection socket)
        {
           
            var filtered = _sockets.Where(s => s != socket).ToList();
            while (!_sockets.IsEmpty)
                _sockets.TryTake(out _);

            foreach (var s in filtered)
                _sockets.Add(s);
        }

     
    }
}
