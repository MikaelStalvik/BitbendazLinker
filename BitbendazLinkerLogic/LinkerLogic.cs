﻿using System;
using System.Collections.Generic;
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

        public static (bool, string) GenerateShaders(IEnumerable<string> shaderFiles, string outputFilename, bool stripComments)
        {
            if (!shaderFiles.Any()) return (false, "No shaders selected");
            var sb = new StringBuilder();
            sb.AppendLine("namespace {");
            foreach (var shaderFile in shaderFiles)
            {
                var sourceData = File.ReadAllLines(shaderFile);
                var name = Path.GetFileName(shaderFile).Replace(Path.GetExtension(shaderFile), "_min");
                sb.AppendLine($"const char *{name} =");
                var nl = "\\n";
                foreach (var s in sourceData)
                {
                    var ts = s.Trim();
                    if (!string.IsNullOrEmpty(ts) && !ts.StartsWith("//"))
                    {
                        if (!ts.StartsWith(@"//"))
                        {
                            sb.AppendLine($"\"{s}{nl}\"");
                        }
                    }
                }
                sb.Append(";");
            }
            sb.AppendLine("}");
            File.WriteAllText(outputFilename, stripComments ? StripComments(sb.ToString()) : sb.ToString());
            return (true, "Shaders minified ok!");
        }

        private static void AddHeader(StringBuilder sb)
        {
            sb.AppendLine("#include <string>");
            sb.AppendLine("namespace {");
            sb.AppendLine("  struct FileObject");
            sb.AppendLine("  {");
            sb.AppendLine("    int offset;");
            sb.AppendLine("    int size;");
            sb.AppendLine("    std::string filename;");
            sb.AppendLine("  };");
        }

        private static long GenerateFileBlock(StringBuilder sb, string filename, long ofs)
        {
            FileInfo fi = new FileInfo(filename);
            sb.Append("{");
            sb.Append(ofs);
            sb.Append(",");
            sb.Append(fi.Length);
            sb.Append(", std::string(\"");
            sb.Append(System.IO.Path.GetFileName(filename));
            sb.Append("\")}");
            return fi.Length;
        }

        private static void GenerateBoilerplate(StringBuilder sb)
        {
            sb.AppendLine("int offsetForObject(std::string resName)");
            sb.AppendLine("{");
            sb.AppendLine("  size_t n = sizeof(objectFileObjects) / sizeof(objectFileObjects[0]);");
            sb.AppendLine("  for (int i = 0; i < n; i++)");
            sb.AppendLine("  {");
            sb.AppendLine("    if (objectFileObjects[i].filename == resName)");
            sb.AppendLine("    {");
            sb.AppendLine("      return objectFileObjects[i].offset;");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine("  return -1;");
            sb.AppendLine("}");

            sb.AppendLine("int offsetForTexture(std::string resName)");
            sb.AppendLine("{");
            sb.AppendLine("  size_t n = sizeof(textureFileObjects) / sizeof(textureFileObjects[0]);");
            sb.AppendLine("  for (int i = 0; i < n; i++)");
            sb.AppendLine("  {");
            sb.AppendLine("    if (textureFileObjects[i].filename == resName)");
            sb.AppendLine("    {");
            sb.AppendLine("      return textureFileObjects[i].offset;");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine("  return -1;");
            sb.AppendLine("}");

            sb.AppendLine("}");
        }

        private static string HeaderFilename(string filename) => Path.ChangeExtension(filename, ".h"); 
        private static void SaveHeaderFile(StringBuilder sb, string outputFilename)
        {
            File.WriteAllText(HeaderFilename(outputFilename), sb.ToString());
        }

        private static void CreateLinkedFile(string outputFilename, IEnumerable<string> objects, IEnumerable<string> textures)
        {
            using (var destFile = new FileStream(outputFilename, FileMode.Create))
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
            }
        }

        public static (bool, string) GenerateLinkedFile(IEnumerable<string> objects, IEnumerable<string> textures, string outputFilename)
        {
            if (string.IsNullOrWhiteSpace(outputFilename)) return (false, "Output filename not defined");
            if (File.Exists(outputFilename)) return (false, "Output file exists");
            if (File.Exists(HeaderFilename(outputFilename))) return (false, $"{HeaderFilename(outputFilename)} exists");
            var sb = new StringBuilder();
            AddHeader(sb);
            long ofs = 0;
            var idx = 0;
            sb.AppendLine($"FileObject objectFileObjects[{objects.Count()}] = {{");
            foreach (var file in objects)
            {
                var l = GenerateFileBlock(sb, file, ofs);
                ofs += l;
                if (idx < objects.Count() - 1)
                {
                    sb.AppendLine(",");
                };
                idx++;
            }
            sb.AppendLine("};");

            idx = 0;
            sb.AppendLine($"FileObject textureFileObjects[{textures.Count()}] = {{");
            foreach (var file in textures)
            {
                var l = GenerateFileBlock(sb, file, ofs);
                ofs += l;
                if (idx < textures.Count() - 1)
                {
                    sb.AppendLine(",");
                };
                idx++;
            }
            sb.AppendLine("};");

            GenerateBoilerplate(sb);
            SaveHeaderFile(sb, outputFilename);
            CreateLinkedFile(outputFilename, objects, textures);
            return (true, "Linked file created OK!");
        }
    }
}