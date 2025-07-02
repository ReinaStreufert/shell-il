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
        public (int w, int h) WindowSize
        {
            get
            {
                lock (_WindowSizeLock)
                    return _WindowSize;
            }
        }

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
            var documentBody = await window.GetDocumentBodyAsync();
            var vtElement = await documentBody.QuerySelectAsync("#vt");
            await vtElement.AddEventListenerAsync(Event.Resize, () => _ = HandleResizeAsync(window));
            await vtElement.AddEventListenerAsync(Event.KeyDown, e => _ = HandleKeyDownAsync(e));
            await vtElement.AddEventListenerAsync(Event.Wheel, e => _ = HandleWheelAsync(e, window));
            await vtElement.AddEventListenerAsync(Event.MouseMove, e => _ = HandleMouseEventAsync(e, TerminalMouseEventType.MouseMove, window));
            await vtElement.AddEventListenerAsync(Event.MouseDown, e => _ = HandleMouseEventAsync(e, TerminalMouseEventType.MouseDown, window));
            await vtElement.AddEventListenerAsync(Event.MouseUp, e => _ = HandleMouseEventAsync(e, TerminalMouseEventType.MouseUp, window));
            await _Client.OnReadyAsync(new VirtualTerminalContext(window, this));
        }

        private async Task HandleResizeAsync(IAppWindow window)
        {
            var oldSize = _WindowSize;
            var updatedSize = await getWindowSize(window);
            if (updatedSize.w != oldSize.w || updatedSize.h != oldSize.h)
            {
                lock (_WindowSizeLock)
                    _WindowSize = updatedSize;
                await _Client.OnViewportResizeAsync(updatedSize.w, updatedSize.h);
            }
        }

        private async Task HandleKeyDownAsync(KeyboardEventArgs e)
        {
            var key = e.Key;
            var modifiers = e.Modifiers;
            if (key == null)
                return;
            if (key.Length == 1)
                await _Client.OnInputCharAsync(key[0], modifiers);
            else if (Enum.TryParse<TerminalSpecialKey>(key, out var specialKey))
                await _Client.OnSpecialKeyAsync(specialKey, modifiers);
        }

        private async Task HandleWheelAsync(WheelEventArgs e, IAppWindow window)
        {
            var deltaX = e.DeltaX;
            var deltaY = e.DeltaY;
            double scrollX;
            double scrollY;
            if (e.Mode == WheelDeltaMode.Lines)
            {
                scrollX = deltaX;
                scrollY = deltaY;
            }
            else if (e.Mode == WheelDeltaMode.Pixels)
            {
                await using (var viewportOffsetJs = (IJSObject)await window.EvaluateJSExpressionAsync($"vtcanvas.rendering.viewportPosFromClientPos({deltaX},{deltaY});"))
                await using (var binding = await viewportOffsetJs.BindAsync())
                {
                    scrollX = ((IJSValue<double>)await binding.GetAsync("x")).Value;
                    scrollY = ((IJSValue<double>)await binding.GetAsync("y")).Value;
                }
            }
            else if (e.Mode == WheelDeltaMode.Pages)
            {
                var viewportSize = WindowSize;
                scrollX = deltaX * viewportSize.w;
                scrollY = deltaY * viewportSize.h;
            }
            else throw new NotImplementedException();
            _WheelDeltaXBuild += scrollX;
            _WheelDeltaYBuild += scrollY;
            if (_WheelDeltaXBuild > 1 || _WheelDeltaYBuild > 1)
            {
                var viewOffsetX = (int)Math.Floor(_WheelDeltaXBuild);
                var viewOffsetY = (int)Math.Floor(_WheelDeltaYBuild);
                _WheelDeltaXBuild -= viewOffsetX;
                _WheelDeltaYBuild -= viewOffsetY;
                await _Client.OnUserScrollAsync(viewOffsetX, viewOffsetY);
            }
        }

        private async Task HandleMouseEventAsync(MouseEventArgs e, TerminalMouseEventType type, IAppWindow window)
        {
            int posX;
            int posY;
            await using (var viewportPosJs = (IJSObject)await window.EvaluateJSExpressionAsync($"vtcanvas.rendering.viewportPosFromClientPos({e.ClientX},{e.ClientY});"))
            await using (var binding = await viewportPosJs.BindAsync())
            {
                posX = (int)Math.Floor(((IJSValue<double>)await binding.GetAsync("x")).Value);
                posY = (int)Math.Floor(((IJSValue<double>)await binding.GetAsync("y")).Value);
            }
            await _Client.OnMouseEventAsync(type, posX, posY);
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
