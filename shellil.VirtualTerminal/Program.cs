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
            /*var encoder = new BufferWriteEncoder();
            var lines = File.ReadAllLines("pemdas.nlisp");
            for (ushort i = 0; i < lines.Length; i++)
            {
                encoder.SetCursorPosition(new TerminalPosition(0, i));
                encoder.Write(lines[i]);
            }
            encoder.Encode(new MemoryStream16());*/
            string hostUrl = "http://localhost:54321/";
            var host = new VirtualTerminalHost(new DebugTerminalService());
            _ = host.ListenAsync(new string[] { hostUrl }, CancellationToken.None);
            var terminal = new VirtualTerminalClient(hostUrl);
            await terminal.LaunchAsync();
        }
    }
}
