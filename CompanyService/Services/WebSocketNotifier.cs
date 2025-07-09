using Fleck;
using System.Collections.Concurrent;
using System.Linq;

namespace CompanyService.Services
{
    public class WebSocketNotifier
    {
        // Thread-safe socket koleksiyonu
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
                    // Ba�lant� kapand���nda, listeden silmek i�in
                    RemoveSocket(socket);
                };
                socket.OnMessage = msg => Console.WriteLine("Received from client: " + msg);
            });
        }

        // D��ar�dan �a��r�lan mesaj g�nderici
        public void NotifyAll(string message)
        {
            foreach (var socket in _sockets.ToList())
            {
                if (socket.IsAvailable)
                    socket.Send(message);
            }
        }

        // Ba�lant� kapand���nda silmek i�in yard�mc� fonksiyon
        private void RemoveSocket(IWebSocketConnection socket)
        {
            // ConcurrentBag silmeye izin vermedi�i i�in filtreyle yeni bag yarat�yoruz.
            var filtered = _sockets.Where(s => s != socket).ToList();
            while (!_sockets.IsEmpty)
                _sockets.TryTake(out _);

            foreach (var s in filtered)
                _sockets.Add(s);
        }

        // Kullan�m�:
        // _notifier.NotifyAll("{ \"event\": \"updated\", \"id\": 5 }");
        // _notifier.NotifyAll("{ \"event\": \"deleted\", \"id\": 5 }");
    }
}
