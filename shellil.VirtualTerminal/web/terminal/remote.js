/*remote client mode offers the entire virtual terminal api over a WebSocket connection which is either a seperate WebSocket hosted
by the device which hosts the CDP connection or by a remote virtual terminal client over the internet.*/

(function () {
    let net = {
        HB_FRONTENDREADY: 0x00,
        HB_VIEWRESIZE: 0x01,
        HB_INPUTCHAR: 0x02,
        HB_SPECIALKEY: 0x03,
        HB_USERSCROLL: 0x04,
        HB_MOUSE: 0x05,
        HB_BUFFERCREATED: 0x06,
        HB_VIEWPORTCREATED: 0x07,
        HB_BUFFERUPDATED: 0x08,
        HB_VIEWPORTUPDATED: 0x09,
        HB_REQUESTPROCESSED: 0x0A,
        CB_CREATEBUFFER: 0x00,
        CB_CREATEVIEWPORT: 0x01,
        CB_PROCBUFFER: 0x02,
        CB_PROCVIEWPORT: 0x03,
        FLAG_SIZEUPDATED: 1,
        FLAG_CURSORPOSUPDATED: 2,
        FLAG_BGCOLORUPDATED: 4,
        FLAG_FGCOLORUPDATED: 8,
        CURSOR_SOLID: 0x00,
        CURSOR_BLINK: 0x01,
        CURSOR_INVISIBLE: 0x02
    };

    let attachToRemoteClient = function (hosturl) {
        let remoteObjectState = {
            remoteBuffers: {},
            remoteViewports: {}
        };
        let ws = new WebSocket(hosturl);
        let interopDispatcherCallback = function (interopEvent) {
            if (interopEvent.event == "ViewportResize") {
                let buf = new Uint16Array(3);
                buf[0] = net.HB_VIEWRESIZE;
                buf[1] = interopEvent.w;
                buf[2] = interopEvent.h;
                ws.send(buf);
            } else if (interopEvent.event == "InputChar") {
                let buf = new Uint16Array(3);
                buf[0] = net.HB_INPUTCHAR;
                buf[1] = interopEvent.inputChar.charCodeAt(0);
                buf[2] = interopEvent.modifiers;
                ws.send(buf);
            } else if (interopEvent.event == "SpecialKey") {
                let buf = new Uint16Array(3);
                buf[0] = net.HB_SPECIALKEY;
                buf[1] = interopEvent.specialKeyCode;
                buf[2] = interopEvent.modifiers;
                ws.send(buf);
            } else if (interopEvent.event == "UserScroll") {
                let buf = new Uint16Array(3);
                buf[0] = net.HB_USERSCROLL;
                buf[1] = interopEvent.offsetX;
                buf[2] = interopEvent.offsetY;
                ws.send(buf);
            } else if (interopEvent.event == "MouseEvent") {
                let buf = new Uint16Array(4);
                buf[0] = net.HB_MOUSE;
                buf[1] = interopEvent.type;
                buf[2] = interopEvent.x
                buf[3] = interopEvent.y;
                ws.send(buf);
            }
        };
        let encodeColor = function (hexCode) {
            if (hexCode[0] != "#")
                throw {};
            let r = Number.parseInt(hexCode.substr(1, 2), 16);
            let g = Number.parseInt(hexCode.substr(3, 2), 16);
            let b = Number.parseInt(hexCode.substr(5, 2), 16);
            let a;
            if (hexCode.length == 9)
                a = Number.parseInt(hexCode.substr(7, 2), 16);
            else
                a = 255;
            return {
                rg: r << 8 | g,
                ba: b << 8 | a
            };
        };
        let decodeColor = function (rg, ba) {
            let r = rg >> 8;
            let g = rg & 255;
            let b = ba >> 8;
            let a = ba & 255;
            return "#" + r.toString(16) + g.toString(16) + b.toString(16) + a.toString(16);
        };
        let encodeCursorState = function (stateName) {
            if (stateName == "solid")
                return net.CURSOR_SOLID;
            else if (stateName == "blink")
                return net.CURSOR_BLINK;
            else
                return net.CURSOR_INVISIBLE;
        }
        let decodeCursorState = function (code) {
            if (code == net.CURSOR_SOLID)
                return "solid";
            else if (code == net.CURSOR_BLINK)
                return "blink";
            else if (code == net.CURSOR_INVISIBLE)
                return "invisible";
            else throw {};
        };
        let saveBufferState = function (buf) {
            return {
                w: buf.bufferWidth,
                h: buf.getBufferHeight(),
                cursorX: buf.cursorX,
                cursorY: buf.cursorY,
                bg: buf.bg,
                fg: buf.fg
            };
        };
        let notifyBufferChanges = function (bufferId, bufferState, buf) {
            let sizeUpdated = (buf.bufferWidth != bufferState.w) || (buf.getBufferHeight() != bufferState.h);
            let cursorPosUpdated = (buf.cursorX != bufferState.cursorX) || (buf.cursorY != bufferState.cursorY);
            let bgColorUpdated = buf.bg != bufferState.bg;
            let fgColorUpdated = buf.fg != bufferState.fg;
            let msgLen = 3; // message type, buffer id, change flags
            let changeFlags = 0;
            if (sizeUpdated) {
                changeFlags |= FLAG_SIZEUPDATED;
                msgLen += 2
            }
            if (cursorPosUpdated) {
                changeFlags |= FLAG_CURSORPOSUPDATED;
                msgLen += 2;
            }
            if (bgColorUpdated) {
                changeFlags |= FLAG_BGCOLORUPDATED;
                msgLen += 2;
            }
            if (fgColorUpdated) {
                changeFlags |= FLAG_FGCOLORUPDATED;
                msgLen += 2;
            }
            let msgBuf = new Uint16Array(msgLen);
            msgBuf[0] = HB_BUFFERUPDATED;
            msgBuf[1] = bufferId;
            msgBuf[2] = changeFlags;
            let n = 3;
            if (sizeUpdated) {
                msgBuf[n++] = buf.bufferWidth;
                msgBuf[n++] = buf.getBufferHeight();
            }
            if (cursorPosUpdated) {
                msgBuf[n++] = buf.cursorX;
                msgBuf[n++] = buf.cursorY;
            }
            if (bgColorUpdated)
                msgBuf[n++] = encodeColor(buf.bg);
            if (fgColorUpdated)
                msgBuf[n++] = encodeColor(buf.fg);
            ws.send(msgBuf);
        };
        let saveViewportState = function (view) {
            return {
                viewportX: view.viewportX,
                viewportY: view.viewportY,
                cursorState: view.cursorState
            };
        };
        let notifyViewportChanges = function (viewportId, viewState, view) {
            if (viewState.viewportX != view.viewportX || viewState.viewportY != view.viewportY || viewState.cursorState != view.cursorState) {
                let msgBuf = new Uint16Array(5);
                msgBuf[0] = HB_VIEWPORTUPDATED;
                msgBuf[1] = viewportId;
                msgBuf[2] = view.viewportX;
                msgBuf[3] = view.viewportY;
                msgBuf[4] = encodeCursorState(view.cursorState);
                ws.send(msgBuf);
            }
        }
        let onMessage = function (e) {
            let msgBuf = e.data;
            
        };
        ws.addEventListener("open", function (e) {
            vtcanvas.setInteropInit(function () {
                vtcanvas.setInteropDispatcher(interopDispatcherCallback);
                ws.addEventListener("message", onMessage);
                let msgBuf = new Uint16Array(3);
                msgBuf[0] = net.HB_FRONTENDREADY;
                let viewportSize = vtcanvas.rendering.getViewportSize();
                msgBuf[1] = viewportSize.w;
                msgBuf[2] = viewportSize.h;
                ws.send(msgBuf);
            });
        });
        
    };
})();