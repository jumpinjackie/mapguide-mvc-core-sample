# MapGuide Sample for ASP.net MVC Core

This is the Developer's Guide sample code ported to ASP.net MVC core.

This sample code uses the new MapGuide API nuget packages and the [mapguide-react-layout](https://github.com/jumpinjackie/mapguide-react-layout) viewer for coordinating all frontend actions.

Starting with MapGuide Open Source 4.0, the .net bindings are now *cross-platform* (supporting `netstandard2.0`) and can work in the following platforms where MapGuide binaries are also provided:

 * Windows (full .net Framework)
 * Windows (.net Core, .net 5+)
 * Linux (.net Core, .net 5+)

This sample code serves multiple purposes:

 * As an end-to-end validation of the MapGuide API nuget packages
 * As an additional validation of the AJAX viewer compatibility layer provided by the [mapguide-react-layout](https://github.com/jumpinjackie/mapguide-react-layout) viewer
 * A modern update to the .net Developer's Guide sample code with none of the webforms (.aspx) legacy and old patterns.

# Requirements

* Windows or Linux
* .net SDK 8.0 (you can use older .net versions, but this sample app targets `net8.0`)
* MapGuide Open Source 4.0 RC1 or newer with Sheboygan dataset loaded

# Building and running the Sample

From the root of your git clone, run:
    * `dotnet restore`
    * `dotnet build`
    * `dotnet run`

# Installing and trying out the self-contained sample

You have MapGuide installed, but don't want to bother with the motions of getting all the required build tools to build and run this sample? No problem.

Thanks to the publishing features of .net Core, a pre-built self-contained version of this example is available for you to download:

 * Windows (64-bit): [Download](https://github.com/jumpinjackie/mapguide-mvc-core-sample/releases/download/v0.2/MapGuide_MvcCoreSample_Windows_x64.zip)
 * Ubuntu 14.04 64-bit: [Download](https://github.com/jumpinjackie/mapguide-mvc-core-sample/releases/download/v0.2/MapGuide_MvcCoreSample_Linux_x64.zip)

All the required dependencies (except for MapGuide itself) are included. All you need is an existing MapGuide Open Source 4.0 RC1 or newer installation with the Sheboygan dataset loaded.

To run the self-contained sample, download the appropriate package, extract it to a directory of your choosing, then from the command-line, go to that directory and run:

 * Windows: `MvcCoreSample.exe`
 * Linux: `./MvcCoreSample`

NOTE: You may need to manually `chmod +x` the `MvcCoreSample` executable on Linux after extracting the package.

Once running, open a web browser and navigate to `http://localhost:5000`