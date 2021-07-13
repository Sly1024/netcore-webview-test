using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ws_test
{
    public class WebSocketService
    {
        public delegate void WindowMoveRequestDelegate(int x, int y);
        public WindowMoveRequestDelegate WindowMoveRequest;

        public Task Start()
        {
            return Task.Run(() => {
                CreateHostBuilder(Array.Empty<string>()).Build().Run();
            });
        }

        public IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup(_ => new Startup(WindowMoveRequest))
                    .UseUrls("http://localhost:5050");
                });
    }
}
