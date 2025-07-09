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
    public class VirtualTerminalClient : IWebApp
    {
        public IWebContent Content => _Content;

        public VirtualTerminalClient(string driverHostUrl)
        {
            _Content.AddManifestSources("/", Assembly.GetExecutingAssembly(), "shellil.VirtualTerminal.web");
            _Content.SetIndex();
            _DriverHostUrl = driverHostUrl;
        }

        private WebContent _Content = new WebContent();
        private string _DriverHostUrl;

        public async Task OnStartupAsync(IAppContext context)
        {
            var window = await context.OpenWindowAsync();
            await Task.Delay(5000);
            await using (var attachFuncJs = (IJSFunction)await window.EvaluateJSExpressionAsync("vtcanvas.remote.attachToRemoteClient"))
                await attachFuncJs.CallAsync(IJSValue.FromString(_DriverHostUrl));
        }
    }
}
