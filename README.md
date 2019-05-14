# Bitbendaz Linker Tool

Bitbendaz Linker Tool is a tool that generates C/C++ header files from one or more shader files from disk.
The generated header file makes it easy to consume shaders in a game/demo without exposing them on disk.
Some trimming is made when the header file is generated, comments can optionally be removed.

The tool will also link multiple binary files into one large file and also generate a C/C++ header file which works as an index for seeking and finding resources quickly.

Bitbendaz Linker Tool comes with a GUI application (Windows only) and a CLI version which works on Windows, Linux and MacOs (requires .NET Core installed)

## Requirements
.NET Framework 4.7.2 (WPF client) and .NET Core (2.1).

VisualStudio 2019 is recommended for compiling the WPF client.


## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License
[Beerware](https://en.wikipedia.org/wiki/Beerware)


Created by Mikael Stalvik (Stalvik / Bitbendaz)