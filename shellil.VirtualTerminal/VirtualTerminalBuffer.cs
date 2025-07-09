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

        public async Task<TerminalPosition> GetCursorPosAsync()
        {
            await FlushCommandsAsync();
            return new TerminalPosition(_CursorX, _CursorY);
        }

        public Task SetCursorPosAsync(int x, int y) => SetCursorPosAsync(new TerminalPosition((ushort)x, (ushort)y));

        public Task SetCursorPosAsync(TerminalPosition position)
        {
            var attrCommand = new BufferAttributeCommand(this);
            attrCommand.Seek = position;
            QueueBufferCommand(attrCommand);
            return Task.CompletedTask;
        }

        public Task SetForegroundColorAsync(TerminalColor color)
        {
            var attrCommand = new BufferAttributeCommand(this);
            attrCommand.NewForegroundColor = color;
            QueueBufferCommand(attrCommand);
            return Task.CompletedTask;
        }

        public Task SetBackgroundColorAsync(TerminalColor color)
        {
            var attrCommand = new BufferAttributeCommand(this);
            attrCommand.NewBackgroundColor = color;
            QueueBufferCommand(attrCommand);
            return Task.CompletedTask;
        }

        public Task LineFeedAsync(int count)
        {
            if (count == 0)
                return Task.CompletedTask;
            var attrCommand = new BufferAttributeCommand(this);
            attrCommand.LineFeed = count;
            QueueBufferCommand(attrCommand);
            return Task.CompletedTask;
        }

        public Task WriteAsync(string text)
        {
            if (text.Length == 0)
                return Task.CompletedTask;
            var writeCommand = new BufferWriteCommand(this);
            writeCommand.Write(text);
            QueueBufferCommand(writeCommand);
            return Task.CompletedTask;
        }

        public async Task<TerminalColor> GetForegroundColorAsync()
        {
            await FlushCommandsAsync();
            return _ForegroundColor;
        }

        public async Task<TerminalColor> GetBackgroundColorAsync()
        {
            await FlushCommandsAsync();
            return _BackgroundColor;
        }

        public Task<IBufferViewport> CreateViewportAsync(int offsetX, int offsetY)
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
            var changeFlags = (VTProtocol.BufferUpdateFlags)messageData[1];
            var i = 2;
            if (changeFlags.HasFlag(VTProtocol.BufferUpdateFlags.BufferSize))
            {
                _BufferWidth = messageData[i++];
                _BufferHeight = messageData[i++];
            }
            if (changeFlags.HasFlag(VTProtocol.BufferUpdateFlags.CursorPos))
            {
                _CursorX = messageData[i++];
                _CursorY = messageData[i++];
            }
            if (changeFlags.HasFlag(VTProtocol.BufferUpdateFlags.BackgroundColor))
            {
                var rg = messageData[i++];
                var ba = messageData[i++];
                _BackgroundColor = new TerminalColor(rg, ba);
            }
            if (changeFlags.HasFlag(VTProtocol.BufferUpdateFlags.ForegroundColor))
            {
                var rg = messageData[i++];
                var ba = messageData[i++];
                _ForegroundColor = new TerminalColor(rg, ba);
            }
            return Task.CompletedTask;
        }

        public Task<IBufferViewport> CreateViewportAsync(TerminalPosition scrollOffset)
        {
            throw new NotImplementedException();
        }

        private interface IBufferCommand
        {
            public Task SendRequestAsync(IVTSocket socket);
            public CoalesceResult TryCoalesce(IBufferCommand existingCommand);
        }

        private enum CoalesceResult
        {
            NotMerged,
            MergedReplaceExisting,
            MergedDisposeNew
        }

        private class BufferAttributeCommand : IBufferCommand
        {
            public TerminalPosition? Seek { get; set; }
            public TerminalColor? NewBackgroundColor { get; set; }
            public TerminalColor? NewForegroundColor { get; set; }
            public int LineFeed { get; set; } = 0;

            public BufferAttributeCommand(VirtualTerminalBuffer buffer)
            {
                _VTBuffer = buffer;
            }

            private VirtualTerminalBuffer _VTBuffer;

            public async Task SendRequestAsync(IVTSocket socket)
            {
                var requestId = socket.NewRequestId();
                var tcs = new TaskCompletionSource();
                var reqProcessedHandler = socket.AddMessageHandler(VTProtocol.HB_REQUESTPROCESSED, messageData =>
                {
                    if (messageData[0] == requestId)
                        tcs.SetResult();
                    return Task.CompletedTask;
                });
                var attributeMessage = new ushort[GetRequestLength()];
                attributeMessage[0] = VTProtocol.CB_SETBUFFERATTR;
                attributeMessage[1] = requestId;
                attributeMessage[2] = _VTBuffer._BufferId;
                attributeMessage[3] = (ushort)GetModifyFlags();
                var i = 4;
                if (Seek != null)
                {
                    attributeMessage[i++] = Seek.X;
                    attributeMessage[i++] = Seek.Y;
                }
                if (NewBackgroundColor != null)
                    i = NewBackgroundColor.Encode(attributeMessage, i);
                if (NewForegroundColor != null)
                    i = NewForegroundColor.Encode(attributeMessage, i);
                if (LineFeed != 0)
                    attributeMessage[i++] = (ushort)LineFeed;
                await socket.SendMessageAsync(attributeMessage);
                await tcs.Task;
                socket.RemoveMessageHandler(reqProcessedHandler);
            }

            public CoalesceResult TryCoalesce(IBufferCommand existingCommand)
            {
                if (existingCommand is BufferAttributeCommand existingAttrCommand)
                {
                    if (Seek != null)
                        existingAttrCommand.Seek = Seek;
                    if (NewBackgroundColor != null)
                        existingAttrCommand.NewBackgroundColor = NewBackgroundColor;
                    if (NewForegroundColor != null)
                        existingAttrCommand.NewForegroundColor = NewForegroundColor;
                    existingAttrCommand.LineFeed += LineFeed;
                    return CoalesceResult.MergedDisposeNew;
                } else if (LineFeed == 0 && existingCommand is IBufferWriter writerCommand)
                {
                    if (Seek != null)
                        writerCommand.SetCursorPosition(Seek);
                    if (NewBackgroundColor != null)
                        writerCommand.SetBackgroundColor(NewBackgroundColor);
                    if (NewForegroundColor != null)
                        writerCommand.SetForegroundColor(NewForegroundColor);
                    return CoalesceResult.MergedDisposeNew;
                }
                return CoalesceResult.NotMerged;
            }

            private VTProtocol.BufferModifyFlags GetModifyFlags()
            {
                var flags = VTProtocol.BufferModifyFlags.None;
                if (Seek != null)
                    flags |= VTProtocol.BufferModifyFlags.SetCursorPos;
                if (NewBackgroundColor != null)
                    flags |= VTProtocol.BufferModifyFlags.SetBackgroundColor;
                if (NewForegroundColor != null)
                    flags |= VTProtocol.BufferModifyFlags.SetForegroundColor;
                if (LineFeed != 0)
                    flags |= VTProtocol.BufferModifyFlags.ApplyLineFeed;
                return flags;
            }

            private int GetRequestLength()
            {
                var len = 4;
                if (Seek != null)
                    len += 2;
                if (NewBackgroundColor != null)
                    len += 2;
                if (NewForegroundColor != null)
                    len += 2;
                if (LineFeed != 0)
                    len += 1;
                return len;
            }
        }

        private class BufferWriteCommand : IBufferCommand, IBufferWriter
        {
            public BufferWriteCommand(VirtualTerminalBuffer buffer)
            {
                _VTBuffer = buffer; ;
            }

            private List<Action<IBufferWriter>> _EncoderQueue = new List<Action<IBufferWriter>>();
            private VirtualTerminalBuffer _VTBuffer;

            public async Task SendRequestAsync(IVTSocket socket)
            {
                var requestId = socket.NewRequestId();
                var encoder = new BufferWriteEncoder();
                foreach (var action in _EncoderQueue)
                    action(encoder);
                var ms = new MemoryStream16();
                ms.Write(VTProtocol.CB_WRITEBUFFER);
                ms.Write(requestId);
                ms.Write(_VTBuffer._BufferId);
                encoder.Encode(ms);
                var writeBufferMessage = ms.ToArray();
                var tcs = new TaskCompletionSource();
                var reqProcessedHandler = socket.AddMessageHandler(VTProtocol.HB_REQUESTPROCESSED, messageData =>
                {
                    if (messageData[0] == requestId)
                        tcs.SetResult();
                    return Task.CompletedTask;
                });
                await socket.SendMessageAsync(writeBufferMessage);
                await tcs.Task;
                socket.RemoveMessageHandler(reqProcessedHandler);
            }

            public CoalesceResult TryCoalesce(IBufferCommand existingCommand)
            {
                if (existingCommand is BufferWriteCommand existingWriteCommand)
                {
                    existingWriteCommand._EncoderQueue.AddRange(_EncoderQueue);
                    return CoalesceResult.MergedDisposeNew;
                }
                // check whether the existing command can be prepended
                var prependedWriteCommand = new BufferWriteCommand(_VTBuffer);
                if (existingCommand.TryCoalesce(prependedWriteCommand) == CoalesceResult.MergedDisposeNew) 
                {
                    var newEncoderQueue = prependedWriteCommand._EncoderQueue;
                    newEncoderQueue.AddRange(_EncoderQueue);
                    _EncoderQueue = newEncoderQueue;
                    return CoalesceResult.MergedReplaceExisting;
                }
                return CoalesceResult.NotMerged;
            }

            public void SetBackgroundColor(TerminalColor color)
            {
                _EncoderQueue.Add(w => w.SetBackgroundColor(color));
            }

            public void SetCursorPosition(TerminalPosition position)
            {
                _EncoderQueue.Add(w => w.SetCursorPosition(position));
            }

            public void SetForegroundColor(TerminalColor color)
            {
                _EncoderQueue.Add(w => w.SetForegroundColor(color));
            }

            public void Write(string s)
            {
                _EncoderQueue.Add(w => w.Write(s));
            }

            public void Write(char c)
            {
                _EncoderQueue.Add(w => w.Write(c));
            }
        }
    }
}
