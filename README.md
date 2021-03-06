# Eclipse Source Converter

The Eclipse Source Converter is a tool that assists in automatically migrating Eclipse-based engines to a modern language and development environment. The current VB6 engine - while powerful and has served developers well for many years - is still incredibly limiting. VB6 has long been out of support, is almost impossible to acquire (legally), and is restricted to running on Windows. 

The end goal for this tool is to allow developers to fully migrate their source code, *including all custom edits*, from VB6 into either C# or VB.NET. In addition to source code, engine-specific converters can be developed alongside this tool to migrate data files and game assets, to facilitate a smooth and fast transition from the legacy VB6 codebase. 

There are several components in the existing VB6 codebase that do not have a direct mapping in .NET. For instance, the entire VB6 forms system is largely obsolete, and cannot be directly copied over. There are also issues with graphics and networking: the Eclipse Origins VB6 codebase uses an ActiveX component to provide Winsock interop, as well as DirectX7/DirectX8. To handle these cases, equivalent modules will be identified in .NET that can be used as a replacement: Winsock can be replaced with either System.Net.Sockets or [Lidgren](https://github.com/lidgren/lidgren-network-gen3), and DirectX7/8 can be replaced with either DirectX11 or [SFML](http://www.sfml-dev.org/). Semantic analysis will be used throughout the conversion pipeline to identify the portions of code that should be replaced and/or updated.

*Note: The current codebase is still under development, and will not yet be able to perform a full conversion*

## Building

1. Install [Visual Studio '15' Preview 5](https://aka.ms/vs/15/preview/vs_enterprise). This codebase uses C#7 features which are unavailable in earlier versions of Visual Studio
2. Clone this repository
3. Build

*Note: The code generated by this converter is compatible with Visual Studio 2015*

## Usage Instructions

The current builds are using [Eclipse Renewal](https://eclipseorigins.com/showthread.php?tid=1193) as tests. To perform a conversion, you'll need to update the path to the .vbp that you want to convert [here](https://github.com/Zynato/EclipseSourceConverter/blob/master/EclipseSourceConverter/Program.cs#L16) and run the converter. Once complete, generated files will be placed on your desktop, in an "ESC" directory.

This is all temporary until the base converter is complete. Once done, a GUI will be added to allow uses to easily convert their projects.
