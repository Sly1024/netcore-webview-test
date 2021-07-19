using System;
using System.Dynamic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
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

        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            // Console.WriteLine("websocket connected");
            var buffer = new byte[4 * 1024 * 1024];

            Task sendMsg(string message)
            {
                var byteCount = Encoding.UTF8.GetBytes(message, 0, message.Length, buffer, 0);

                return webSocket.SendAsync(new ReadOnlyMemory<byte>(buffer, 0, byteCount), WebSocketMessageType.Text, true, CancellationToken.None).AsTask();
            }

            WebSocketReceiveResult result;
            int currentOffset = 0;

            while (!(result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, currentOffset, buffer.Length - currentOffset), CancellationToken.None)).CloseStatus.HasValue)
            {
                currentOffset += result.Count;

                if (!result.EndOfMessage)
                {
                    if (currentOffset >= buffer.Length) throw new IndexOutOfRangeException("The websocket message does not fit in the buffer.");
                    continue;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, currentOffset);
                currentOffset = 0;

                try
                {
                    await ParseMessage(message, sendMsg);
                } 
                catch (Exception e) {
                    Console.WriteLine("Error parsing websocket message", e);
                }

                //Console.WriteLine($"msg: {message}");
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }


        private class WsMessage
        {
            public int msgId;
            public string action;
            public string data;
        }

        private class WsResponse
        {
            public int msgId;
            public bool success;
            public string data;
        }

        private Task ParseMessage(string message, Func<string, Task> sendMsg)
        {
            var obj = JsonConvert.DeserializeObject<WsMessage>(message);
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
            return sendMsg(JsonConvert.SerializeObject(response));
        }
    }
}
