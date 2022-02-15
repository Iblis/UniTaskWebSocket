using Cysharp.Threading.Tasks;
using System;
using System.Net.WebSockets;
using System.Threading;

namespace UniTaskWebSocket
{
    public interface IWebSocket : IDisposable
    {
        String SubProtocol { get; }

        UniTask ConnectAsync(Uri uri, CancellationToken cancellationToken);

        UniTask ConnectAsync(String uri, CancellationToken cancellationToken);

        UniTask CloseAsync(WebSocketCloseStatus closeCode, String reason, CancellationToken cancellationToken);

        UniTask<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);

        UniTask SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken token);

        UniTask SendText(String message, CancellationToken token);
    }
}