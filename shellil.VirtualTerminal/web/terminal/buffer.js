(function () {
    let consts = vtcanvas.constants;
    let rendering = vtcanvas.rendering;

    let newViewport = function (buf) {
        let view = {
            viewportX: 0,
            viewportY: 0,
            cursorState: "blink", // solid, blink, invisible
            buffer: buf
        }
        view.present = function () {
            rendering.setActiveViewport(view)
        }
        view.scroll = function (x, y) {
            let viewSize = rendering.getViewportSize();
            view.viewportX = Math.max(0, Math.min(view.viewportX + x, buf.bufferWidth - viewSize.w));
            view.viewportY = Math.max(0, Math.min(view.viewportY + y, buf.getBufferHeight() - viewSize.h));
        }
        view.scrollCursorIntoView = function () {
            let viewSize = rendering.getViewportSize();
            let cursorX = buf.cursorX;
            let cursorY = buf.cursorY;
            let viewSrcLeft = view.viewportX;
            let viewSrcTop = view.viewportY;
            let viewSrcRight = viewSrcLeft + viewSize.w - 1;
            let viewSrcBottom = viewSrcTop + viewSize.h - 1;
            if (cursorX < viewSrcLeft)
                view.viewportX = cursorX;
            else if (cursorX > viewSrcRight)
                view.viewportX = cursorX - (viewSize.w - 1);
            if (cursorY < viewSrcTop)
                view.viewportY = cursorY;
            else if (cursorY > viewSrcBottom)
                view.viewportY = cursorY - (viewSize.h - 1);
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
                copiedRows[y] = copiedRow;
                let srcRow = buf.rows[y + srcOffset.y];
                for (let x = 0; x < copyW; x++) {
                    let srcX = x + srcOffset.x;
                    copiedRow[x] = srcRow[srcX];
                }
            }
            return {
                w: copyW,
                h: copyH,
                rows: copiedRows,
                cursorX: buf.cursorX - view.viewportX,
                cursorY: buf.cursorY - view.viewportY,
                cursorState: view.cursorState
            };
        }
        return view;
    };

    window.createTerminalBuffer = function (cols) {
        let buf = {
            bufferWidth: cols,
            cursorX: 0,
            cursorY: 0,
            bg: consts.defaultBg,
            fg: consts.defaultFg
        };
        let newRow = function () {
            let row = new Array(cols);
            for (let i = 0; i < cols; i++) {
                row[i] = ({
                    bg: buf.bg,
                    fg: buf.fg,
                    c: ' '
                });
            }
            return row;
        };
        buf.rows = [newRow()];
        buf.getBufferHeight = function () {
            return buf.rows.length;
        };
        buf.lineFeed = function (lines) {
            let rows = buf.rows;
            if (lines > 0) {
                for (let i = 0; i < lines; i++)
                    rows.push(newRow());
            } else if (lines < 0) {
                rows.splice(rows.length + lines, 0 - lines);
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
        };
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
        };
        buf.writeCommands = function (commandBuf) {
            
        };
        buf.createViewport = function (xOffset, yOffset) {
            let view = newViewport(buf);
            view.viewportX = xOffset;
            view.viewportY = yOffset;
            return view;
        }
        return buf;
    }
})();