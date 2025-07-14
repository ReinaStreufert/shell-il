using shellil.VirtualTerminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor.UI
{
    public class PromptView : ITerminalUIComponent
    {
        public PromptView(TextEditor editor)
        {
            _Editor = editor;
        }

        private TextEditor _Editor;
        private IUserPrompt? _LastUserPrompt;
        private StringBuilder _InputText = new StringBuilder();
        private int _ScrollOffset = 0;
        private int _CursorOffset = 0;

        public void Render(IFrameRenderContext context)
        {
            var userPrompt = _Editor.UserPrompt;
            if (userPrompt != _LastUserPrompt)
            {
                _InputText.Clear();
                _ScrollOffset = 0;
                _LastUserPrompt = userPrompt;
            }
            if (userPrompt == null)
                return;
            var frameBuffer = context.FrameBuffer;
            var appearance = _Editor.Appearance;
            var lastLine = context.FrameHeight - 1;
            frameBuffer.SetCursorPosition(new TerminalPosition(0, lastLine - 1));
            frameBuffer.SetBackgroundColor(appearance.PromptBackground);
            frameBuffer.SetForegroundColor(appearance.PromptCaptionForeground);
            var caption = userPrompt.Caption;
            frameBuffer.Write(caption + new string(' ', context.FrameWidth - caption.Length));
            frameBuffer.SetCursorPosition(new TerminalPosition(0, lastLine));
            frameBuffer.SetForegroundColor(appearance.PromptInputForeground);
        }


    }
}
