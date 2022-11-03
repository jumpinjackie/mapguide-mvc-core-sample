using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OSGeo.MapGuide;
using System.Runtime.InteropServices;
using System;

var builder = WebApplication.CreateBuilder(args);

// OS platform checks
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Configuration.AddJsonFile("appsettings.windows.json");
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Configuration.AddJsonFile("appsettings.linux.json");
}
else
{
    throw new NotSupportedException("MapGuide doesn't work on this platform");
}

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Init the web tier
string mgWebConfigPath = app.Configuration["MapGuide.WebConfigPath"];
MapGuideApi.MgInitializeWebTier(mgWebConfigPath);

//NOTE: https disabled for localhost. If you wish to enable, uncomment the following lines below
//
// 1. app.UseHsts()
// 2. app.UseHttpsRedirection()

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
