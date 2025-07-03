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
        public IWebContent Content => _Content;
        public IVirtualTerminalClient Client => _Client;

        public VirtualTerminal(IVirtualTerminalClient client)
        {
            _Client = client;
            _Content.AddManifestSources("/", Assembly.GetExecutingAssembly(), "shellil.VirtualTerminal.web");
            _Content.SetIndex();
        }

        private WebContent _Content = new WebContent();
        private IVirtualTerminalClient _Client;
        private (int w, int h) _WindowSize;
        private object _WindowSizeLock = new object();
        private double _WheelDeltaXBuild = 0d;
        private double _WheelDeltaYBuild = 0d;

        public async Task OnStartupAsync(IAppContext context)
        {
            var window = await context.OpenWindowAsync();
            var frontendReady = await window.AddJSAwaitableBindingAsync();
            await using (var setInteropInitFunc = (IJSFunction)await window.EvaluateJSExpressionAsync("vtcanvas.setInteropInit"))
                await setInteropInitFunc.CallAsync(frontendReady.CompletionSignal);
            await frontendReady.Task;

            await _Client.OnReadyAsync(new VirtualTerminalContext(window, this));
        }

        

        private async Task<(int w, int h)> getWindowSize(IAppWindow window)
        {
            await using (var viewportSizeJS = (IJSObject)await window.EvaluateJSExpressionAsync("vtcanvas.rendering.getViewportSize();"))
            await using (var binding = await viewportSizeJS.BindAsync())
            {
                int w = (int)Math.Round(((IJSValue<double>)await binding.GetAsync("w")).Value);
                int h = (int)Math.Round(((IJSValue<double>)await binding.GetAsync("h")).Value);
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

#if DEBUG
            public IAppWindow GetAppWindow() => _Window;
#endif
        }
    }
}
