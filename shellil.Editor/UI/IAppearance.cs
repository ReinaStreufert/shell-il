using shellil.VirtualTerminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor.UI
{
    public interface IAppearance
    {
        public TerminalColor SelectedTabBackground { get; }
        public TerminalColor SelectedTabForeground { get; }
        public TerminalColor TabBackground { get; }
        public TerminalColor TabForeground { get; }
        public TerminalColor TabCloseHighlight { get; }

        public TerminalColor BufferPlainTextBackground { get; }
        public TerminalColor BufferPlainTextForeground { get; }

        public TerminalColor PromptBackground { get; }
        public TerminalColor PromptCaptionForeground { get; }
        public TerminalColor PromptInputForeground { get; }
    }
}
