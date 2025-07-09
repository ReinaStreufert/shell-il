using LibChromeDotNet;
using LibChromeDotNet.HTML5.DOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    // all of this stuff is just for testing both the frontend and backend of the library and will all be deleted later.
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string hostUrl = "http://localhost:54321/";
            var host = new VirtualTerminalHost(new DebugTerminalService());
            var listenerTask = host.ListenAsync(new string[] { hostUrl }, CancellationToken.None);
            var terminal = new VirtualTerminalClient(hostUrl);
            _ = terminal.LaunchAsync();
            await listenerTask;
        }
    }
}
