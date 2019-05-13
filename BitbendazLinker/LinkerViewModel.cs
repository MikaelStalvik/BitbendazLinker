using BitbendazLinker.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

namespace BitbendazLinker
{
    public class LinkerViewModel : ViewModelBase
    {
        private ObservableCollection<string> _shaders;
        public ObservableCollection<string> Shaders
        {
            get => _shaders;
            set => SetProperty(ref _shaders, value);
        }
        private ObservableCollection<string> _objects;
        public ObservableCollection<string> Objects
        {
            get => _objects;
            set => SetProperty(ref _objects, value);
        }
        private ObservableCollection<string> _textures;
        public ObservableCollection<string> Textures
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

        private List<string> _selectedShaders;
        public List<string> SelectedShaders
        {
            get => _selectedShaders;
            set
            {
                SetProperty(ref _selectedShaders, value);
                RemoveShadersCommand.InvokeCanExecuteChanged();
            }
        }

        public RelayCommand BrowseCommand { get; }
        public RelayCommand LoadCommand { get; }
        public RelayCommand GenerateShadersCommand { get; }
        public RelayCommand BrowseShaderOutputFileCommand { get; }
        public RelayCommand RemoveShadersCommand { get; }

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
                    LoadCommand.Execute(null);
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
            RemoveShadersCommand = new RelayCommand(o => 
            {
                var tmp = new List<string>();
                foreach (var item in _shaders)
                {
                    if (!_selectedShaders.Contains(item))
                    {
                        tmp.Add(item);
                    }
                }
                Shaders = new ObservableCollection<string>(tmp);
                
                SelectedShaders.Clear();
                RemoveShadersCommand.InvokeCanExecuteChanged();
            }, o => SelectedShaders?.Count > 0);
        }
        private void LoadFromFile(object o)
        {
            var json = File.ReadAllText(_indexFile);
            var contentData = JsonConvert.DeserializeObject<ContentData>(json);
            Shaders = new ObservableCollection<string>(contentData.Shaders);
            Objects = new ObservableCollection<string>(contentData.Objects);
            Textures = new ObservableCollection<string>(contentData.Textures);
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
