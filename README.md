# Bitbendaz Linker Tool

Bitbendaz Linker Tool is a tool that generates C/C++ header files from one or more shader files from disk.
The generated header file makes it easy to consume shaders in a game/demo without exposing them on disk.
Some trimming is made when the header file is generated, comments can optionally be removed.

The tool will also link multiple binary files into one large file and also generate a C/C++ header file which works as an index for seeking and finding resources quickly.

Bitbendaz Linker Tool comes with a GUI application (Windows only) and a CLI version which works on Windows, Linux and MacOs (requires .NET Core installed)

## Using generated files

In your C/C++ project include the generated .h files.

To get the contents from a shader by name, simply get the string (const char*) by it's name. The generated name will be set to the shader file plus the \_min suffix. Example:

  source filename is "abstract_worlds.glsl", the generated constant will be named "abstract_worlds_min"

To load an object or texture, use the generated dictionary header file to perform offset lookups. Example:
	
  int ofs = offsetForTexture(resname); // get the file offset for a resource. resname is of type std::string.
  offsetForTexture is a generated helper function.
  
If a resource is not found, ofs will be set to -1. If found, it will return the offset to where in the linked file the resource data starts (in bytes).

Each entry in the generated dictionary contains: offset in bytes, size in bytes and resource name as std::string.


## Requirements
.NET Framework 4.7.2 (WPF client) and .NET Core (2.1).

VisualStudio 2019 is recommended for compiling the WPF client.


## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License
[Beerware](https://en.wikipedia.org/wiki/Beerware)


Created by Mikael Stalvik (Stalvik / Bitbendaz)
