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
            var client = new DebugTerminalDriver("pemdas.nlisp");
            var terminal = new VirtualTerminalClient(client);
            await terminal.LaunchAsync();
        }
    }
}
