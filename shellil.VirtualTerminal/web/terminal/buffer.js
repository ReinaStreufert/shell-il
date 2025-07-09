(function () {
    let consts = vtcanvas.constants;
    let rendering = vtcanvas.rendering;

    let newViewport = function (buf) {
        let view = {
            viewportX: 0,
            viewportY: 0,
            cursorState: "blink", // solid, blink, invisible
            buffer: buf
        };
        view.present = function () {
            rendering.setActiveViewport(view);
        };
        view.scrollTo = function (x, y) {
            let viewSize = rendering.getViewportSize();
            view.viewportX = Math.max(0, Math.min(x, buf.bufferWidth - viewSize.w));
            view.viewportY = Math.max(0, Math.min(y, buf.getBufferHeight() - viewSize.h));
        };
        view.scroll = function (x, y) {
            view.scrollTo(view.viewportX + x, view.viewportY + y);
        };
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
        };
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
        };
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
        let readControlToken = function (commandBuf, bufIndex, commandIndex, runLengthState) {
            let contiguousUnspecifiedCount = commandBuf[bufIndex++];
            let contiguousSpecifiedCount = commandBuf[bufIndex++];
            var sum = contiguousSpecifiedCount + contiguousUnspecifiedCount;
            if (sum == 0)
                throw "Invalid command format";
            runLengthState.nextControlToken = commandIndex + sum;
            runLengthState.runLengthThreshold = commandIndex + contiguousUnspecifiedCount;
            return bufIndex;
        };
        buf.writeCommands = function (commandBuf, commandCount) {
            let runLengthState = {
                position: {
                    nextControlToken: 0,
                    runLengthThreshold: 0
                },
                bgcolor: {
                    nextControlToken: 0,
                    runLengthThreshold: 0
                },
                fgcolor: {
                    nextControlToken: 0,
                    runLengthThreshold: 0
                },
                textchar: {
                    nextControlToken: 0,
                    runLengthThreshold: 0
                },
                nextCharacter: ' '
            };
            let bufIndex = 0;
            for (let commandIndex = 0; commandIndex < commandCount; commandIndex++) {
                if (commandIndex == runLengthState.position.nextControlToken)
                    bufIndex = readControlToken(commandBuf, bufIndex, commandIndex, runLengthState.position);
                if (commandIndex >= runLengthState.position.runLengthThreshold) {
                    buf.cursorX = commandBuf[bufIndex++];
                    buf.cursorY = commandBuf[bufIndex++];
                }
                if (commandIndex == runLengthState.bgcolor.nextControlToken)
                    bufIndex = readControlToken(commandBuf, bufIndex, commandIndex, runLengthState.bgcolor);
                if (commandIndex >= runLengthState.bgcolor.runLengthThreshold) {
                    let rg = commandBuf[bufIndex++];
                    let ba = commandBuf[bufIndex++];
                    buf.bg = vtcanvas.remote.decodeColor(rg, ba);
                }
                if (commandIndex == runLengthState.fgcolor.nextControlToken)
                    bufIndex = readControlToken(commandBuf, bufIndex, commandIndex, runLengthState.fgcolor);
                if (commandIndex >= runLengthState.fgcolor.runLengthThreshold) {
                    let rg = commandBuf[bufIndex++];
                    let ba = commandBuf[bufIndex++];
                    buf.fg = vtcanvas.remote.decodeColor(rg, ba);
                }
                if (commandIndex == runLengthState.textchar.nextControlToken)
                    bufIndex = readControlToken(commandBuf, bufIndex, commandIndex, runLengthState.textchar);
                if (commandIndex >= runLengthState.textchar.runLengthThreshold)
                    runLengthState.nextCharacter = String.fromCharCode(commandBuf[bufIndex++]);
                buf.write(runLengthState.nextCharacter);
            }
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