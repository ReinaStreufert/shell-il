using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class VirtualTerminalBuffer : IVirtualTerminalBuffer
    {
        public int Width => _BufferWidth;

        public VirtualTerminalBuffer(IVTSocket socket, ArraySegment<ushort> createdMessageData)
        {
            _Socket = socket;
            _BufferId = createdMessageData[1];
            _BufferWidth = createdMessageData[2];
            _BufferHeight = createdMessageData[3];
            _CursorX = createdMessageData[4];
            _CursorY = createdMessageData[5];
            _BackgroundColor = new TerminalColor(createdMessageData[6], createdMessageData[7]);
            _ForegroundColor = new TerminalColor(createdMessageData[8], createdMessageData[9]);
            socket.AddMessageHandler(VTProtocol.HB_BUFFERUPDATED, HandleBufferUpdatedAsync);
        }

        private IVTSocket _Socket;
        private ushort _BufferId;
        private ushort _BufferWidth;
        private ushort _BufferHeight;
        private ushort _CursorX;
        private ushort _CursorY;
        private TerminalColor _BackgroundColor;
        private TerminalColor _ForegroundColor;
        private List<IBufferCommand> _CommandQueue = new List<IBufferCommand>();
        private object _QueueLock = new object();

        private void QueueBufferCommand(IBufferCommand command)
        {
            lock (_QueueLock)
            {
                if (_CommandQueue.Count > 0)
                {
                    var lastCommand = _CommandQueue[_CommandQueue.Count - 1];
                    var coalesceResult = command.TryCoalesce(lastCommand);
                    if (coalesceResult == CoalesceResult.MergedReplaceExisting)
                    {
                        _CommandQueue[_CommandQueue.Count - 1] = command;
                        return;
                    }
                    else if (coalesceResult == CoalesceResult.MergedDisposeNew)
                        return;
                }
                _CommandQueue.Add(command);
            }
        }

        private async Task FlushCommandsAsync()
        {
            List<IBufferCommand> commands;
            lock (_QueueLock)
            {
                commands = _CommandQueue;
                _CommandQueue = new List<IBufferCommand>();
            }
            foreach (var command in commands)
                await command.SendRequestAsync(_Socket);
        }

        public async Task<int> GetHeightAsync()
        {
            await FlushCommandsAsync();
            return _BufferHeight;
        }

        public async Task<(int x, int y)> GetCursorPosAsync()
        {
            await FlushCommandsAsync();
            return (_CursorX, _CursorY);
        }

        public Task SetCursorPosAsync(int x, int y)
        {
            throw new NotImplementedException();
        }

        public Task SetForegroundColorAsync(TerminalColor color)
        {
            throw new NotImplementedException();
        }

        public Task SetBackgroundColorAsync(TerminalColor color)
        {
            throw new NotImplementedException();
        }

        public Task<TerminalColor> GetForegroundColorAsync()
        {
            throw new NotImplementedException();
        }

        public Task<TerminalColor> GetBackgroundColorAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IBufferViewport> CreateViewportAsync(int offsetX, int offsetY)
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(string text)
        {
            throw new NotImplementedException();
        }

        public Task LineFeedAsync(int count)
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        private Task HandleBufferUpdatedAsync(ArraySegment<ushort> messageData)
        {
            if (messageData[0] != _BufferId)
                return Task.CompletedTask;
            var changeFlags = (VTProtocol.BufferChangeFlags)messageData[1];
            var i = 2;
            if (changeFlags.HasFlag(VTProtocol.BufferChangeFlags.BufferSize))
            {
                _BufferWidth = messageData[i++];
                _BufferHeight = messageData[i++];
            }
            if (changeFlags.HasFlag(VTProtocol.BufferChangeFlags.CursorPos))
            {
                _CursorX = messageData[i++];
                _CursorY = messageData[i++];
            }
            if (changeFlags.HasFlag(VTProtocol.BufferChangeFlags.BackgroundColor))
            {
                var rg = messageData[i++];
                var ba = messageData[i++];
                _BackgroundColor = new TerminalColor(rg, ba);
            }
            if (changeFlags.HasFlag(VTProtocol.BufferChangeFlags.ForegroundColor))
            {
                var rg = messageData[i++];
                var ba = messageData[i++];
                _ForegroundColor = new TerminalColor(rg, ba);
            }
            return Task.CompletedTask;
        }

        private interface IBufferCommand
        {
            public Task SendRequestAsync(IVTSocket socket);
            public CoalesceResult TryCoalesce(IBufferCommand existingCommand);
        }

        private enum CoalesceResult
        {
            Unmerged,
            MergedReplaceExisting,
            MergedDisposeNew
        }

        
    }
}
