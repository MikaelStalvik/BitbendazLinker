using BitbendazLinkerLogic;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace BitbendazLinkerCli
{
    class Program
    {
        static void ShowHelp()
        {
            Console.WriteLine("BBLINK.EXE");
            Console.WriteLine("");
            Console.WriteLine("PARAMETERS:");
            Console.WriteLine("  --help: Show this page");
            Console.WriteLine("  --proj: project input file");
            Console.WriteLine("  --so: generated shader output filename");
            Console.WriteLine("  --lo: generated linked file output filename");
            Console.WriteLine("  --lhf: generated linked file header filename");
            Console.WriteLine("  --removecomments: remove comments when linking shaders");
        }
        static bool HasMissingMandatoryParameters(string[] args)
        {
            return args.FirstOrDefault(x => x.ToLowerInvariant().StartsWith("--so")) == null || 
                args.FirstOrDefault(x => x.ToLowerInvariant().StartsWith("--lo")) == null ||
                args.FirstOrDefault(x => x.ToLowerInvariant().StartsWith("--proj")) == null;
        }
        static bool HelpRequested(string[] args)
        {
            return args.Count() == 0 || 
                args.FirstOrDefault(x => x.ToLowerInvariant() == "--help") != null || 
                args.FirstOrDefault(x => x.ToLowerInvariant() == "--h") != null;
        }
        static (string, string) SplitArg(string arg)
        {
            var res = arg.Split('=');
            return (res[0], res[1]);
        }
        static void Main(string[] args)
        {
            if (HelpRequested(args))
            {
                ShowHelp();
                return;
            }
            if (HasMissingMandatoryParameters(args))
            {
                Console.WriteLine("Missing mandatory parameters (--so, --lo, --lhf or --proj).");
                Console.WriteLine("Type bblink.exe --help for more information.");
            }
            else
            {
                var (_, shaderOutputFile) = SplitArg(args.FirstOrDefault(x => x.ToLowerInvariant().StartsWith("--so")));
                var (_, linkedOutputFile) = SplitArg(args.FirstOrDefault(x => x.ToLowerInvariant().StartsWith("--lo")));
                var (_, linkedOutputHeaderFile) = SplitArg(args.FirstOrDefault(x => x.ToLowerInvariant().StartsWith("--lhf")));
                var (_, projectFile) = SplitArg(args.FirstOrDefault(x => x.ToLowerInvariant().StartsWith("--proj")));
                var removeComments = args.FirstOrDefault(x => x.ToLowerInvariant().StartsWith("--removecomments")) != null;
                if (!File.Exists(projectFile))
                {
                    Console.WriteLine($"Project file '{projectFile}' not found");
                    return;
                }
                var json = File.ReadAllText(projectFile);
                var contentData = JsonConvert.DeserializeObject<ContentData>(json);
                LinkerLogic.GenerateShaders(contentData.Shaders, shaderOutputFile, removeComments);
                LinkerLogic.GenerateLinkedFile(contentData.Objects, contentData.Textures,  contentData.Embedded, linkedOutputFile, linkedOutputHeaderFile, true);
                Console.WriteLine($"  {shaderOutputFile} generated with no issues.");
                Console.WriteLine($"  {linkedOutputFile} generated with no issues.");
            }
        }
    }
}
