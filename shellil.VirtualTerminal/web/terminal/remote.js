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
        CB_WRITEBUFFER: 0x02,
        CB_SETBUFFERATTR: 0x03,
        CB_VIEWPORTCOMMAND: 0x04,
        CB_DESTROYBUFFER: 0x05,
        CB_DESTROYVIEWPORT: 0x06,
        FLAG_SIZEUPDATED: 1,
        FLAG_CURSORPOSUPDATED: 2,
        FLAG_BGCOLORUPDATED: 4,
        FLAG_FGCOLORUPDATED: 8,
        CURSOR_SOLID: 0x00,
        CURSOR_BLINK: 0x01,
        CURSOR_INVISIBLE: 0x02,
        FLAG_SETCURSORPOS: 1,
        FLAG_SETBGCOLOR: 2,
        FLAG_SETFGCOLOR: 4,
        FLAG_LINEFEED: 8,
        FLAG_SCROLLTO: 1,
        FLAG_SCROLLOFFSET: 2,
        FLAG_SCROLLCURSORINTOVIEW: 4,
        FLAG_SETCURSORSTATE: 8,
        FLAG_PRESENT: 16
    };

    let remote = {};
    vtcanvas.remote = remote;

    remote.encodeColor = function (hexCode) {
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
    remote.decodeColor = function (rg, ba) {
        let r = rg >> 8;
        let g = rg & 255;
        let b = ba >> 8;
        let a = ba & 255;
        return "#" + r.toString(16) + g.toString(16) + b.toString(16) + a.toString(16);
    };

    remote.attachToRemoteClient = function (hosturl) {
        let remoteObjectState = {
            remoteBuffers: {},
            remoteViewports: {},
            idCounter: 0
        };
        let ws = new WebSocket(hosturl);
        ws.binaryType = "arraybuffer";
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
            if (bgColorUpdated) {
                let encodedColor = remote.encodeColor(buf.bg);
                msgBuf[n++] = encodedColor.rg;
                msgBuf[n++] = encodedColor.ba;
            }
            if (fgColorUpdated) {
                let encodedColor = remote.encodeColor(buf.fg);
                msgBuf[n++] = encodedColor.rg;
                msgBuf[n++] = encodedColor.ba;
            }
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
            let msgBuf = new Uint16Array(e.data, 0, e.data.byteLength / 2);
            let msgType = msgBuf[0];
            if (msgType == net.CB_CREATEBUFFER) {
                let requestId = msgBuf[1];
                let bufferWidth = msgBuf[2];
                let buf = window.createTerminalBuffer(bufferWidth);
                let resultId = remoteObjectState.idCounter++;
                remoteObjectState.remoteBuffers[resultId] = buf;
                let responseBuf = new Uint16Array(11);
                responseBuf[0] = net.HB_BUFFERCREATED;
                responseBuf[1] = requestId;
                responseBuf[2] = resultId;
                responseBuf[3] = bufferWidth;
                responseBuf[4] = buf.getBufferHeight();
                responseBuf[5] = buf.cursorX;
                responseBuf[6] = buf.cursorY;
                let encodedBg = remote.encodeColor(buf.bg);
                let encodedFg = remote.encodeColor(buf.fg);
                responseBuf[7] = encodedBg.rg;
                responseBuf[8] = encodedBg.ba;
                responseBuf[9] = encodedFg.rg;
                responseBuf[10] = encodedFg.ba;
                ws.send(responseBuf);
            } else if (msgType == net.CB_CREATEVIEWPORT) {
                let requestId = msgBuf[1];
                let bufObjId = msgBuf[2];
                let buf = remoteObjectState.remoteBuffers[bufObjId];
                let xOffset = msgBuf[3];
                let yOffset = msgBuf[4];
                let view = buf.createViewport(xOffset, yOffset);
                let resultId = remoteObjectState.idCounter++;
                remoteObjectState.remoteViewports[resultId] = view;
                let responseBuf = new Uint16Array(6);
                responseBuf[0] = net.HB_VIEWPORTCREATED;
                responseBuf[1] = requestId;
                responseBuf[2] = resultId;
                responseBuf[3] = view.viewportX;
                responseBuf[4] = view.viewportY;
                responseBuf[5] = encodeCursorState(view.cursorState);
                ws.send(responseBuf);
            } else if (msgType == net.CB_WRITEBUFFER) {
                let requestId = msgBuf[1];
                let bufObjId = msgBuf[2];
                let buf = remoteObjectState.remoteBuffers[bufObjId];
                let bufState = saveBufferState(buf);
                let commandCount = msgBuf[3];
                buf.writeCommands(msgBuf.subarray(3), commandCount);
                notifyBufferChanges(bufObjId, bufState, buf);
                let responseBuf = new Uint16Array(2);
                responseBuf[0] = net.HB_REQUESTPROCESSED;
                responseBuf[1] = requestId;
                ws.send(responseBuf);
            } else if (msgType == net.CB_SETBUFFERATTR) {
                let requestId = msgBuf[1];
                let bufObjId = msgBuf[2];
                let actionFlags = msgBuf[3];
                let buf = remoteObjectState.remoteBuffers[bufObjId];
                let bufState = saveBufferState(buf);
                let i = 4;
                if (actionFlags & net.FLAG_SETCURSORPOS > 0) {
                    buf.cursorX = msgBuf[i++];
                    buf.cursorY = msgBuf[i++];
                }
                if (actionFlags & net.FLAG_SETBGCOLOR > 0) {
                    let rg = msgBuf[i++];
                    let ba = msgBuf[i++];
                    buf.bg = remote.decodeColor(rg, ba);
                }
                if (actionFlags & net.FLAG_SETFGCOLOR > 0) {
                    let rg = msgBuf[i++];
                    let ba = msgBuf[i++];
                    buf.fg = remote.decodeColor(rg, ba);
                }
                if (actionFlags & net.FLAG_LINEFEED > 0) {
                    let lineOffsetUnsigned = msgBuf[i++];
                    let lineOffset;
                    // check for sign bit
                    if (lineOffsetUnsigned & (1 << 15) > 0)
                        lineOffset = 0 - (lineOffsetUnsigned & (65535 >> 1));
                    else
                        lineOffset = lineOffsetUnsigned;
                    buf.lineFeed(lineOffset);
                }
                notifyBufferChanges(bufObjId, bufState, buf);
                let responseBuf = new Uint16Array(2);
                responseBuf[0] = net.HB_REQUESTPROCESSED;
                responseBuf[1] = requestId;
                ws.send(responseBuf);
            } else if (msgType == net.CB_VIEWPORTCOMMAND) {
                let requestId = msgBuf[1];
                let viewObjId = msgBuf[2];
                let actionFlags = msgBuf[3];
                let view = remoteObjectState.remoteViewports[viewObjId];
                let viewState = saveViewportState(view);
                let i = 4;
                if (actionFlags & FLAG_SCROLLTO > 0) {
                    let x = msgBuf[i++];
                    let y = msgBuf[i++];
                    view.scrollTo(x, y);
                }
                if (actionFlags & FLAG_SCROLLOFFSET > 0) {
                    let offsetX = msgBuf[i++];
                    let offsetY = msgBuf[i++];
                    view.scroll(offsetX, offsetY);
                }
                if (actionFlags & FLAG_SCROLLCURSORINTOVIEW)
                    view.scrollCursorIntoView();
                if (actionFlags & FLAG_SETCURSORSTATE)
                    decodeCursorState(msgBuf[i++]);
                notifyViewportChanges(viewObjId, viewState, view);
                let responseBuf = new Uint16Array(2);
                responseBuf[0] = net.HB_REQUESTPROCESSED;
                responseBuf[1] = requestId;
                ws.send(responseBuf);
                if (actionFlags & FLAG_PRESENT)
                    view.present();
            } else if (msgType == net.CB_DESTROYBUFFER) {
                let bufObjId = msgBuf[1];
                delete remoteObjectState.remoteBuffers[bufObjId];
            } else if (msgType == net.CB_DESTROYVIEWPORT) {
                let bufObjId = msgBuf[1];
                delete remoteObjectState.remoteViewports[bufObjId];
            }
        };
        console.log("connecting");
        ws.addEventListener("open", function (e) {
            console.log("connected");
            vtcanvas.setInteropInit(function () {
                console.log("initialized");
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
        ws.addEventListener("error", function (e) {
            console.log(e);
        });
    };
})();