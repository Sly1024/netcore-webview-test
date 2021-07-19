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
|Window Move|4.8 ms|4.7 ms|6.5 ms|
|Throughput 1KB/msg|2.3 MB/s|2.7 MB/s|0.78 MB/s|
|Throughput 10KB/msg|90 MB/s|18.3 MB/s|6.5 MB/s|
|Throughput 100KB/msg|190 MB/s|40 MB/s|27.5 MB/s|

When I open the DevConsole (F12) the results are surprisingly different:

|Test|WebSocket|postMessage|hostObjects|
|---|---:|---:|---:|
|Window Move|2.4 ms|2.7 ms|35 ms|
|Throughput 1KB/msg|1.5 MB/s|1.7 MB/s|0.5 MB/s|
|Throughput 10KB/msg|26 MB/s|12 MB/s|4.2 MB/s|
|Throughput 100KB/msg|48 MB/s|36 MB/s|23 MB/s|

## Conclusion
I thought that the hostObjects approach would be the fastest, at least in terms of latency, but it turns out the WebSocket beats it in every aspect.

An interesting result is that opening the DevConsole slows down the throughtput, but lowers the latency (not for hostObejcts though).

However, this is a proof of concept. The backend doesn't do any processing, and maybe I'm doing the host object sharing the wrong way.