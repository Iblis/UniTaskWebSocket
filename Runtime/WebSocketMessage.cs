using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.InteropServices;

namespace UniTaskWebSocket
{
    public class WebSocketMessage
    {
        public WebSocketReceiveResult Result { get; }

        public WebSocketMessage(IntPtr ptr, int length, bool isTextMessage)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(length);
            Marshal.Copy(ptr, _buffer, 0, length);            
            Result = new WebSocketReceiveResult(length, isTextMessage ? WebSocketMessageType.Text : WebSocketMessageType.Binary, true); // endOfMessage is always true when we receive data from a Browser's Websocket API
        }

        public void MoveDataTo(Memory<byte> span)
        {
            if(_buffer == null)
            {
                throw new InvalidOperationException(INVALID_OPERATION);
            }
            _buffer.AsMemory().Slice(0, Result.Count).CopyTo(span);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null;
        }

        private byte[] _buffer;

        private const String INVALID_OPERATION = "No Data to move. MoveTo(Span<byte> span) can only be called once";
    }
}
