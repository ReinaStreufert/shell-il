using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class VTMessageHandler : IVTMessageHandler
    {
        public ushort MessageId => _MessageId;

        public VTMessageHandler(ushort messageId, Func<ArraySegment<ushort>, Task> handlerFunc)
        {
            _MessageId = messageId;
            _HandlerFunc = handlerFunc;
        }

        private ushort _MessageId;
        private Func<ArraySegment<ushort>, Task> _HandlerFunc;

        public async Task HandleAsync(ArraySegment<ushort> messageBody)
        {
            await _HandlerFunc(messageBody);
        }
    }

    public static class VTSocketExtensions
    {
        public static IVTMessageHandler AddMessageHandler(this IVTSocket socket, ushort messageType, Func<ArraySegment<ushort>, Task> handlerFunc)
        {
            var handler = new VTMessageHandler(messageType, handlerFunc);
            socket.AddMessageHandler(handler);
            return handler;
        }
    }
}
