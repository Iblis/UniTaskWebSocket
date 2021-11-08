using Cysharp.Threading.Tasks;
using System;
using System.Net.WebSockets;
using System.Threading;

namespace UniTaskWebSocket
{
    public class WebSocketWrapper : IWebSocket
    {
        public ClientWebSocket Socket { get; }

        public WebSocketWrapper(ClientWebSocket socket)
        {
            Socket = socket;
        }
        public async UniTask ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await Socket.ConnectAsync(uri, cancellationToken);
        }

        public UniTask ConnectAsync(string uri, CancellationToken cancellationToken)
        {
            return ConnectAsync(new Uri(uri), cancellationToken);
        }

        public async UniTask CloseAsync(WebSocketCloseStatus closeCode, string reason, CancellationToken cancellationToken)
        {
            if(Socket.CloseStatus.HasValue)
            {
                return;
            }
            await Socket.CloseAsync(closeCode, reason, cancellationToken);
        }        

        public void Dispose()
        {
            Socket.Dispose();
        }
    }
}