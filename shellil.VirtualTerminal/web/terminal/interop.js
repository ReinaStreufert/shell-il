(function () {
    let interopState = {
        frontendLoaded: false,
        onFrontendLoad: null
    };

    vtcanvas.vtload = function () {
        interopState.frontendLoaded = true;
        if (interopState.onFrontendLoad != null)
            interopState.onFrontendLoad();
    };

    vtcanvas.setInteropInit = function (initCallback) {
        if (interopState.frontendLoaded)
            initCallback();
        else
            interopState.onFrontendLoad = initCallback;
    };

    // it is better to do higher level apis written in js called from the library so it is basically a wrapper
    // unnecessary evaluations of new javascript text at runtimem, disposable of unused js objects, etc. take up
    // way more bandwidth than this needs.
    let eventState = {
        deltaBuildX: 0,
        deltaBuildY: 0,
        lastViewportSize: null
    };
    vtcanvas.setInteropDispatcher = function (callback) {
        let canv = vtcanvas.element;
        canv.addEventListener("resize", function (e) {
            let viewportSize = vtcanvas.rendering.getViewportSize();
            if (viewportSize.w != eventState.lastViewportSize.w || viewportSize.h != eventState.lastViewportSize.h) {
                eventState.lastViewportSize = viewportSize;
                callback({
                    event: "ViewportResize",
                    w: viewportSize.w,
                    h: viewportSize.h
                });
            };
        });
        let getModifierFlags = function (e) {
            let modifiers = 0;
            if (e.shiftKey)
                modifiers |= 1;
            if (e.altKey)
                modifiers |= 2;
            if (e.metaKey)
                modifiers |= 4;
            if (e.ctrlKey)
                modifiers |= 8;
            return modifiers;
        }
        let capturedSpecialKeys = ["Backspace", "Enter", "Tab", "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"];
        canv.addEventListener("keydown", function (e) {
            let key = e.key;
            if (key.length == 1) {
                callback({
                    event: "InputChar",
                    inputChar: key[0],
                    modifiers: getModifierFlags(e)
                });
                return;
            }
            let specialKeyIndex = capturedSpecialKeys.indexOf(key)
            if (specialKeyIndex > -1) {
                callback({
                    event: "SpecialKey",
                    specialKeyCode: specialKeyIndex,
                    modifiers: getModifierFlags(e)
                });
            }
        });
        canv.addEventListener("wheel", function (e) {
            let deltaMode = e.deltaMode;
            let scrollX;
            let scrollY;
            if (deltaMode == WheelEvent.DOM_DELTA_LINE) {
                scrollX = eventState.deltaBuildX + e.deltaX;
                scrollY = eventState.deltaBuildY + e.deltaY;
            } else if (deltaMode == WheelEvent.DOM_DELTA_PAGE) {
                let viewportSize = vtcanvas.rendering.getViewportSize();
                scrollX = eventState.deltaBuildX + e.deltaX * viewportSize.w;
                scrollY = eventState.deltaBuildY + e.deltaY * viewportSize.y;
            } else if (deltaMode == WheelEvent.DOM_DELTA_PIXEL) {
                let viewportPos = vtcanvas.rendering.viewportPosFromClientPos(e.deltaX, e.deltaY);
                scrollX = eventState.deltaBuildX + viewportPos.x;
                scrollY = eventState.deltaBuildY + viewportPos.y;
            } else throw "not implemented";
            if (scrollX > 1 || scrollY > 1) {
                let viewOffsetX = Math.floor(scrollX);
                let viewOffsetY = Math.floor(scrollY);
                eventState.deltaBuildX = scrollX - viewOffsetX;
                eventState.deltaBuildY = scrollY - viewOffsetY;
                callback({
                    event: "UserScroll",
                    offsetX: viewOffsetX, // viewport offset is opposite of wheel direction
                    offsetY: viewOffsetY
                });
            } else if (scrollX < -1 || scrollY < -1) {
                let viewOffsetX = Math.ceil(scrollX);
                let viewOffsetY = Math.ceil(scrollY);
                eventState.deltaBuildX = scrollX - viewOffsetX;
                eventState.deltaBuildY = scrollY - viewOffsetY;
                callback({
                    event: "UserScroll",
                    offsetX: viewOffsetX,
                    offsetY: viewOffsetY
                });
            }
            return false;
            //console.log("x: " + eventState.deltaBuildX.toString() + " y: " + eventState.deltaBuildY.toString());
        });
        var dispatchMouseEvent = function (e, clientEventCode) {
            let viewportPos = vtcanvas.rendering.viewportPosFromClientPos(e.offsetX, e.offsetY);
            callback({
                event: "MouseEvent",
                type: clientEventCode,
                x: Math.floor(viewportPos.x),
                y: Math.floor(viewportPos.y)
            });
        };
        canv.addEventListener("mousemove", e => dispatchMouseEvent(e, 0));
        canv.addEventListener("mousedown", e => dispatchMouseEvent(e, 1));
        canv.addEventListener("mouseup", e => dispatchMouseEvent(e, 2));
    };
})();