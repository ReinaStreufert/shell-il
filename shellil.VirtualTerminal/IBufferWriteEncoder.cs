using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public interface IBufferWriter
    {
        public void SetCursorPosition(TerminalPosition position);
        public void SetBackgroundColor(TerminalColor color);
        public void SetForegroundColor(TerminalColor color);
        public void Write(string s);
        public void Write(char c);
    }

    public interface IBufferWriteEncoder : IBufferWriter
    {
        public void Encode(MemoryStream16 outputStream);
    }

    
}
