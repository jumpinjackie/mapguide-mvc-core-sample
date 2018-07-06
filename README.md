# MapGuide Sample for ASP.net MVC Core

This is the Developer's Guide sample code ported to ASP.net MVC core.

This sample code uses the experimental `MapGuideDotNetApi` .net package from the [mapguide-api-bindings](https://github.com/jumpinjackie/mapguide-api-bindings) project and the [mapguide-react-layout](https://github.com/jumpinjackie/mapguide-react-layout) viewer for coordinating all frontend actions.

Unlike the regular .net binding for MapGuide. The binding provided by `MapGuideDotNetApi` is *cross-platform* (supporting `netstandard2.0`) and can work in the following platforms where MapGuide binaries are also provided:

 * Windows (full .net Framework)
 * Windows (.net Core)
 * Linux (.net Core, Ubuntu 14.04 64-bit)

This sample code serves multiple purposes:

 * As an end-to-end validation of the `MapGuideDotNetApi` package
 * As an additional validation of the AJAX viewer compatibility layer provided by the [mapguide-react-layout](https://github.com/jumpinjackie/mapguide-react-layout) viewer
 * A modern update to the .net Developer's Guide sample code with none of the webforms (.aspx) legacy and old patterns.

# Requirements

* Windows or Ubuntu 14.04 64-bit
* .net Core SDK 2.1
* MapGuide Open Source 3.1.1 with Sheboygan dataset loaded

# Building and running the Sample

1. Download the latest `MapGuideDotNetApi` pre-release package [here](https://github.com/jumpinjackie/mapguide-api-bindings/releases). Save the package to the `packages` subdirectory of your git clone

2. From the root of your git clone, run:
    * `dotnet add src/MvcCoreSample package MapGuideDotNetApi --version $PACKAGE_VER` where `$PACKAGE_VER` is the nuget package version (including prerelease label)
    * `dotnet restore`
    * `dotnet build`
    * `dotnet run`

# Installing and trying out the self-contained sample

You have MapGuide installed, but don't want to bother with the motions of getting all the required build tools to build and run this sample? No problem.

Thanks to the publishing features of .net Core, a pre-built self-contained version of this example is available for you to download:

 * Windows (64-bit): [Download](https://github.com/jumpinjackie/mapguide-mvc-core-sample/releases/download/v0.1/MapGuide_MvcCoreSample_Windows_x64.zip)
 * Ubuntu 14.04 64-bit: [Download](https://github.com/jumpinjackie/mapguide-mvc-core-sample/releases/download/v0.1/MapGuide_MvcCoreSample_Ubuntu14_x64.zip)

All the required dependencies (except for MapGuide itself) are included. All you need is an existing MapGuide Open Source 3.1.1 installation with the Sheboygan dataset loaded.

To run the self-contained sample, download the appropriate package, extract it to a directory of your choosing, then from the command-line, go to that directory and run:

Windows: `MvcCoreSample.exe`
Linux: `./MvcCoreSample`

NOTE: You may need to manually `chmod +x` the `MvcCoreSample` executable on Linux after extracting the package.

Once running, open a web browser and navigate to `http://localhost:5000`