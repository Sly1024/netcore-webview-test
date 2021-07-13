ws = new WebSocket('ws://localhost:5050/ws');

let resolveRequestPromise = null;
let rejectRequestPromise = null;

ws.onmessage = (evt) => resolveRequestPromise?.(evt.data);
ws.onerror = (evt) => rejectRequestPromise?.(evt.data);

ws.onopen = async () => {

    function request(message) {
        ws.send(message);
        return new Promise((resolve, reject) => {
            resolveRequestPromise = resolve;
            rejectRequestPromise = reject;
        });
    }

    document.body.addEventListener('mousedown', dragStart);
    document.body.addEventListener('mouseup', dragEnd);

    let mouseOffsetX, mouseOffsetY;
    let lastWindowX = window.screenX;
    let lastWindowY = window.screenY;

    function dragStart(evt) {
        mouseOffsetX = lastWindowX - evt.screenX;
        mouseOffsetY = lastWindowY - evt.screenY;
        document.body.addEventListener('mousemove', dragMove);
    }

    function dragEnd() {
        document.body.removeEventListener('mousemove', dragMove);
    }

    function dragMove(evt) {
        request(`move${lastWindowX = evt.screenX + mouseOffsetX}, ${lastWindowY = evt.screenY + mouseOffsetY }`);
    }
};

