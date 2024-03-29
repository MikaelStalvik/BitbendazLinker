﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BitbendazLinkerLogic
{
    public class LinkerLogic
    {
        private static string StripComments(string input)
        {
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"//(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";
            string noComments = Regex.Replace(input, blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings, me => {
                if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                    return me.Value.StartsWith("//") ? Environment.NewLine : "";
                return me.Value;
            }, RegexOptions.Singleline);
            return noComments;
        }

        private static string ShaderHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine("#include <map>");
            sb.AppendLine("#include <string>");
            sb.AppendLine("");
            sb.AppendLine("namespace Bitbendaz");
            sb.AppendLine("{");
            sb.AppendLine(" using namespace std;");
            sb.AppendLine(" class generated_shaders");
            sb.AppendLine(" {");
            sb.AppendLine("private:");
            return sb.ToString();
        }

        private static string ShaderFooter()
        {
            var sb = new StringBuilder();
            sb.AppendLine("public:");
            sb.AppendLine("string get_shader(string key)");
            sb.AppendLine("{");
            sb.AppendLine(" return shader_table[key];");
            sb.AppendLine("}");
            sb.AppendLine("};");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static bool IsLineEmpty(string data)
        {
            return string.IsNullOrEmpty(data) || data.StartsWith("//");
        }

        public static (bool, string) GenerateShaders(IEnumerable<string> shaderFiles, string outputFilename, bool stripComments)
        {
            if (!shaderFiles.Any()) return (false, "No shaders selected");
            var sb = new StringBuilder();
            sb.AppendLine(ShaderHeader());
            sb.AppendLine("map<std::string, std::string> shader_table{");

            var notFound = new StringBuilder();
            foreach (var shaderFile in shaderFiles)
            {
                if (!File.Exists(shaderFile))
                {
                    notFound.AppendLine(shaderFile);
                    Console.WriteLine($"NOT FOUND: {shaderFile}");
                    continue;
                }
                var sourceData = File.ReadAllLines(shaderFile);
                var ext = Path.GetExtension(shaderFile);
                var suffix = ext.Replace(".", "_");
                suffix += "_min";
                var name = Path.GetFileName(shaderFile).Replace(ext, string.Empty);
                name = name.Replace("-", "_");
                name += suffix;
                const string nl = "\\n";
                sb.AppendLine("{\"" + name + "\",");
                foreach (var s in sourceData)
                {
                    var ts = s.Trim();
                    //if (!string.IsNullOrEmpty(ts) && !ts.StartsWith("//"))
                    if (!IsLineEmpty(ts))
                    {
                        if (!ts.StartsWith(@"//"))
                        {
                            if (ts.StartsWith("#include"))
                            {
                                var includeName = ts.Replace("#include", string.Empty).Trim();
                                var fullIncludeName = Path.Combine(Path.GetDirectoryName(shaderFile), includeName);
                                var includeData = File.ReadAllLines(fullIncludeName);
                                foreach (var includeLine in includeData)
                                {
                                    var trimmed = includeLine.Trim();
                                    if (!IsLineEmpty(trimmed))
                                    {
                                        var adjustedInclude = trimmed.Replace("\"", "\"\"");
                                        sb.AppendLine($"\"{adjustedInclude}{nl}\"");
                                    }
                                }
                            } else
                            {
                                var adjusted = s.Replace("\"", "\"\"");
                                sb.AppendLine($"\"{adjusted}{nl}\"");
                            }
                        }
                    }
                }

                sb.AppendLine("},");

            }
            sb.AppendLine("};");
            sb.AppendLine(ShaderFooter());
            File.WriteAllText(outputFilename, stripComments ? StripComments(sb.ToString()) : sb.ToString());

            return (true, "Shaders minified ok!");
        }

        private static void AddHeader(StringBuilder sb)
        {
            sb.AppendLine("#ifndef  _LINKED_HEADER_");
            sb.AppendLine("#define _LINKED_HEADER_");

            sb.AppendLine("#include <string>");
            sb.AppendLine("namespace Bitbendaz {");
            sb.AppendLine("  struct FileObject");
            sb.AppendLine("  {");
            sb.AppendLine("    int offset;");
            sb.AppendLine("    int size;");
            sb.AppendLine("    std::string target_path;");
            sb.AppendLine("    std::string filename;");
            sb.AppendLine("  };");
        }

        private static long GenerateFileBlock(StringBuilder sb, string filename, long ofs, string targetPath)
        {
            if (!File.Exists(filename))
            {
                Debug.WriteLine($"{filename} DOES NOT EXISTS");
                return 0;
            }
            var fi = new FileInfo(filename);
            sb.Append("{");
            sb.Append(ofs);
            sb.Append(",");
            sb.Append(fi.Length);
            if (!string.IsNullOrEmpty(targetPath))
            {
                sb.Append(", std::string(\"");
                sb.Append(targetPath);
                sb.Append("\")");
            }
            else
            {
                sb.Append(", std::string(\"");
                sb.Append("\")");
            }
            sb.Append(", std::string(\"");
            sb.Append(Path.GetFileName(filename));
            sb.Append("\")}");
            return fi.Length;
        }

        private static void GenerateBoilerplate(StringBuilder sb, bool hasObjects, bool hasTextures, bool hasEmbedded)
        {
            if (hasObjects)
            {
                sb.AppendLine("static int offsetForObject(std::string resName)");
                sb.AppendLine("{");
                sb.AppendLine("  auto n = sizeof(objectFileObjects) / sizeof(objectFileObjects[0]);");
                sb.AppendLine("  for (auto i = 0; i < n; i++)");
                sb.AppendLine("  {");
                sb.AppendLine("    if (objectFileObjects[i].filename == resName)");
                sb.AppendLine("    {");
                sb.AppendLine("      return objectFileObjects[i].offset;");
                sb.AppendLine("    }");
                sb.AppendLine("  }");
                sb.AppendLine("  return -1;");
                sb.AppendLine("}");
            }

            if (hasEmbedded)
            {
                sb.AppendLine("static auto offsetForEmbedded(std::string resName)");
                sb.AppendLine("{");
                sb.AppendLine("  auto n = sizeof(embeddedFileObjects) / sizeof(embeddedFileObjects[0]);");
                sb.AppendLine("  for (auto i = 0; i < n; i++)");
                sb.AppendLine("  {");
                sb.AppendLine("    if (embeddedFileObjects[i].filename == resName)");
                sb.AppendLine("    {");
                sb.AppendLine("      return embeddedFileObjects[i].offset;");
                sb.AppendLine("    }");
                sb.AppendLine("  }");
                sb.AppendLine("  return -1;");
                sb.AppendLine("}");
            }

            if (hasTextures)
            {
                sb.AppendLine("static int offsetForTexture(std::string resName)");
                sb.AppendLine("{");
                sb.AppendLine("  auto n = sizeof(textureFileObjects) / sizeof(textureFileObjects[0]);");
                sb.AppendLine("  for (auto i = 0; i < n; i++)");
                sb.AppendLine("  {");
                sb.AppendLine("    if (textureFileObjects[i].filename == resName)");
                sb.AppendLine("    {");
                sb.AppendLine("      return textureFileObjects[i].offset;");
                sb.AppendLine("    }");
                sb.AppendLine("  }");
                sb.AppendLine("  return -1;");
                sb.AppendLine("}");
            }
            sb.AppendLine("}");
        }

        private static void SaveHeaderFile(StringBuilder sb, string outputFilename)
        {
            File.WriteAllText(outputFilename, sb.ToString());
        }

        private static void CreateLinkedFile(string outputFilename, IEnumerable<string> objects, IEnumerable<string> textures, IEnumerable<string> embeded, bool useCompression)
        {
            var tempName = Path.GetTempFileName();
            using (var destFile = new FileStream(useCompression ? tempName : outputFilename, FileMode.Create))
            {
                foreach (var file in objects)
                {
                    using (var src = new FileStream(file, FileMode.Open))
                    {
                        var buf = new byte[src.Length];
                        src.Read(buf, 0, buf.Length);
                        destFile.Write(buf, 0, buf.Length);
                    }
                }
                foreach (var file in textures)
                {
                    using (var src = new FileStream(file, FileMode.Open))
                    {
                        var buf = new byte[src.Length];
                        src.Read(buf, 0, buf.Length);
                        destFile.Write(buf, 0, buf.Length);
                    }
                }
                foreach (var file in embeded)
                {
                    if (!File.Exists(file)) continue;
                    using (var src = new FileStream(file, FileMode.Open))
                    {
                        var buf = new byte[src.Length];
                        src.Read(buf, 0, buf.Length);
                        destFile.Write(buf, 0, buf.Length);
                    }
                }
            }

            if (useCompression)
            {
                var buffer = File.ReadAllBytes(tempName);
                var compressedData = Snappy.SnappyCodec.Compress(buffer);
                using (var compressedFile = new FileStream(outputFilename, FileMode.Create))
                {
                    compressedFile.Write(compressedData, 0, compressedData.Length);
                }
                try
                {
                    File.Delete(tempName);
                }
                catch
                {
                    // just tolerate it..
                }
            }
        }

        private static readonly string[] SizeSuffixes =
            { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string SizeSuffix(long value, int decimalPlaces = 1)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            var i = 0;
            var dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }
        public static (bool, string) GenerateLinkedFile(
            IEnumerable<string> objects, 
            IEnumerable<string> textures, 
            IEnumerable<string> embedded, 
            string outputFilename, 
            string outputHeaderFilename, 
            bool useCompression)
        {
            if (string.IsNullOrWhiteSpace(outputFilename))
                return (false, "Output filename not defined");
            if (string.IsNullOrWhiteSpace(outputHeaderFilename))
                return (false, "Output header filename not defined");

            var outputPath = Path.GetDirectoryName(outputFilename);
            var sb = new StringBuilder();
            AddHeader(sb);
            long ofs = 0;
            var idx = 0;
            if (objects.Any())
            {
                sb.AppendLine($"static FileObject objectFileObjects[{objects.Count()}] = {{");
                foreach (var file in objects)
                {
                    var l = GenerateFileBlock(sb, file, ofs, string.Empty);
                    if (l > 0)
                    {
                        ofs += l;
                        if (idx < objects.Count() - 1)
                        {
                            sb.AppendLine(",");
                        };
                    }
                    idx++;
                }
                sb.AppendLine("};");
            }

            idx = 0;
            sb.AppendLine($"static FileObject textureFileObjects[{textures.Count()}] = {{");
            foreach (var file in textures)
            {
                var lastPath = new DirectoryInfo(file).Parent.Name;
                var pathOnly = Path.GetDirectoryName(file);
                var trimmedPath = pathOnly.Replace(outputPath, string.Empty, StringComparison.CurrentCultureIgnoreCase);
                if (trimmedPath.StartsWith("\\")) trimmedPath = trimmedPath.Substring(1);
                lastPath = trimmedPath.Replace("\\", "\\\\");

                var l = GenerateFileBlock(sb, file, ofs, lastPath /*string.Empty*/);
                Console.WriteLine(sb.ToString());
                ofs += l;
                if (idx < textures.Count() - 1)
                {
                    sb.AppendLine(",");
                };
                idx++;
            }
            sb.AppendLine("};");

            if (embedded.Any())
            {
                idx = 0;
                sb.AppendLine($"static FileObject embeddedFileObjects[{embedded.Count()}] = {{");
                foreach (var file in embedded)
                {
                    var lastPath = new DirectoryInfo(file).Parent.Name;
                    var pathOnly = Path.GetDirectoryName(file);
                    var trimmedPath = pathOnly.Replace(outputPath, string.Empty, StringComparison.CurrentCultureIgnoreCase);
                    if (trimmedPath.StartsWith("\\")) trimmedPath = trimmedPath.Substring(1);
                    lastPath = trimmedPath.Replace("\\", "\\\\");

                    var l = GenerateFileBlock(sb, file, ofs, lastPath);
                    ofs += l;
                    if (idx < embedded.Count() - 1)
                    {
                        sb.AppendLine(",");
                    }

                    ;
                    idx++;
                }

                sb.AppendLine("};");
            } 
            else
            {
                sb.AppendLine($"static FileObject embeddedFileObjects[1] = {{");
                sb.AppendLine("};");
            }

            GenerateBoilerplate(sb, objects.Any(), textures.Any(), embedded.Any());
            sb.AppendLine("#endif");
            SaveHeaderFile(sb, outputHeaderFilename);
            CreateLinkedFile(outputFilename, objects, textures, embedded, useCompression);

            var fileInfo = new FileInfo(outputFilename);
            return (true, $"Linked file created OK!\n\nSize: {SizeSuffix(fileInfo.Length)}");
        }
    }
}
