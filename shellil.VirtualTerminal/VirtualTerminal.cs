using LibChromeDotNet;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5;
using LibChromeDotNet.HTML5.DOM;
using LibChromeDotNet.HTML5.JS;
using Newtonsoft.Json.Linq;
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
            await using (var setInteropDispatcherFunc = (IJSFunction)await window.EvaluateJSExpressionAsync("vtcanvas.setInteropDispatcher"))
            await using (var jsInteropHandler = await window.AddJSBindingAsync(async obj => await HandleDispatchMessage(obj)))
                await setInteropDispatcherFunc.CallAsync(jsInteropHandler);
            await _Client.OnReadyAsync(new VirtualTerminalContext(window, this));
        }

        private async Task HandleDispatchMessage(JObject messageData)
        {
            var eventName = messageData["event"]!.ToString();
            if (eventName == "ViewportResize")
            {
                int width = (int)messageData["w"]!;
                int height = (int)messageData["h"]!;
                await _Client.OnViewportResizeAsync(width, height);
            } else if (eventName == "InputChar")
            {
                var c = messageData["inputChar"]!.ToString()[0];
                var modifiers = (TerminalModifierKeys)((int)messageData!["modifiers"]!);
                await _Client.OnInputCharAsync(c, modifiers);
            } else if (eventName == "SpecialKey")
            {
                var specialKey = (TerminalSpecialKey)((int)messageData["specialKeyCode"]!);
                var modifiers = (TerminalModifierKeys)((int)messageData["modifiers"]!);
                await _Client.OnSpecialKeyAsync(specialKey, modifiers);
            } else if (eventName == "UserScroll")
            {
                var offsetX = (int)messageData["offsetX"]!;
                var offsetY = (int)messageData["offsetY"]!;
                await _Client.OnUserScrollAsync(offsetX, offsetY);
            } else if (eventName == "MouseEvent")
            {
                var mouseEventType = (TerminalMouseEventType)((int)messageData["type"]!);
                var x = (int)messageData["x"]!;
                var y = (int)messageData["y"]!;
                await _Client.OnMouseEventAsync(mouseEventType, x, y);
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
