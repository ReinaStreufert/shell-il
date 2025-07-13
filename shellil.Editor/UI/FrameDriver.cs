using shellil.VirtualTerminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor.UI
{
    public class FrameDriver : IVirtualTerminalDriver, IFrameDriver
    {
        public FrameDriver(ITerminalUIComponent[] compositeChain)
        {
            _CompositeChain = compositeChain;
        }

        private IVirtualTerminalContext? _TerminalContext;
        private ITerminalUIComponent[] _CompositeChain;
        private List<InputRegion>? _InputRegions;
        private int _ViewportWidth;
        private int _ViewportHeight;
        private IFocusable? _Focused;
        private long _LastValidatedTime = 0;
        private long _LastInvalidatedTime = 0;
        private object _FrameSync = new object();
        private Task? _ValidateTask = null;

        public async Task OnReadyAsync(IVirtualTerminalContext ctx)
        {
            _TerminalContext = ctx;
            await ValidateAsync();
        }

        public async Task OnMouseEventAsync(TerminalMouseEventType type, int x, int y)
        {
            List<InputRegion>? inputRegions;
            lock (_FrameSync)
                inputRegions = _InputRegions;
            if (inputRegions == null)
                return;
            InputRegion? hitRegion = null;
            for (int i = inputRegions.Count - 1; i >= 0; i--)
            {
                var region = inputRegions[i];
                if (region.Contains(new TerminalPosition(x, y)))
                {
                    hitRegion = region;
                    break;
                }
            }
            if (hitRegion == null)
                return;
            if (type == TerminalMouseEventType.MouseDown && hitRegion.Handler.CanFocus)
            {
                lock (_FrameSync)
                    _Focused = hitRegion.Handler;
                hitRegion.Handler.OnFocused();
            }
            await hitRegion.Handler.OnMouseEventAsync(type, x - hitRegion.Position.X, y - hitRegion.Position.Y);
            EnsureValidated();
        }

        public async Task OnInputCharAsync(char inputChar, TerminalModifierKeys modifiers)
        {
            IFocusable? focused;
            lock (_FrameSync)
                focused = _Focused;
            if (focused != null)
            {
                await focused.OnInputCharAsync(inputChar, modifiers);
                EnsureValidated();
            }
        }

        public async Task OnSpecialKeyAsync(TerminalSpecialKey key, TerminalModifierKeys modifiers)
        {
            IFocusable? focused;
            lock (_FrameSync)
                focused = _Focused;
            if (focused != null)
            {
                await focused.OnSpecialKeyAsync(key, modifiers);
                EnsureValidated();
            }
        }

        public async Task OnUserScrollAsync(int deltaX, int deltaY)
        {
            IFocusable? focused;
            lock (_FrameSync)
                focused = _Focused;
            if (focused != null)
            {
                await focused.OnUserScrollAsync(deltaX, deltaY);
                EnsureValidated();
            }
        }

        public Task OnViewportResizeAsync(int widthCols, int heightRows)
        {
            lock (_FrameSync)
            {
                _ViewportWidth = widthCols;
                _ViewportHeight = heightRows;
                Invalidate();
            }
            EnsureValidated();
            return Task.CompletedTask;
        }

        public void Invalidate()
        {
            InterlockedMax(ref _LastInvalidatedTime, DateTime.Now.Ticks);
        }

        public void EnsureValidated()
        {
            lock (_FrameSync)
            {
                if (_ValidateTask == null && _LastInvalidatedTime >= _LastValidatedTime)
                    _ValidateTask = ValidateAsync();
            }
        }

        private async Task ValidateAsync()
        {
            if (_TerminalContext == null)
                return;
            for (; ;)
            {
                int viewWidth;
                int viewHeight;
                IFocusable? focused;
                long frameTime;
                lock (_FrameSync)
                {
                    frameTime = DateTime.Now.Ticks;
                    viewWidth = _ViewportWidth;
                    viewHeight = _ViewportHeight;
                    focused = _Focused;
                }
                List<InputRegion> regions = new List<InputRegion>();
                await using (var buffer = await _TerminalContext.CreateBufferAsync(viewWidth))
                await using (var viewport = await buffer.CreateViewportAsync(0, 0))
                {
                    var renderContext = new FrameRenderContext(viewWidth, viewHeight, this, buffer, regions);
                    for (int i = 0; i < _CompositeChain.Length; i++)
                        _CompositeChain[i].Render(renderContext);
                    if (focused == null)
                    {
                        focused = regions
                            .Select(f => f.Handler)
                            .Where(h => h.CanFocus)
                            .FirstOrDefault();
                        focused?.OnFocused();
                    }
                    if (focused != null)
                    {
                        await buffer.SetCursorPosAsync(focused.ShownCursorPosition);
                        await viewport.SetCursorStateAsync(focused.ShownCursorState);
                    }
                    else await viewport.SetCursorStateAsync(TerminalCursorState.Invisible);
                    await viewport.PresentAsync();
                }
                lock (_FrameSync)
                {
                    if (_Focused == null)
                        _Focused = focused;
                    _InputRegions = regions;
                    _LastValidatedTime = frameTime;
                    if (_LastInvalidatedTime < frameTime)
                    {
                        _ValidateTask = null;
                        return;
                    }
                }
            }
        }

        private static void InterlockedMax(ref long time, long currentTime)
        {
            for (; ;)
            {
                var lastTime = time;
                if (lastTime >= currentTime || Interlocked.CompareExchange(ref time, currentTime, lastTime) == lastTime)
                    break;
            }
        }

        private class FrameRenderContext : IFrameRenderContext, IBufferWriter
        {
            public IFrameDriver Driver => _Driver;
            public IBufferWriter FrameBuffer => this;
            public int FrameWidth => _FrameWidth;
            public int FrameHeight => _FrameHeight;

            public FrameRenderContext(int frameWidth, int frameHeight, FrameDriver driver, IVirtualTerminalBuffer buffer, List<InputRegion> inputRegions)
            {
                _FrameWidth = frameWidth;
                _FrameHeight = frameHeight;
                _Driver = driver;
                _Buffer = buffer;
                _InputRegions = inputRegions;
            }

            private int _FrameWidth;
            private int _FrameHeight;
            private FrameDriver _Driver;
            private IVirtualTerminalBuffer _Buffer;
            private List<InputRegion> _InputRegions;

            public void ClaimInputRegion(TerminalPosition position, TerminalPosition size, IFocusable handler)
            {
                _InputRegions.Add(new InputRegion(position, size, handler));
            }

            public async void SetCursorPosition(TerminalPosition position)
            {
                await _Buffer.SetCursorPosAsync(position);
            }

            public async void SetBackgroundColor(TerminalColor color)
            {
                await _Buffer.SetBackgroundColorAsync(color);
            }

            public async void SetForegroundColor(TerminalColor color)
            {
                await _Buffer.SetForegroundColorAsync(color);
            }

            public async void Write(string s)
            {
                await _Buffer.WriteAsync(s);
            }

            public async void Write(char c)
            {
                await _Buffer.WriteAsync(c.ToString());
            }
        }

        private class InputRegion
        {
            public TerminalPosition Position { get; }
            public TerminalPosition Size { get; }
            public IFocusable Handler { get; }

            public InputRegion(TerminalPosition position, TerminalPosition size, IFocusable handler)
            {
                Position = position;
                Size = size;
                Handler = handler;
            }

            public bool Contains(TerminalPosition point)
            {
                return (point.X >= Position.X && point.X < Position.X + Size.X &&
                    point.Y >= Position.Y && point.Y < Position.Y + Size.Y);
            }
        }
    }
}
