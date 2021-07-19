using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static ws_test.WebSocketService;

namespace ws_test
{
    public class Startup
    {
        private WindowMoveRequestDelegate WindowMoveRequest;
        public Startup(WindowMoveRequestDelegate windowMoveRequest)
        {
            WindowMoveRequest = windowMoveRequest;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
                        {
                            await Echo(context, webSocket);
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    await next();
                }

            });
        }

        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            //// Console.WriteLine("websocket connected");

            ValueTask SendMessage(ReadOnlyMemory<byte> message)
            {
                return webSocket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
            }

            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0));
            var messageLength = 0;
            var responseBuffer = new ArrayBufferWriter<byte>();

            while (true)
            {
                var mem = pipe.Writer.GetMemory(ReceiveBufferSize);

                var receiveResult = await webSocket.ReceiveAsync(mem, CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Close) break;

                messageLength += receiveResult.Count;
                pipe.Writer.Advance(receiveResult.Count);

                if (receiveResult.EndOfMessage)
                {
                    await pipe.Writer.FlushAsync();
                    while (pipe.Reader.TryRead(out var readResult))
                    {
                        if (readResult.Buffer.Length >= messageLength)
                        {
                            var messageBuffer = readResult.Buffer.Slice(readResult.Buffer.Start, messageLength);
                            await ParseMessage(messageBuffer, responseBuffer);
                            await SendMessage(responseBuffer.WrittenMemory);
                            responseBuffer.Clear();
                            pipe.Reader.AdvanceTo(messageBuffer.End);
                            messageLength = 0;
                            break;
                        }

                        if (readResult.IsCompleted) break;
                    }
                }
            }

            await webSocket.CloseAsync(webSocket.CloseStatus.Value, webSocket.CloseStatusDescription, CancellationToken.None);
        }

        private const int ReceiveBufferSize = 4 * 1024;
        
        private class WsMessage
        {
            public int msgId { get; set; }
            public string action { get; set; }
            public string data { get; set; }
        }

        private class WsResponse
        {
            public int msgId { get; set; }
            public bool success { get; set; }
            public string data { get; set; }
        }

        private Task ParseMessage(ReadOnlySequence<byte> messageBuffer, IBufferWriter<byte> responseBuffer)
        {
            var jsonReader = new Utf8JsonReader(messageBuffer);
            var obj = JsonSerializer.Deserialize<WsMessage>(ref jsonReader);
            var response = new WsResponse { msgId = obj.msgId };
            try
            {
                switch (obj.action)
                {
                    case "moveWindow":
                        var xyCoord = obj.data.Split(',');
                        if (xyCoord.Length != 2) throw new ArgumentException($"Wrong message format");
                        var x = int.Parse(xyCoord[0]);
                        var y = int.Parse(xyCoord[1]);
                        WindowMoveRequest?.Invoke(x, y);
                        response.data = "OK";
                        break;
                    case "sendMessage":
                        response.data = "OK";
                        break;
                }
                response.success = true;
            } 
            catch (Exception e)
            {
                response.success = false;
                response.data = e.ToString();
            }

            using var jsonWriter = new Utf8JsonWriter(responseBuffer);
            JsonSerializer.Serialize(jsonWriter, response);
            return Task.CompletedTask;
        }
    }
}
