# ASP.net Core Communication with WebView2

## The Goal
I wanted to test both the latency and throughput between a ASP.net Core app and a web page hosted in WebView2.

## Communication Channels
* WebSocket - The backend app starts a websocket server
* postMessage - WebView2 supports `chrome.webview.postMessage()`
* hostObjects - WebView2 supports exposing a C# object via `chrome.webview.hostObjects` and directly calling methods on it

All three communication channels are asynchronous, the functions return a Promise. In every case the JavaScript code awaits for the returned Promise.

## Test Cases
1. Latency - I wanted to **see** the visual latency, so I created a WPF app hosting WebView2 and a webpage in it. When you grab the window with the mouse inside (not the title bar) then the JavaScript code handles the "mousemove" messages and sends WindowMove requests/messages to the backend. It measures the turnaround time and shows a running average.
1. Throughput - On a button click I send 1/10/100 KB messages until 10MB of total data is sent. The total time is measured with `performance.now()` and the throughput is displayed on the page. In this case I still `await` for the returned Promise before sending the next message.

## Results
I tested this on my (aging) PC: Intel Core i3-2130 (3.4GHz), 8GB DDR3 RAM, Samsung EVO 840 SSD

|Test|WebSocket|postMessage|hostObjects|
|---|---:|---:|---:|
|Window Move|5 ms|4.8 ms|4.8 ms|
|Throughput 1KB/msg|2.1 MB/s|2.1 MB/s|2.3 MB/s|
|Throughput 10KB/msg|15.3 MB/s|14.5 MB/s|15.5 MB/s|
|Throughput 100KB/msg|36 MB/s|42 MB/s|42 MB/s|

When I open the DevConsole (F12) the results are surprisingly different:

|Test|WebSocket|postMessage|hostObjects|
|---|---:|---:|---:|
|Window Move|9 ms|8 ms|8 ms|
|Throughput 1KB/msg|1.6 MB/s|1.5 MB/s|1.6 MB/s|
|Throughput 10KB/msg|10.5 MB/s|11.5 MB/s|11.5 MB/s|
|Throughput 100KB/msg|30 MB/s|40 MB/s|40 MB/s|

## Conclusion
The results are very close. WebSocket seems to be the slowest, probably doing all the JSON serialization/deserialization and all the message correlation on the JS side inefficiently.
