using System.Collections.Generic;

namespace BitbendazLinkerLogic
{
    public class ContentData
    {
        public List<string> Shaders { get; set; }
        public List<string> Objects { get; set; }
        public List<string> Textures { get; set; }
        public List<string> Embedded { get; set; }
        public string ShaderOutputFile { get; set; }
        public string LinkedOutputFile { get; set; }
        public string LinkedOutputHeaderFile { get; set; }
        public bool RemoveComments { get; set; }
        public bool GenerateShaders { get; set; }
        public bool GenerateLinkedFiles { get; set; }
        public bool UseCompression { get; set; }
    }
}
