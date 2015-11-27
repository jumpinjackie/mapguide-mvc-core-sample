﻿using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OSGeo.MapGuide;
using System;
using System.Runtime.InteropServices;

namespace Sheboygan
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                MapGuideApi.MgInitializeWebTier("C:\\Program Files\\OSGeo\\MapGuide\\Web\\www\\webconfig.ini");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                MapGuideApi.MgInitializeWebTier("/usr/local/mapguideopensource-3.1.0/webserverextensions/www/webconfig.ini");
            else
                throw new NotSupportedException("MapGuide is not supported on your platform");
            app.UseStaticFiles();
            app.UseDeveloperExceptionPage();
            app.UseMvcWithDefaultRoute();
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
