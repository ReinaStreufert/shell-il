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

    vtcanvas.setInteropInit = function (interopCallback) {
        if (interopState.frontendLoaded)
            interopCallback();
        else
            interopState.onFrontendLoad = interopCallback;
    };
})();