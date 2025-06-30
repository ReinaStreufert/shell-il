(function () {
    let consts = vtcanvas.constants;



    vtcanvas.vtload = function () {
        let canv = vtcanvas.element;
        let ctx = vtcanvas.context;
        ctx.textBaseline = "top";
        ctx.font = consts.font;
        ctx.fontKerning = "none";
        ctx.fillStyle = "#ffffff";

        let monospace = ((function () {
            let precision = 200;
            let monospaceMetric = ctx.measureText(" ".repeat(precision));
            console.log(monospaceMetric);
            return {
                w: (monospaceMetric.width) / precision,
                h: (Math.abs(monospaceMetric.fontBoundingBoxAscent) + Math.abs(monospaceMetric.fontBoundingBoxDescent))
            };
        }))();
        vtcanvas.fg = "#ffffff";
        vtcanvas.bg = "#000000";
        vtcanvas.x = 0;
        vtcanvas.y = 0;
        vtcanvas.clear = function () {
            ctx.clearRect(0, 0, canv.width, canv.height);
        };
        vtcanvas.write = function (text) {
            let x = vtcanvas.x * monospace.w;
            let y = vtcanvas.y * monospace.h;
            let len = text.length;
            ctx.fillStyle = vtcanvas.bg;
            ctx.fillRect(x, y, len * monospace.w, monospace.h);
            ctx.fillStyle = vtcanvas.fg;
            ctx.fillText(text, x, y);
            vtcanvas.x += text.length;
        };
        ctx.fillText("Hello world", 0, 0);
    }
})();