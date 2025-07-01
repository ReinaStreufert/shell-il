(function () {
    vtcanvas.vtload = function () {
        let buffer = createTerminalBuffer(200);
        let view = buffer.createViewport(0, 0);
        vtcanvas.debug = {
            buffer: buffer,
            view: view
        };
        window.addEventListener("keydown", function (e) {
            console.log(e);
            let key = e.key;
            if (key == "Enter") {
                buffer.lineFeed(1);
                buffer.cursorX = 0;
                buffer.cursorY++;
            } else if (key == "Backspace") {
                if (buffer.cursorX == 0) {
                    buffer.cursorY--;
                    buffer.lineFeed(-1);
                } else {
                    buffer.cursorX--;
                    buffer.write(" ");
                    buffer.cursorX--;
                }
            }
            else if (key.length == 1)
                buffer.write(key);
            else
                return;
            view.scrollCursorIntoView();
            view.present();
        });
    };
})();