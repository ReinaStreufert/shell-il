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

            }
        }
    }
}
