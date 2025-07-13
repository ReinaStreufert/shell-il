using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    internal class DebugTerminalService : IVirtualTerminalService
    {
        public Task<IVirtualTerminalDriver> AttachDriverAsync()
        {
            return Task.FromResult<IVirtualTerminalDriver>(new DebugTerminalDriver("pemdas.nlisp"));
        }

        public Task DetachDriverAsync(IVirtualTerminalDriver driver)
        {
            return Task.CompletedTask;
        }
    }
}
