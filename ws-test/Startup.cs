using System;
using System.Collections.Generic;
using System.Linq;
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
            var buffer = new byte[1024 * 4];

            Task sendMsg(string message)
            {
                var byteCount = Encoding.UTF8.GetBytes(message, 0, message.Length, buffer, 0);

                return webSocket.SendAsync(new ReadOnlyMemory<byte>(buffer, 0, byteCount), WebSocketMessageType.Text, true, CancellationToken.None).AsTask();
            }

            WebSocketReceiveResult result;
            while (!(result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)).CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                try
                {
                    ParseMessage(message);
                } 
                catch (Exception)
                {

                }

                //Console.WriteLine($"msg: {message}");

                await sendMsg($"ACK");
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        private void ParseMessage(string message)
        {
            if (message.StartsWith("move"))
            {
                var xyCoord = message[4..].Split(',');
                if (xyCoord.Length != 2) throw new ArgumentException($"Wrong message format");
                var x = int.Parse(xyCoord[0]);
                var y = int.Parse(xyCoord[1]);
                WindowMoveRequest?.Invoke(x, y);
            }
        }
    }
}
