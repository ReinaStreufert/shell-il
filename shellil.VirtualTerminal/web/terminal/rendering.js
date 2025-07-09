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
        lastPresentTime: 0,
        lastFrameWidth: 0,
        lastFrameHeight: 0
    };

    rendering.renderload = function () {
        let ctx = vtcanvas.context;
        ctx.textBaseline = "middle";
        ctx.font = consts.font;
        ctx.fontKerning = "none";
        ctx.imageSmoothingEnabled = false;
        metrics.monospace = ((function () {
            let precision = 200;
            let monospaceMetric = ctx.measureText(" ".repeat(precision));
            return {
                w: (monospaceMetric.width) / precision,
                h: (Math.abs(monospaceMetric.fontBoundingBoxAscent) + Math.abs(monospaceMetric.fontBoundingBoxDescent))
            };
        }))();
    };

    rendering.setActiveViewport = function (buf) {
        snapshot.active = buf.getSnapshot();
        snapshot.lastPresentTime = snapshot.lastFrameTime;
    }

    rendering.viewportPosFromClientPos = function (x, y) {
        let monospace = metrics.monospace;
        return {
            x: x * window.devicePixelRatio / monospace.w,
            y: y * window.devicePixelRatio / monospace.h
        };
    };

    rendering.clientPosFromViewportPos = function (x, y) {
        let monospace = metrics.monospace;
        return {
            x: Math.round(x * monospace.w / window.devicePixelRatio),
            y: Math.round(y * monospace.h / window.devicePixelRatio)
        };
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
        let text = contiguous.text;
        let len = text.length;
        ctx.fillStyle = contiguous.bg;
        ctx.fillRect(x, y, len * monospace.w, monospace.h);
        ctx.fillStyle = contiguous.fg;
        if (text.trim())
            ctx.fillText(contiguous.text, x, y + monospace.h / 2);
    }

    let renderCursor = function (cursorX, cursorY) {
        let ctx = vtcanvas.context;
        let monospace = metrics.monospace;
        let x = cursorX * monospace.w + 1;
        let y = cursorY * monospace.h;
        let contrastHigh = Math.round((0.5 + (consts.cursorContrast / 2)) * 255);
        let contrastLow = Math.round((0.5 - (consts.cursorContrast / 2)) * 255);
        let bgw = consts.cursorWidth;
        let fgw = consts.cursorWidth / 2;
        ctx.fillStyle = `rgb(${contrastLow} ${contrastLow} ${contrastLow})`;
        ctx.fillRect(x, y, bgw, monospace.h);
        ctx.fillStyle = `rgb(${contrastHigh} ${contrastHigh} ${contrastHigh})`;
        ctx.fillRect(x, y, fgw, monospace.h);
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
        else if (activeSnapshot.cursorState == "blink") {
            let presentUptime = uptime - snapshot.lastPresentTime;
            isCursorVisible = presentUptime % (consts.blinkInterval * 2) < consts.blinkInterval;
        }
        let lastCursorDraw = snapshot.lastCursorDraw;
        if (lastCursorDraw != null) {
            if (isCursorVisible && lastCursorDraw.x == activeSnapshot.cursorX && lastCursorDraw.y == activeSnapshot.cursorY)
                return;
            let oldCursorRow = lastCursorDraw.y < activeSnapshot.h ? activeSnapshot.rows[lastCursorDraw.y] : null;
            let oldCursorCell = oldCursorRow == null ? null : (lastCursorDraw.x < oldCursorRow.length ? oldCursorRow[lastCursorDraw.x] : null);
            if (oldCursorCell != null) {
                renderText({
                    text: oldCursorCell.c,
                    bg: oldCursorCell.bg,
                    fg: oldCursorCell.fg,
                    x: lastCursorDraw.x,
                    y: lastCursorDraw.y
                });
            }
        }
        let x = activeSnapshot.cursorX;
        let y = activeSnapshot.cursorY;
        if (isCursorVisible && x >= 0 && y >= 0 && x < activeSnapshot.w && y < activeSnapshot.h) {
            
            renderCursor(x, y);
            snapshot.lastCursorDraw = { x: x, y: y };
        } else snapshot.lastCursorDraw = null;
    };

    vtcanvas.vtdraw = function (uptime) {
        snapshot.lastFrameTime = uptime;
        let canv = vtcanvas.element;
        let ctx = vtcanvas.context;

        let canvasWidth = canv.width;
        let canvasHeight = canv.height;
        if (canvasWidth != snapshot.lastFrameWidth || canvasHeight != snapshot.lastFrameHeight) {
            snapshot.lastFrameWidth = canvasWidth;
            snapshot.lastFrameHeight = canvasHeight;
            snapshot.lastPresented = null;
        }
        let viewportSize = rendering.getViewportSize();
        let activeSnapshot = snapshot.active;
        let lastSnapshot = snapshot.lastPresented;
        if (activeSnapshot == lastSnapshot) {
            updateCursor(uptime);
            return;
        }
        let scanW = activeSnapshot.w;
        let scanH = activeSnapshot.h;
        for (let y = 0; y < scanH; y++) {
            let contiguous = null;
            let activeRow = activeSnapshot.rows[y];
            let oldRow = lastSnapshot == null ? null : lastSnapshot[y];
            for (let x = 0; x < scanW; x++) {
                let activeCell = activeRow[x];
                let oldCell = oldRow == null ? null : oldRow[x];
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
            if (contiguous != null)
                renderText(contiguous);
        }
        let monospace = metrics.monospace;
        if (lastSnapshot != null && Math.min(lastSnapshot.w, viewportSize.w) > activeSnapshot.w) {
            let cropX = activeSnapshot.w;
            let cropY = 0;
            let cropW = viewportSize.w - activeSnapshot.w;
            let cropH = viewportSize.h;
            ctx.clearRect(cropX * monospace.w, cropY, cropW * monospace.w, cropH * monospace.h);
        }
        if (lastSnapshot != null && Math.min(lastSnapshot.h, viewportSize.h) > activeSnapshot.h) {
            let cropX = 0;
            let cropY = activeSnapshot.h;
            let cropW = viewportSize.w;
            let cropH = viewportSize.h - activeSnapshot.h;
            ctx.clearRect(cropX, cropY * monospace.h, cropW * monospace.w, cropH * monospace.h);
        }
        updateCursor(uptime);
        snapshot.lastPresented = activeSnapshot;
    };
})();