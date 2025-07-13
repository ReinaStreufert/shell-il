using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class BufferViewport : IBufferViewport
    {
        public IVirtualTerminalBuffer SourceBuffer => _SourceBuffer;

        public BufferViewport(IVirtualTerminalBuffer sourceBuffer, IVTSocket socket, ArraySegment<ushort> createdMessageData)
        {
            _Socket = socket;
            _SourceBuffer = sourceBuffer;
            _ViewportId = createdMessageData[1];
            _XOffset = createdMessageData[2];
            _YOffset = createdMessageData[3];
            _CursorState = (TerminalCursorState)createdMessageData[4];
            _QueuedCursorState = _CursorState;
            _UpdateHandler = socket.AddMessageHandler(VTProtocol.HB_VIEWPORTUPDATED, HandleViewportUpdatedAsync);
        }

        private IVTMessageHandler _UpdateHandler;
        private IVTSocket _Socket;
        private IVirtualTerminalBuffer _SourceBuffer;
        private ushort _ViewportId;
        private ushort _XOffset;
        private ushort _YOffset;
        private TerminalCursorState _CursorState;
        private TerminalPosition? _QueuedScrollTo;
        private TerminalPosition? _QueuedScrollOffset;
        private TerminalCursorState _QueuedCursorState;
        private bool _QueuedCursorInView = false;
        private object _QueueSync = new object();

        public Task FlushAsync() => FlushAsync(false);

        private async Task FlushAsync(bool present)
        {
            await _SourceBuffer.FlushAsync();
            TerminalPosition? queuedScrollTo;
            TerminalPosition? queuedScrollOffset;
            TerminalCursorState queuedCursorState;
            bool queuedCursorInView;
            lock (_QueueSync)
            {
                queuedScrollTo = _QueuedScrollTo;
                queuedScrollOffset = _QueuedScrollOffset;
                queuedCursorState = _QueuedCursorState;
                queuedCursorInView = _QueuedCursorInView;
                _QueuedScrollTo = null;
                _QueuedScrollOffset = null;
                _QueuedCursorInView = false;
            }
            var commandMsgLen = 4;
            var actionFlags = present ? VTProtocol.ViewportActionFlags.Present : VTProtocol.ViewportActionFlags.None;
            if (queuedScrollTo != null)
            {
                actionFlags |= VTProtocol.ViewportActionFlags.ScrollTo;
                commandMsgLen += 2;
            }
            if (queuedScrollOffset != null)
            {
                actionFlags |= VTProtocol.ViewportActionFlags.ApplyScrollOffset;
                commandMsgLen += 2;
            }
            if (_QueuedCursorInView)
                actionFlags |= VTProtocol.ViewportActionFlags.ScrollCursorIntoView;
            if (_CursorState != queuedCursorState)
            {
                actionFlags |= VTProtocol.ViewportActionFlags.SetCursorState;
                commandMsgLen++;
            }
            var requestId = _Socket.NewRequestId();
            var commandMessage = new ushort[commandMsgLen];
            commandMessage[0] = VTProtocol.CB_VIEWPORTCOMMAND;
            commandMessage[1] = requestId;
            commandMessage[2] = _ViewportId;
            commandMessage[3] = (ushort)actionFlags;
            var i = 4;
            if (queuedScrollTo != null)
            {
                commandMessage[i++] = queuedScrollTo.X;
                commandMessage[i++] = queuedScrollTo.Y;
            }
            if (queuedScrollOffset != null)
            {
                commandMessage[i++] = queuedScrollOffset.X;
                commandMessage[i++] = queuedScrollOffset.Y;
            }
            if (actionFlags.HasFlag(VTProtocol.ViewportActionFlags.SetCursorState))
                commandMessage[i++] = (ushort)queuedCursorState;
            var tcs = new TaskCompletionSource();
            var reqProcessedHandler = _Socket.AddMessageHandler(VTProtocol.HB_REQUESTPROCESSED, messageData =>
            {
                if (messageData[0] == requestId)
                    tcs.SetResult();
                return Task.CompletedTask;
            });
            await _Socket.SendMessageAsync(commandMessage);
            await tcs.Task;
            _Socket.RemoveMessageHandler(reqProcessedHandler);
        }

        public async Task<TerminalPosition> GetScrollOffsetAsync()
        {
            await FlushAsync();
            return new TerminalPosition(_XOffset, _YOffset);
        }

        public async Task PresentAsync()
        {
            await FlushAsync(true);
        }

        public Task ScrollAsync(int x, int y) => ScrollAsync(new TerminalPosition(x, y));

        public async Task ScrollAsync(TerminalPosition offset)
        {
            if (_QueuedCursorInView)
                await FlushAsync();
            lock (_QueueSync)
            {
                if (_QueuedScrollOffset == null)
                    _QueuedScrollOffset = offset;
                else
                    _QueuedScrollOffset += offset;
            }
        }

        public Task ScrollToAsync(int x, int y) => ScrollToAsync(new TerminalPosition(x, y));

        public async Task ScrollToAsync(TerminalPosition scrollOffset)
        {
            if (_QueuedCursorInView)
                await FlushAsync();
            lock (_QueueSync)
            {
                _QueuedScrollOffset = null;
                _QueuedScrollTo = new TerminalPosition(scrollOffset.X, scrollOffset.Y);
            }
        }

        public Task ScrollCursorIntoViewAsync()
        {
            lock (_QueueSync)
                _QueuedCursorInView = true;
            return Task.CompletedTask;
        }

        public Task SetCursorStateAsync(TerminalCursorState cursorState)
        {
            lock (_QueueSync)
                _QueuedCursorState = cursorState;
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await FlushAsync();
            var destroyMessage = new ushort[2];
            destroyMessage[0] = VTProtocol.CB_DESTROYVIEWPORT;
            destroyMessage[1] = _ViewportId;
            await _Socket.SendMessageAsync(destroyMessage);
            _Socket.RemoveMessageHandler(_UpdateHandler);
        }

        private Task HandleViewportUpdatedAsync(ArraySegment<ushort> messageData)
        {
            if (messageData[0] != _ViewportId)
                return Task.CompletedTask;
            _XOffset = messageData[1];
            _YOffset = messageData[2];
            _CursorState = (TerminalCursorState)messageData[3];
            return Task.CompletedTask;
        }
    }
}
