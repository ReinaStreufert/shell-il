(function () {
    let dpRatio = window.devicePixelRatio;

    let updateCanvasResolution = function (canvas) {
        canvas.width = canvas.clientWidth * devicePixelRatio;
        canvas.height = canvas.clientHeight * devicePixelRatio;
    }

    let vtloop = function (vtdraw) {
        vtdraw();
        requestAnimationFrame(vtloop);
    }

    window.addEventListener("load", function (eload) {
        let canvas = document.getElementById("vt");
        let vtdraw = vtcanvas.vtdraw;
        let consts = vtcanvas.constants;
        window.vtcanvas = {
            element: canvas,
            context: canvas.getContext("2d")
        };
        updateCanvasResolution(canvas);
        window.addEventListener("resize", function (eresize) {
            updateCanvasResolution(canvas);
        });
        document.fonts.load(consts.font).then(function () {
            requestAnimationFrame(() => vtloop(vtdraw))
        });
    });
})();