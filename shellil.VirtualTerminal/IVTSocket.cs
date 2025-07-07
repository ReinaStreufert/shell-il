using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public interface IVTSocket
    {
        public event Action? SocketClosed;
        public void AddMessageHandler(IVTMessageHandler handler);
        public void RemoveMessageHandler(IVTMessageHandler handler);
        public Task SendMessageAsync(ushort[] message);
        public ushort NewRequestId();
    }

    public interface IVTMessageHandler
    {
        public ushort MessageId { get; }
        public Task HandleAsync(ArraySegment<ushort> messageBody);
    }
}
