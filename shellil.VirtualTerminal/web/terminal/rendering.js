(function () {
    let consts = vtcanvas.constants;
    let rendering = {};
    vtcanvas.rendering = rendering;

    let metrics = {
        monospace: null
    };

    let snapshot = {
        active: null,
        lastPresented: null,
        lastCursorDraw: null,
        lastFrameTime: 0,
        lastPresentTime: 0
    };

    rendering.setActiveViewport = function (buf) {
        snapshot.active = buf.getSnapshot();
        snapshot.lastPresentTime = snapshot.lastFrameTime;
    }

    rendering.getViewportSize = function () {
        let canv = vtcanvas.element;
        let monospace = metrics.monospace;
        return {
            w: Math.floor(canv.width / monospace.w),
            h: Math.floor(canv.height / monospace.h)
        }
    };

    let renderText = function (contiguous) {
        let ctx = vtcanvas.context;
        let monospace = metrics.monospace;
        let x = contiguous.x * monospace.w;
        let y = contiguous.y * monospace.h;
        let len = text.length;
        ctx.fillStyle = contiguous.bg;
        ctx.fillRect(x, y, len * monospace.w, monospace.h);
        ctx.fillStyle = contiguous.fg;
        ctx.fillText(contiguous.text, x, y);
    }

    let renderCursor = function (cursorX, cursorY) {
        let ctx = vtcanvas.context;
        let monospace = metrics.monospace;
        let x = cursorX * monospace.w;
        let y = cursorY * monospace.h;
        let contrastHigh = Math.round((0.5 + (consts.cursorContrast / 2)) * 255);
        let contrastLow = Math.round((0.5 - (consts.cursorContrast / 2)) * 255);
        let bgw = consts.cursorWidth;
        let fgw = consts.cursorWidth / 2;
        ctx.fillStyle = `rgb(${contrastLow} ${contrastLow} ${contrastLow})`;
        ctx.fillRect(x, y, bgw, monospace.h);
        ctx.fillStyle = `rgb(${contrastHigh} ${contrashHigh} ${contrastHigh})`;
        ctx.fillRect(x, y, fgw, mmonospace.h);
    }

    let updateCursor = function (uptime) {
        let activeSnapshot = snapshot.active;
        if (activeSnapshot == null)
            return;
        let isCursorVisible;
        if (activeSnapshot.cursorState == "invisible")
            isCursorVisible = false;
        else if (activeSnapshot.cursorState == "solid")
            isCursorVisible = true;
        else if (active.cursorState == "blinking") {
            let presentUptime = uptime - snapshot.lastPresentTime;
            isCursorVisible = presentUptime % (consts.blinkInterval * 2) < consts.blinkInterval;
        }
        let lastCursorDraw = snapshot.lastCursorDraw;
        if (lastCursorDraw != null) {
            if (isCursorVisible && lastCursorDraw.x == activeSnapshot.cursorX && lastCursorDraw.y == activeSnapshot.cursorY)
                return;
            let oldCursorCell = (activeSnapshot.buf[lastCursorDraw.y])[lastCursorDraw.x];
            renderText({
                text: oldCursorCell.c,
                bg: oldCursorCell.bg,
                fg: oldCursorCell.fg,
                x: lastCursorDraw.x,
                y: lastCursorDraw.y
            });
        }
        if (isCursorVisible) {
            let x = activeSnapshot.cursorX;
            let y = activeSnapshot.cursorY;
            renderCursor(x, y);
            snapshot.lastCursorDraw = { x: x, y: y };
        } else snapshot.lastCursorDraw = null;
    };

    vtcanvas.vtdraw = function (uptime) {
        snapshot.lastFrameTime = uptime;
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
        let viewportSize = rendering.getViewportSize();
        let activeSnapshot = snapshot.active;
        let lastSnapshot = snapshot.lasPresented;
        if (activeSnapshot != lastSnapshot) {
            updateCursor(uptime);
            return;
        }
        let scanW = Math.min(viewportSize.w, Math.max(activeSnapshot.w, lastSnapshot.w));
        let scanH = Math.min(viewportSize.h, Math.max(activeSnapshot.h, lastSnapshot.h));
        for (let y = 0; y < scanH; y++) {
            let contiguous = null;
            let activeRow = activeSnapshot.rows[y];
            let oldRow = lastSnapshot[y];
            for (let x = 0; x < scanW; x++) {
                let activeCell = activeRow[x];
                let oldCell = oldRow[x];
                if (activeCell == oldCell) {
                    contiguous = null;
                    continue;
                }
                let lastCursorDraw = snapshot.lastCursorDraw;
                if (lastCursorDraw != null && lastCursorDraw.x == x && lastCursorDraw.y == y)
                    snapshot.lastCursorDraw = null;
                if (contiguous != null) {
                    if (contiguous.fg == activeCell.fg && contiguous.bg == activeCell.bg) {
                        contiguous.text += activeCell.c;
                        continue;
                    }
                    else
                        renderText(contiguous);
                }
                contiguous = {
                    fg: activeCell.fg,
                    bg: activeCell.bg,
                    text: activeCell.c,
                    x: x,
                    y: y
                };
            }
        }
        updateCursor(uptime);
        snapshot.lastPresented = activeSnapshot;
    };
});