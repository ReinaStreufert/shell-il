(function () {
    let consts = vtcanvas.constants;
    let rendering = vtcanvas.rendering;

    let newViewport = function (buf) {
        let view = {
            viewportX: 0,
            viewportY: 0,
            buffer: buf
        }
        view.present = function () {
            rendering.setActiveViewport(view)
        }
        view.scroll(x, y) = function () {
            view.viewportX += x;
            view.viewportY += y;
        }
        view.scrollCursorIntoView = function () {
            let viewSize = rendering.getViewportSize();
            let srcOffset = { x: view.viewportX, y: view.viewportY };
            let cursorX = buf.cursorX;
            let cursorY = buf.cursorY;
            let viewSrcLeft = view.viewportX;
            let viewSrcTop = view.viewportY;
            let viewSrcRight = viewSrcLeft + viewSize.w - 1;
            let viewSrcBottom = viewSrcTop + viewSize.h - 1;
            if (cursorX < viewSrcLeft)
                view.viewportX = cursorX;
            else if (cursorX > viewSrcRight)
                view.viewportX = cursorX - viewSize.w;
            if (cursorY < viewSrcTop)
                view.viewportY = cursorY;
            else if (cursorY > viewSrcBottom)
                view.viewportY = - viewSize.h;

        }
        view.getSnapshot = function () {
            let viewSize = rendering.getViewportSize();
            let bufSize = { w: buf.bufferWidth, h: buf.getBufferHeight() };
            let srcOffset = { x: view.viewportX, y: view.viewportY };
            let copyW = Math.min(viewSize.w, bufSize.w - srcOffset.x)
            let copyH = Math.min(viewSize.h, bufSize.h - srcOffset.y);
            let copiedRows = new Array(copyH);
            for (let y = 0; y < copyH; y++) {
                let copiedRow = new Array(copyW);
                let srcRow = buf.rows[y + srcOffset.y];
                for (let x = 0; x < copyW; x++) {
                    let srcX = x + srcOffset.x;
                    copiedRow[x] = srcRow[srcX];
                }
            }
            return copiedRows;
        }
    };

    window.createTerminalBuffer = function (cols) {
        let buf = {
            bufferWidth: cols,
            cursorX: 0,
            cursorY: 0,
            cursorState: "blink", // solid, blink, invisible
            bg: consts.defaultFg,
            fg: consts.defaultBg
        };
        let newRow = function () {
            let row = new Array(cols);
            for (let i = 0; i < cols; i++) {
                row.push({
                    bg: buf.bg,
                    fg: buf.fg,
                    c: null
                });
            }
        };
        buf.rows = [newRow()];
        buf.getBufferHeight = function () {
            return rows.length;
        };
        buf.lineFeed = function (lines) {
            let rows = buf.rows;
            if (lines > 0) {
                for (let i = 0; i < lines; i++)
                    rows.push(newRow());
            } else if (lines < 0) {
                rows.splice(0, 0 - lines);
            }
        };
        let advance = function () {
            let x = buf.cursorX + 1;
            let y = buf.cursorY;
            if (x >= buf.bufferWidth) {
                x = 0;
                y++;
            }
            let h = buf.rows.length;
            if (y >= h)
                buf.lineFeed(y - h + 1);
            buf.cursorX = x;
            buf.cursorY = y;
        }
        buf.write = function (text) {
            for (let i = 0; i < text.length; i++) {
                let row = buf.rows[buf.cursorY];
                row[buf.cursorX] = {
                    bg: buf.bg,
                    fg: buf.fg,
                    c: text[i]
                };
                advance();
            }
        }
    }
});