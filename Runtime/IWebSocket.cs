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

        UniTask<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);

        UniTask SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken token);

        UniTask SendText(string message, CancellationToken token);
    }
}