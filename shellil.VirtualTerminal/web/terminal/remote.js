/*remote client mode offers the entire virtual terminal api over a WebSocket connection which is either a seperate WebSocket hosted
by the device which hosts the CDP connection or by a remote virtual terminal client over the internet.*/

(function () {
    let attachToRemoteClient = function (hosturl) {
        let remoteObjectState = {
            remoteBuffers: {},
            remoteViewports: {}
        };
        let ws = new WebSocket(hosturl);
        ws.addEventListener("open", function (e) {

        });
    };
})();