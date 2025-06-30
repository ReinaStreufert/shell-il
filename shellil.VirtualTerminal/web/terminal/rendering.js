(function () {
    let consts = vtcanvas.constants;
    let rendering = {};
    vtcanvas.rendering = rendering;

    let metrics = {
        monospace: null
    };

    let buffers = {
        activeBuffer: null,
        lastPresentedBuffer: null
    };

    rendering.setActiveViewport = function (buf) {
        buffers.activeBuffer(buf);
    }

    rendering.getViewportSize = function () {
        let canv = vtcanvas.element;
        let lastViewportSize = metrics.lastViewportSize;
        let monospace = metrics.monospace;
        return {
            w: Math.floor(canv.width / monospace.w),
            h: Math.floor(canv.height / monospace.h)
        }
    };

    vtcanvas.vtdraw = function () {
        let canv = vtcanvas.element;
        let ctx = vtcanvas.context;
        
        ctx.textBaseline = "top";
        ctx.font = consts.font;
        ctx.fontKerning = "none";
        if (metrics.monospace == null) {
            metrics.monospace = ((function () {
                let precision = 200;
                let monospaceMetric = ctx.measureText(" ".repeat(precision));
                return {
                    w: (monospaceMetric.width) / precision,
                    h: (Math.abs(monospaceMetric.fontBoundingBoxAscent) + Math.abs(monospaceMetric.fontBoundingBoxDescent))
                };
            }))();
        }


    };
});