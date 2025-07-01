using LibChromeDotNet;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5;
using LibChromeDotNet.HTML5.DOM;
using LibChromeDotNet.HTML5.JS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class VirtualTerminal : IWebApp, IVirtualTerminal
    {
        public event Action<(int w, int h)>? OnResize;
        public event Action<IVirtualTerminalContext>? OnReady;
        public event Action<char>? OnInputChar;
        public event Action<TerminalSpecialKey>? OnSpecialKey;

        public IWebContent Content => _Content;
        public (int w, int h) WindowSize
        {
            get
            {
                lock (_WindowSizeLock)
                    return _WindowSize;
            }
        }

        public VirtualTerminal()
        {
            _Content.AddManifestSources("/", Assembly.GetExecutingAssembly(), "shellil.VirtualTerminal.web");
            _Content.SetIndex();
        }

        private WebContent _Content = new WebContent();
        private (int w, int h) _WindowSize;
        private object _WindowSizeLock = new object();

        public async Task OnStartupAsync(IAppContext context)
        {
            var window = await context.OpenWindowAsync();
            var frontendReady = await window.AddJSAwaitableBindingAsync();
            await using (var setInteropInitFunc = (IJSFunction)await window.EvaluateJSExpressionAsync("vtcanvas.setInteropInit"))
                await setInteropInitFunc.CallAsync(frontendReady.CompletionSignal);
            await frontendReady.Task;
            var documentBody = await window.GetDocumentBodyAsync();
            await documentBody.AddEventListenerAsync(Event.Resize, async () =>
            {
                var oldSize = _WindowSize;
                var updatedSize = await getWindowSize(window);
                if (updatedSize.w != oldSize.w || updatedSize.h != oldSize.h)
                {
                    lock (_WindowSizeLock)
                        _WindowSize = updatedSize;
                    OnResize?.Invoke(updatedSize);
                }
            });
            await documentBody.AddEventListenerAsync(Event.KeyDown, e =>
            {
                var key = e.Key;
                if (key == null)
                    return;
                if (key.Length == 1)
                    OnInputChar?.Invoke(key[0]);
                else if (Enum.TryParse<TerminalSpecialKey>(key, out var specialKey))
                    OnSpecialKey?.Invoke(specialKey);
            });
            
            OnReady?.Invoke(new VirtualTerminalContext(window, this));
        }

        private async Task<(int w, int h)> getWindowSize(IAppWindow window)
        {
            await using (var viewportSizeJS = (IJSObject)await window.EvaluateJSExpressionAsync("vtcanvas.rendering.getViewportSize();"))
            await using (var binding = await viewportSizeJS.BindAsync())
            {
                int w = (int)Math.Round(((IJSNumber)await binding.GetAsync("w")).Value);
                int h = (int)Math.Round(((IJSNumber)await binding.GetAsync("h")).Value);
                return (w, h);
            }
        }

        private class VirtualTerminalContext : IVirtualTerminalContext
        {
            public VirtualTerminalContext(IAppWindow window, IVirtualTerminal terminal)
            {
                _Window = window;
                _Terminal = terminal;
            }

            private IVirtualTerminal _Terminal;
            private IAppWindow _Window;

            public async Task<IVirtualTerminalBuffer> CreateBufferAsync(int cols)
            {
                await using (var jsBuffer = (IJSObject)await _Window.EvaluateJSExpressionAsync($"createTerminalBuffer({cols})"))
                    return new VirtualTerminalBuffer(await jsBuffer.BindAsync(), cols, _Terminal);
            }
        }
    }
}
