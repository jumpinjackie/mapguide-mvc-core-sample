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