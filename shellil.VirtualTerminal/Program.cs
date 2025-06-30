using LibChromeDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await new App().LaunchAsync();
        }
    }
}
