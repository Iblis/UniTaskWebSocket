using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using Cysharp.Threading.Tasks;

namespace UniTaskWebSocket
{
    // this is the client implementation used by browsers
    public class WebGLWebSocket : IWebSocket
    {
        public WebGLWebSocket()
        {
            instanceId = WebSocketAllocate();
        }

        ~WebGLWebSocket()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                HandleInstanceDestroy(instanceId);
                _disposed = true;
            }
        }

        public UniTask ConnectAsync(Uri uri, CancellationToken token)
        {
            return ConnectAsync(uri.ToString(), token);
        }

        public UniTask ConnectAsync(string uri, CancellationToken token)
        {
            clients[instanceId] = this;
            _connectCompletionSource = new UniTaskCompletionSource();

            int result = WebSocketConnect(uri, instanceId, OnOpen, OnMessage, OnError, OnClose);

            if (result < 0)
            {
                throw WebSocketHelpers.GetErrorMessageFromCode(result);
            }

            return _connectCompletionSource.Task.AttachExternalCancellation(token);
        }

        public UniTask CloseAsync(WebSocketCloseStatus closeCode, string reason, CancellationToken token)
        {
            if(_closeCompletionSource != null)
            {
                return UniTask.CompletedTask;
            }
            _closeCompletionSource = new UniTaskCompletionSource();
            int ret = WebSocketClose(instanceId, (int)closeCode, reason);

            if (ret < 0)
                throw WebSocketHelpers.GetErrorMessageFromCode(ret);

            return _closeCompletionSource.Task.AttachExternalCancellation(token);
        }

        public UniTask SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken token)
        {
            if (messageType == WebSocketMessageType.Text)
            {
                SendText(System.Text.Encoding.UTF8.GetString(buffer.Array));
            }
            else
            {
                int ret = WebSocketSendFragment(instanceId, buffer.Array, buffer.Array.Length, endOfMessage);

                if (ret < 0) throw WebSocketHelpers.GetErrorMessageFromCode(ret);
            }
            return UniTask.CompletedTask;
        }

        public UniTask SendText(string message)
        {
            int ret = WebSocketSendText(instanceId, message);

            if (ret < 0) throw WebSocketHelpers.GetErrorMessageFromCode(ret);

            return UniTask.CompletedTask;
        }

        public async UniTask<WebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                WebSocketMessage message = await receivedQueue.Reader.ReadAsync(cancellationToken);
                message.MoveDataTo(buffer.Slice(0, message.Result.Count));
                return message.Result;
            }
            catch (ChannelClosedException)
            {
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, "Connection Closed by Server");
            }
        }

        // TODO: allow using modern data types if runtime allows it
        /*public async UniTask<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken token)
        {
            try
            {
                WebSocketMessage message = await receivedQueue.Reader.ReadAsync();
                Debug.Log($"Message Received with bytes: {message.Result.Count}");
                message.MoveDataTo(buffer.AsMemory());
                return message.Result;
            }
            catch (ChannelClosedException)
            {
                throw new EndOfStreamException();
            }
        }*/

        readonly int instanceId;
        readonly Channel<WebSocketMessage> receivedQueue = Channel.CreateSingleConsumerUnbounded<WebSocketMessage>();

        bool _disposed = false;
        UniTaskCompletionSource _connectCompletionSource;
        UniTaskCompletionSource _closeCompletionSource;

        static readonly Dictionary<int, WebGLWebSocket> clients = new Dictionary<int, WebGLWebSocket>();
        
        public static void HandleInstanceDestroy(int instanceId)
        {
            clients.Remove(instanceId);
            WebSocketFree(instanceId);
        }

        #region Javascript native functions
        [DllImport("__Internal")]
        public static extern int WebSocketAllocate();

        [DllImport("__Internal")]
        public static extern int WebSocketAddSubProtocol(int instanceId, string subprotocol);

        [DllImport("__Internal")]
        public static extern void WebSocketFree(int instanceId);

        [DllImport("__Internal")]
        public static extern int WebSocketConnect(string url,
            int id,
            Action<int> onopen,
            Action<int, IntPtr, int, bool> ondata,
            Action<int, IntPtr> onerror,
            Action<int,int> onclose);

        [DllImport("__Internal")]
        public static extern int WebSocketClose(int instanceId, int code, string reason);

        [DllImport("__Internal")]
        public static extern int WebSocketSend(int instanceId, byte[] dataPtr, int dataLength);

        [DllImport("__Internal")]
        public static extern int WebSocketSendFragment(int instanceId, byte[] dataPtr, int dataLength, bool endOfMessage);

        [DllImport("__Internal")]
        public static extern int WebSocketSendText(int instanceId, string message);

        [DllImport("__Internal")]
        public static extern int WebSocketGetState(int instanceId);
               

        #endregion

        #region Javascript callbacks

        [MonoPInvokeCallback(typeof(Action))]
        public static void OnOpen(int id)
        {
            clients[id]._connectCompletionSource.TrySetResult();
        }

        [MonoPInvokeCallback(typeof(Action))]
        public static void OnClose(int id, int closeCode)
        {
            clients[id].receivedQueue.Writer.Complete();
            clients[id]._closeCompletionSource.TrySetResult();
            clients.Remove(id);
        }

        [MonoPInvokeCallback(typeof(Action))]
        public static void OnError(int instanceId, IntPtr errorPtr)
        {
            WebGLWebSocket instanceRef;

            if (clients.TryGetValue(instanceId, out instanceRef))
            {
                string errorMsg = Marshal.PtrToStringAuto(errorPtr);
                // TODO: handle on error....
                //instanceRef.DelegateOnErrorEvent(errorMsg);
            }
        }

        [MonoPInvokeCallback(typeof(Action))]
        public static void OnMessage(int instanceId, IntPtr ptr, int length, bool isTextMessage)
        {
            clients[instanceId].receivedQueue.Writer.TryWrite(new WebSocketMessage(ptr, length, isTextMessage));
        }

        #endregion
        
        public enum WebSocketCloseCode
        {
            /* Do NOT use NotSet - it's only purpose is to indicate that the close code cannot be parsed. */
            NotSet = 0,
            Normal = 1000,
            Away = 1001,
            ProtocolError = 1002,
            UnsupportedData = 1003,
            Undefined = 1004,
            NoStatus = 1005,
            Abnormal = 1006,
            InvalidData = 1007,
            PolicyViolation = 1008,
            TooBig = 1009,
            MandatoryExtension = 1010,
            ServerError = 1011,
            TlsHandshakeFailure = 1015
        }

        public static class WebSocketHelpers
        {
            public static WebSocketCloseCode ParseCloseCodeEnum(int closeCode)
            {

                if (WebSocketCloseCode.IsDefined(typeof(WebSocketCloseCode), closeCode))
                {
                    return (WebSocketCloseCode)closeCode;
                }
                else
                {
                    return WebSocketCloseCode.Undefined;
                }
            }

            public static WebSocketException GetErrorMessageFromCode(int errorCode)
            {
                switch (errorCode)
                {
                    case -1:
                        return new WebSocketUnexpectedException("WebSocket instance not found.");
                    case -2:
                        return new WebSocketInvalidStateException("WebSocket is already connected or in connecting state.");
                    case -3:
                        return new WebSocketInvalidStateException("WebSocket is not connected.");
                    case -4:
                        return new WebSocketInvalidStateException("WebSocket is already closing.");
                    case -5:
                        return new WebSocketInvalidStateException("WebSocket is already closed.");
                    case -6:
                        return new WebSocketInvalidStateException("WebSocket is not in open state.");
                    case -7:
                        return new WebSocketInvalidArgumentException("Cannot close WebSocket. An invalid code was specified or reason is too long.");
                    default:
                        return new WebSocketUnexpectedException("Unknown error.");
                }
            }

            public class WebSocketException : Exception
            {
                public WebSocketException() { }
                public WebSocketException(string message) : base(message) { }
                public WebSocketException(string message, Exception inner) : base(message, inner) { }
            }

            public class WebSocketUnexpectedException : WebSocketException
            {
                public WebSocketUnexpectedException() { }
                public WebSocketUnexpectedException(string message) : base(message) { }
                public WebSocketUnexpectedException(string message, Exception inner) : base(message, inner) { }
            }

            public class WebSocketInvalidArgumentException : WebSocketException
            {
                public WebSocketInvalidArgumentException() { }
                public WebSocketInvalidArgumentException(string message) : base(message) { }
                public WebSocketInvalidArgumentException(string message, Exception inner) : base(message, inner) { }
            }

            public class WebSocketInvalidStateException : WebSocketException
            {
                public WebSocketInvalidStateException() { }
                public WebSocketInvalidStateException(string message) : base(message) { }
                public WebSocketInvalidStateException(string message, Exception inner) : base(message, inner) { }
            }

        }
    }
}
