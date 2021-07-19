
const logDiv = document.querySelector('#logContainer');
let msgLatencySum = 0;
let msgLatencyCnt = 0;

let currentSetWindowPos; 
let wsSetWindowPos = initWebSocket().setWindowPos;
let pmSetWindowPos = initPostMessage().setWindowPos;
let hoSetWindowPos = initHostObject().setWindowPos;

const buttons = [
    document.querySelector('#websocketBtn'),
    document.querySelector('#postmsgBtn'),
    document.querySelector('#hostobjBtn')
];

buttons[0].addEventListener('click', selectWebSocket);
buttons[1].addEventListener('click', selectPostMessage);
buttons[2].addEventListener('click', selectHostObject);

selectWebSocket();
initLogTimer();
initDragging();


function selectWebSocket() {
    currentSetWindowPos = wsSetWindowPos;
    msgLatencySum = msgLatencyCnt = 0;
    setActiveButton(0);
}

function selectPostMessage() {
    currentSetWindowPos = pmSetWindowPos;
    msgLatencySum = msgLatencyCnt = 0;
    setActiveButton(1);
}

function selectHostObject() {
    currentSetWindowPos = hoSetWindowPos;
    msgLatencySum = msgLatencyCnt = 0;
    setActiveButton(2);
}

function setActiveButton(idx) {
    for (let i = 0; i < buttons.length; i++) {
        buttons[i].classList[i === idx ? 'add' : 'remove']('active');
    }
}

function initLogTimer() {
    setInterval(() => {
        logDiv.innerText = `Turnaround latency (avg):` + (msgLatencyCnt ? (msgLatencySum / msgLatencyCnt).toFixed(2) : '---');
    }, 500);
}

function initDragging() {
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

    async function dragMove(evt) {
        lastWindowX = evt.screenX + mouseOffsetX;
        lastWindowY = evt.screenY + mouseOffsetY;

        const startTime = performance.now();
        await currentSetWindowPos(lastWindowX, lastWindowY);
        const endTime = performance.now();
        msgLatencySum += endTime - startTime;
        msgLatencyCnt++;
    }
}


function initWebSocket() {
    var ws = new WebSocket('ws://localhost:5050/ws');

    let resolveRequestPromise = null;
    let rejectRequestPromise = null;

    ws.onmessage = (evt) => resolveRequestPromise?.(evt.data);
    ws.onerror = (evt) => rejectRequestPromise?.(evt.data);

    function wsrequest(message) {
        return new Promise((resolve, reject) => {
            resolveRequestPromise = resolve;
            rejectRequestPromise = reject;
            ws.send(message);
        });
    }

    return {
        setWindowPos(x, y) {
            return wsrequest('move' + x + ',' + y);
        }
    };
}

function initPostMessage() {
    let resolveRequestPromise = null;
    let rejectRequestPromise = null;

    window.chrome.webview.addEventListener('message', evt => {
        if (evt.data == "OK") resolveRequestPromise(); else rejectRequestPromise();
    });

    return {
        setWindowPos(x, y) {
            return new Promise((resolve, reject) => {
                resolveRequestPromise = resolve;
                rejectRequestPromise = reject;
                window.chrome.webview.postMessage('move' + x + ',' + y);
            });
        }
    };
}

function initHostObject() {
    return {
        setWindowPos(x, y) {
            return windowManager.MoveMainWindow(x, y);
        }
    };
}