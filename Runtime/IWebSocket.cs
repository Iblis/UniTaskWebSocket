using Cysharp.Threading.Tasks;
using System;
using System.Net.WebSockets;
using System.Threading;

namespace UniTaskWebSocket
{
    public interface IWebSocket : IDisposable
    {
        UniTask ConnectAsync(Uri uri, CancellationToken cancellationToken);

        UniTask ConnectAsync(string uri, CancellationToken cancellationToken);

        UniTask CloseAsync(WebSocketCloseStatus closeCode, string reason, CancellationToken cancellationToken);
    }
}