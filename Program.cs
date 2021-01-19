using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ChartBlazorApp.Models;

namespace ChartBlazorApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var _args = args;
            if (args != null && args.Length > 0 && args[^1] == "--debug") {
                ConsoleLog.DEBUG_FLAG = true;
                //ConsoleLog.DEBUG_LEVEL = 1;
                _args = args[0..^1];
            }
            CreateHostBuilder(_args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
