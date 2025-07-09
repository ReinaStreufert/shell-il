using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class VTWebSocket : IVTSocket, IDisposable
    {
        public event Action? SocketClosed;

        public VTWebSocket(WebSocket socket)
        {
            _Socket = socket;
        }

        private WebSocket _Socket;
        private HashSet<IVTMessageHandler> _Handlers = new HashSet<IVTMessageHandler>();
        private object _HandlersSync = new object();
        private Task? _ListenerTask;
        private byte[] _ReceiveBuffer = new byte[1024];
        private CancellationTokenSource _CTokenSource = new CancellationTokenSource();
        private ushort _IdCounter = 0;

        public void AddMessageHandler(IVTMessageHandler handler)
        {
            lock (_HandlersSync)
            {
                _Handlers.Add(handler);
                if (_ListenerTask == null)
                    _ListenerTask = Task.Factory.StartNew(ListenAsync, TaskCreationOptions.LongRunning);
            }
        }

        public void RemoveMessageHandler(IVTMessageHandler handler)
        {
            lock (_HandlersSync)
                _Handlers.Remove(handler);
        }

        public async Task SendMessageAsync(ushort[] message)
        {
            var messageBytes = new byte[message.Length * 2];
            Buffer.BlockCopy(message, 0, messageBytes, 0, messageBytes.Length);
            await _Socket.SendAsync(messageBytes, WebSocketMessageType.Binary, true, _CTokenSource.Token);
        }

        private async Task ListenAsync()
        {
            var cancelToken = _CTokenSource.Token;
            for (; ;)
            {
                var receiveResult = await _Socket.ReceiveAsync(_ReceiveBuffer, cancelToken);
                cancelToken.ThrowIfCancellationRequested();
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    SocketClosed?.Invoke();
                    return;
                } else if (receiveResult.MessageType == WebSocketMessageType.Binary)
                {
                    int messageLength = receiveResult.Count;
                    int receiveBufferOffset = 0;
                    while (!receiveResult.EndOfMessage)
                    {
                        receiveBufferOffset += receiveResult.Count;
                        if (receiveBufferOffset >= _ReceiveBuffer.Length)
                            Array.Resize(ref _ReceiveBuffer, _ReceiveBuffer.Length * 2);
                        receiveResult = await _Socket.ReceiveAsync(new ArraySegment<byte>(_ReceiveBuffer, receiveBufferOffset, _ReceiveBuffer.Length - receiveBufferOffset), cancelToken);
                        cancelToken.ThrowIfCancellationRequested();
                        messageLength += receiveResult.Count;
                    }
                    var messageBuf = new ushort[messageLength / 2];
                    Buffer.BlockCopy(_ReceiveBuffer, 0, messageBuf, 0, messageLength);
                    var messageType = messageBuf[0];
                    IVTMessageHandler[] handlers;
                    lock (_HandlersSync)
                        handlers = _Handlers.ToArray();
                    foreach (var handler in handlers.Where(h => h.MessageId == messageType))
                        _ = handler.HandleAsync(new ArraySegment<ushort>(messageBuf, 1, messageBuf.Length - 1));
                }
            }
        }

        public void Dispose()
        {
            _CTokenSource?.Cancel();
        }

        public ushort NewRequestId()
        {
            ushort currentValue;
            ushort newValue;
            do
            {
                currentValue = _IdCounter;
                newValue = (ushort)(currentValue < ushort.MaxValue ? currentValue + 1 : 0);
            } while (Interlocked.CompareExchange(ref _IdCounter, newValue, currentValue) != currentValue);
            return currentValue;
        }
    }
}
