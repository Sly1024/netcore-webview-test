
const latencyTextContainer = document.querySelector('#latencyTextContainer');
const thoughputTextContainer = document.querySelector('#thoughputTextContainer');

let msgLatencySum = 0;
let msgLatencyCnt = 0;

let currentChannel; 
let wsChannel = initWebSocket();
let pmChannel = initPostMessage();
let hoChannel = initHostObject();
let mockChannel = initMockComm();

const buttons = [
    document.querySelector('#websocketBtn'),
    document.querySelector('#postmsgBtn'),
    document.querySelector('#hostobjBtn'),
    document.querySelector('#mockcommBtn'),
];

buttons[0].addEventListener('click', selectWebSocket);
buttons[1].addEventListener('click', selectPostMessage);
buttons[2].addEventListener('click', selectHostObject);
buttons[3].addEventListener('click', selectMockComm);


document.querySelector('#through1Btn').addEventListener('click', () => testThroughput(1024));
document.querySelector('#through2Btn').addEventListener('click', () => testThroughput(1024*10));
document.querySelector('#through3Btn').addEventListener('click', () => testThroughput(1024*100));

selectWebSocket();
initLatencyLogTimer();
initDragging();


function selectWebSocket() {
    currentChannel = wsChannel;
    msgLatencySum = msgLatencyCnt = 0;
    setActiveButton(0);
}

function selectPostMessage() {
    currentChannel = pmChannel;
    msgLatencySum = msgLatencyCnt = 0;
    setActiveButton(1);
}

function selectHostObject() {
    currentChannel = hoChannel;
    msgLatencySum = msgLatencyCnt = 0;
    setActiveButton(2);
}

function selectMockComm() {
    currentChannel = mockChannel;
    msgLatencySum = msgLatencyCnt = 0;
    setActiveButton(3);
}

function setActiveButton(idx) {
    for (let i = 0; i < buttons.length; i++) {
        buttons[i].classList[i === idx ? 'add' : 'remove']('active');
    }
}

function initLatencyLogTimer() {
    setInterval(() => {
        latencyTextContainer.innerText = 'Turnaround latency (avg):' + (msgLatencyCnt ? (msgLatencySum / msgLatencyCnt).toFixed(2) : '---');
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
        await currentChannel.setWindowPos(lastWindowX, lastWindowY);
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
        },
        sendMessage(msg) {
            return wsrequest('message' + msg);
        }
    };
}

function initPostMessage() {
    let resolveRequestPromise = null;
    let rejectRequestPromise = null;

    window.chrome.webview.addEventListener('message', evt => {
        if (evt.data == "OK") resolveRequestPromise(); else rejectRequestPromise();
    });

    function sendPostMsg(msg) {
        return new Promise((resolve, reject) => {
            resolveRequestPromise = resolve;
            rejectRequestPromise = reject;
            window.chrome.webview.postMessage(msg);
        });
    }

    return {
        setWindowPos(x, y) {
            return sendPostMsg('move' + x + ',' + y);
        },
        sendMessage(msg) {
            return sendPostMsg('message' + msg);
        }
    };
}

function initHostObject() {
    return {
        setWindowPos(x, y) {
            return chrome.webview.hostObjects.bridge.MoveMainWindow(x, y);
        },
        sendMessage(msg) {
            return chrome.webview.hostObjects.bridge.SendMessage(msg);
        }
    };
}

function initMockComm() {
    return {
        setWindowPos(x, y) {
            return Promise.resolve("OK");
        },
        sendMessage(msg) {
            return Promise.resolve("OK");
        }
    };
}

async function testThroughput(messageSize) {
    const totalAmount = 10 * (1 << 20); // 10 MB

    function writeLog(msg) {
        thoughputTextContainer.innerText = `Throughput (${(totalAmount / (1 << 20))} MB in ${messageSize / (1 << 10)} KB messages, ` +
            (totalAmount / messageSize).toFixed(0) + ` messages): ` + msg;
    }

    writeLog('testing...');

    // generate a 1KB message
    var message = '0123'.repeat(messageSize/4);

    let sent = 0;

    const startTime = performance.now();

    while (sent < totalAmount) {
        await currentChannel.sendMessage(message);
        sent += message.length;
    }

    const endTime = performance.now();

    writeLog((sent / (endTime - startTime) / 1024).toFixed(2) + 'MB/s');
}