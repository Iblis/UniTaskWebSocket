using Cysharp.Threading.Tasks;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace UniTaskWebSocket
{
    public class WebSocketWrapper : IWebSocket
    {
        public ClientWebSocket Socket { get; }

        public String SubProtocol => Socket != null ? Socket.SubProtocol : String.Empty;

        public WebSocketWrapper(ClientWebSocket socket)
        {
            Socket = socket;
        }
        public async UniTask ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await Socket.ConnectAsync(uri, cancellationToken);
        }

        public UniTask ConnectAsync(String uri, CancellationToken cancellationToken)
        {
            return ConnectAsync(new Uri(uri), cancellationToken);
        }

        public async UniTask CloseAsync(WebSocketCloseStatus closeCode, String reason, CancellationToken cancellationToken)
        {
            if(Socket.CloseStatus.HasValue)
            {
                return;
            }
            await Socket.CloseAsync(closeCode, reason, cancellationToken);
        }
        
        public async UniTask<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return await Socket.ReceiveAsync(buffer, cancellationToken);
        }

        public async UniTask SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken token)
        {
            await Socket.SendAsync(buffer, messageType, endOfMessage, token);
        }

        public async UniTask SendText(String message, CancellationToken token)
        {
            var dataToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            await Socket.SendAsync(dataToSend, WebSocketMessageType.Text, true, token);
        }

        public void Dispose()
        {
            Socket.Dispose();
        }
    }
}