(function () {
    let dpRatio = window.devicePixelRatio;

    let updateCanvasResolution = function (canvas) {
        canvas.width = canvas.clientWidth * devicePixelRatio;
        canvas.height = canvas.clientHeight * devicePixelRatio;
    }

    let vtloop = function (uptime) {
        vtcanvas.vtdraw(uptime);
        requestAnimationFrame(vtloop);
    }

    window.addEventListener("load", function (eload) {
        let canvas = document.getElementById("vt");
        vtcanvas.element = canvas;
        vtcanvas.context = canvas.getContext("2d");
        updateCanvasResolution(canvas);
        window.addEventListener("resize", function (eresize) {
            updateCanvasResolution(canvas);
        });
        document.fonts.load(vtcanvas.constants.font).then(function () {
            requestAnimationFrame((uptime) => vtloop(uptime));
        });
    });
})();