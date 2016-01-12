using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OSGeo.MapGuide;
using System;
using System.Runtime.InteropServices;

namespace Sheboygan
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                builder.AddJsonFile("appsettings.windows.json");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                builder.AddJsonFile("appsettings.linux.json");
            }
            else
            {
                throw new NotSupportedException("MapGuide doesn't work on this platform");
            }

            // Set up configuration sources.
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();

            string mgWebConfigPath = Configuration["MapGuide.WebConfigPath"];
            MapGuideApi.MgInitializeWebTier(mgWebConfigPath);
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseDeveloperExceptionPage();
            app.UseMvcWithDefaultRoute();
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
