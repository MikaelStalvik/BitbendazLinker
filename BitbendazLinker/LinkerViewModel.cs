using BitbendazLinker.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace BitbendazLinker
{
    public class LinkerViewModel : ViewModelBase
    {
        private List<string> _shaders;
        public List<string> Shaders
        {
            get => _shaders;
            set => SetProperty(ref _shaders, value);
        }
        private List<string> _objects;
        public List<string> Objects
        {
            get => _objects;
            set => SetProperty(ref _objects, value);
        }
        private List<string> _textures;
        public List<string> Textures
        {
            get => _textures;
            set => SetProperty(ref _textures, value);
        }

        private string _indexFile;
        public string IndexFile
        {
            get => _indexFile;
            set => SetProperty(ref _indexFile, value);
        }

        private string _shaderOutputFile;
        public string ShaderOutputFile
        {
            get => _shaderOutputFile;
            set => SetProperty(ref _shaderOutputFile, value);
        }

        public RelayCommand BrowseCommand { get; }
        public RelayCommand LoadCommand { get; }
        public RelayCommand GenerateShadersCommand { get; }
        public RelayCommand BrowseShaderOutputFileCommand { get; }

        public LinkerViewModel()
        {
            BrowseCommand = new RelayCommand(o =>
            {
                var dlg = new OpenFileDialog();
                dlg.DefaultExt = ".json";
                dlg.Filter = "JSON files|*.json";
                if (dlg.ShowDialog() == true)
                {
                    IndexFile = dlg.FileName;
                    LoadCommand.InvokeCanExecuteChanged();
                }
            }, o => true);
            LoadCommand = new RelayCommand(LoadFromFile, o => File.Exists(_indexFile));
            GenerateShadersCommand = new RelayCommand(GenerateShaders, o => !string.IsNullOrWhiteSpace(_shaderOutputFile) );
            BrowseShaderOutputFileCommand = new RelayCommand(o =>
            {
                var dlg = new SaveFileDialog();
                dlg.DefaultExt = ".h";
                dlg.Filter = "Header files|*.h";
                if (dlg.ShowDialog() == true)
                {
                    ShaderOutputFile = dlg.FileName;
                    GenerateShadersCommand.InvokeCanExecuteChanged();
                }
            }, o => true);
        }
        private void LoadFromFile(object o)
        {
            var json = File.ReadAllText(_indexFile);
            var contentData = JsonConvert.DeserializeObject<ContentData>(json);
            Shaders = contentData.Shaders;
            Objects = contentData.Objects;
            Textures = contentData.Textures;
        }

        private void GenerateShaders(object o)
        {
            if (_shaders.Count == 0)
            {
                MessageBox.Show("No shaders selected");
                return;
            }
            var sb = new StringBuilder();
            sb.AppendLine("namespace {");
            foreach (var glslFile in _shaders)
            {
                var sourceData = File.ReadAllLines(glslFile);
                var name = Path.GetFileName(glslFile).Replace(".glsl", "_min");
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
            File.WriteAllText(_shaderOutputFile, sb.ToString());
            MessageBox.Show("Shader header file generated");
        }
    }
}
