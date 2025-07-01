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

        public IWebContent Content => _Content;
        public (int w, int h) WindowSize => throw new NotImplementedException();

        public VirtualTerminal()
        {
            _Content.AddManifestSources("/", Assembly.GetExecutingAssembly(), "shellil.VirtualTerminal.web");
            _Content.SetIndex();
        }

        private WebContent _Content = new WebContent();
        private (int w, int h) _WindowSize;
        private object _WindowSizeLock = new object();
        private bool _FrontendReady = false;

        

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
                
            });
        }

        public IVirtualTerminalBuffer CreateBuffer(int cols)
        {
            throw new NotImplementedException();
        }

        private async Task<(int w, int h)> GetWindowSize(IAppWindow window)
        {
            await using (var viewportSizeJS = (IJSObject)await window.EvaluateJSExpressionAsync("vtcanvas.rendering.getViewportSize();"))
            await using (var wGetter = await viewportSizeJS.BindGetterAsync("w"))
            await using (var hGetter = await viewportSizeJS.BindGetterAsync("h"))
            {
                int w = (int)Math.Round(((IJSNumber)await wGetter.GetValueAsync()).Value);
                int h = (int)Math.Round(((IJSNumber)await wGetter.GetValueAsync()).Value);
                return (w, h);
            }
        }
    }
}
