using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class VirtualTerminalHost : IVirtualTerminalHost
    {
        public ITerminalDriverFactory DriverFactory => _DriverFactory;

        public VirtualTerminalHost(ITerminalDriverFactory driverFactory)
        {
            _DriverFactory = driverFactory;
        }

        private ITerminalDriverFactory _DriverFactory;

        public async Task ListenAsync(IEnumerable<string> httpPrefixes, CancellationToken cancelToken)
        {
            HttpListener listener = new HttpListener();
            foreach (var httpPrefix in httpPrefixes)
                listener.Prefixes.Add(httpPrefix);
            listener.Start();
            for (; ;)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    listener.Stop();
                    return;
                }
                var listenerContext = await listener.GetContextAsync();
                if (!listenerContext.Request.IsWebSocketRequest)
                {
                    listenerContext.Response.StatusCode = (int)HttpStatusCode.UpgradeRequired;
                    listenerContext.Response.Close();
                    continue;
                }
                var webSocketContext = await listenerContext.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;
                var vtSocket = new VTWebSocket(webSocket);
            }
        }

        public class VTClient
        {
            public VTClient(VirtualTerminalHost host, IVTSocket socket, IVirtualTerminalDriver driver)
            {
                _Socket = socket;
                _Driver = driver;
                _Socket.AddMessageHandler(VTProtocol.HB_FRONTENDREADY, HandleFrontendReadyAsync);
            }

            private IVTSocket _Socket;
            private IVirtualTerminalDriver _Driver;

            private async Task HandleFrontendReadyAsync(ArraySegment<ushort> messageData)
            {
                if (messageData.Count != 2)
                    throw new ProtocolViolationException();
                var initialViewWidth = messageData[0];
                var initialViewHeight = messageData[1];
                var context = new VirtualTerminalContext(_Socket, initialViewWidth, initialViewHeight);
                await _Driver.OnReadyAsync(context);
            }

            private async Task HandleViewportResizeAsync(ArraySegment<ushort> messageData)
            {
                if (messageData.Count != 2)
                    throw new ProtocolViolationException();
                var viewWidth = messageData[0];
                var viewHeight = messageData[1];
                await _Driver.OnViewportResizeAsync(viewWidth, viewHeight);
            }

            private async Task HandleInputCharAsync(ArraySegment<ushort> messageData)
            {
                if (messageData.Count != 2)
                    throw new ProtocolViolationException();
                var inputChar = (char)messageData[0];
                var modifiers = (TerminalModifierKeys)messageData[1];
                await _Driver.OnInputCharAsync(inputChar, modifiers);
            }

            private async Task HandleSpecialKeyAsync(ArraySegment<ushort> messageData)
            {
                if (messageData.Count != 2)
                    throw new ProtocolViolationException();
                var inputKey = (TerminalSpecialKey)messageData[0];
                var modifiers = (TerminalModifierKeys)messageData[1];
                await _Driver.OnSpecialKeyAsync(inputKey, modifiers);
            }

            private async Task HandleUserScrollAsync(ArraySegment<ushort> messageData)
            {
                if (messageData.Count != 2)
                    throw new ProtocolViolationException();
                var deltaX = messageData[0];
                var deltaY = messageData[1];
                await _Driver.OnUserScrollAsync(deltaX, deltaY);
            }

            private async Task HandleMouseEventAsync(ArraySegment<ushort> messageData)
            {
                if (messageData.Count != 3)
                    throw new ProtocolViolationException();
                var mouseEventType = (TerminalMouseEventType)messageData[0];
                var mouseX = messageData[1];
                var mouseY = messageData[2];
                await _Driver.OnMouseEventAsync(mouseEventType, mouseX, mouseY);
            }
        }

        private class VirtualTerminalContext : IVirtualTerminalContext
        {
            public int InitialViewportWidth => _ViewportWidth;
            public int InitialViewportHeight => _ViewportHeight;

            public VirtualTerminalContext(IVTSocket socket, int initialViewportWidth,  int initialViewportHeight)
            {
                _Socket = socket;
                _ViewportWidth = initialViewportWidth;
                _ViewportHeight = initialViewportHeight;
            }

            private IVTSocket _Socket;
            private int _ViewportWidth;
            private int _ViewportHeight;

            public async Task<IVirtualTerminalBuffer> CreateBufferAsync(int cols)
            {
                if (cols < 0 || cols > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(cols));
                var requestId = _Socket.NewRequestId();
                var tcs = new TaskCompletionSource<IVirtualTerminalBuffer>();
                var createdHandler = _Socket.AddMessageHandler(VTProtocol.HB_BUFFERCREATED, messageData =>
                {
                    if (messageData[0] == requestId)
                        tcs.SetResult(new VirtualTerminalBuffer(_Socket, messageData));
                    return Task.CompletedTask;
                });
                var createMessage = new ushort[3];
                createMessage[0] = VTProtocol.CB_CREATEBUFFER;
                createMessage[1] = requestId;
                createMessage[2] = (ushort)cols;
                await _Socket.SendMessageAsync(createMessage);
                var result = await tcs.Task;
                _Socket.RemoveMessageHandler(createdHandler);
                return result;
            }
        }
    }
}
