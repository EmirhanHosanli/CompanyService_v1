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
                    // Baðlantý kapandýðýnda, listeden silmek için
                    RemoveSocket(socket);
                };
                socket.OnMessage = msg => Console.WriteLine("Received from client: " + msg);
            });
        }

        // Dýþarýdan çaðýrýlan mesaj gönderici
        public void NotifyAll(string message)
        {
            foreach (var socket in _sockets.ToList())
            {
                if (socket.IsAvailable)
                    socket.Send(message);
            }
        }

        // Baðlantý kapandýðýnda silmek için yardýmcý fonksiyon
        private void RemoveSocket(IWebSocketConnection socket)
        {
            // ConcurrentBag silmeye izin vermediði için filtreyle yeni bag yaratýyoruz.
            var filtered = _sockets.Where(s => s != socket).ToList();
            while (!_sockets.IsEmpty)
                _sockets.TryTake(out _);

            foreach (var s in filtered)
                _sockets.Add(s);
        }

        // Kullanýmý:
        // _notifier.NotifyAll("{ \"event\": \"updated\", \"id\": 5 }");
        // _notifier.NotifyAll("{ \"event\": \"deleted\", \"id\": 5 }");
    }
}
