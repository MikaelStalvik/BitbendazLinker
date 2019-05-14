using BitbendazLinker.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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

        private string _linkedOutputFile;
        public string LinkedOutputFile
        {
            get => _linkedOutputFile;
            set => SetProperty(ref _linkedOutputFile, value);
        }
        private bool _removeComments;
        public bool RemoveComments
        {
            get => _removeComments;
            set => SetProperty(ref _removeComments, value);
        }

        private List<string> _selectedTextures;
        public List<string> SelectedTextures
        {
            get => _selectedShaders;
            set
            {
                SetProperty(ref _selectedTextures, value);
                RemoveTexturesCommand.InvokeCanExecuteChanged();
            }
        }

        private List<string> _selectedObjects;
        public List<string> SelectedObjects
        {
            get => _selectedObjects;
            set
            {
                SetProperty(ref _selectedObjects, value);
                RemoveObjectsCommand.InvokeCanExecuteChanged();
            }
        }

        public RelayCommand BrowseCommand { get; }
        public RelayCommand LoadCommand { get; }
        public RelayCommand GenerateShadersCommand { get; }
        public RelayCommand BrowseShaderOutputFileCommand { get; }
        public RelayCommand AddShadersCommand { get; }
        public RelayCommand RemoveShadersCommand { get; }
        public RelayCommand BrowseLinkedOutputFileCommand { get; }
        public RelayCommand GenerateDataFileCommand { get; }
        public RelayCommand AddTexturesCommand { get; }
        public RelayCommand RemoveTexturesCommand { get; }
        public RelayCommand AddObjectsCommand { get; }
        public RelayCommand RemoveObjectsCommand { get; }

        public LinkerViewModel()
        {
            Shaders = new ObservableCollection<string>();
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
            BrowseLinkedOutputFileCommand = new RelayCommand(o =>
            {
                var dlg = new SaveFileDialog();
                dlg.DefaultExt = ".bb";
                dlg.Filter = "Bitbendaz data files|*.bb";
                if (dlg.ShowDialog() == true)
                {
                    LinkedOutputFile = dlg.FileName;
                    GenerateDataFileCommand.InvokeCanExecuteChanged();
                }
            }, o => true);
            GenerateDataFileCommand = new RelayCommand(GenerateDataFile, o => !string.IsNullOrWhiteSpace(_linkedOutputFile));

            AddShadersCommand = new RelayCommand(o =>
            {
                var dlg = new OpenFileDialog();
                dlg.Multiselect = true;
                if (dlg.ShowDialog() == true)
                {
                    foreach (var file in dlg.FileNames) Shaders.Add(file);
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


            AddTexturesCommand = new RelayCommand(o =>
            {
                var dlg = new OpenFileDialog();
                dlg.Multiselect = true;
                if (dlg.ShowDialog() == true)
                {
                    foreach (var file in dlg.FileNames) Textures.Add(file);
                }
            }, o => true);
            RemoveTexturesCommand = new RelayCommand(o =>
            {
                var tmp = new List<string>();
                foreach (var item in _textures)
                {
                    if (!_selectedTextures.Contains(item))
                    {
                        tmp.Add(item);
                    }
                }
                Textures = new ObservableCollection<string>(tmp);

                SelectedTextures.Clear();
                RemoveTexturesCommand.InvokeCanExecuteChanged();
            }, o => SelectedTextures?.Count > 0);

            AddObjectsCommand = new RelayCommand(o =>
            {
                /// TODO REFACT - REUSE
                var dlg = new OpenFileDialog();
                dlg.Multiselect = true;
                if (dlg.ShowDialog() == true)
                {
                    foreach (var file in dlg.FileNames) Objects.Add(file);
                }
            }, o => true);
            RemoveTexturesCommand = new RelayCommand(o =>
            {
                // TODO REFACT
                var tmp = new List<string>();
                foreach (var item in _objects)
                {
                    if (!_selectedObjects.Contains(item))
                    {
                        tmp.Add(item);
                    }
                }
                Objects = new ObservableCollection<string>(tmp);

                SelectedObjects.Clear();
                RemoveObjectsCommand.InvokeCanExecuteChanged();
            }, o => SelectedObjects?.Count > 0);

        }
        private void LoadFromFile(object o)
        {
            var json = File.ReadAllText(_indexFile);
            var contentData = JsonConvert.DeserializeObject<ContentData>(json);
            Shaders = new ObservableCollection<string>(contentData.Shaders);
            Objects = new ObservableCollection<string>(contentData.Objects);
            Textures = new ObservableCollection<string>(contentData.Textures);
        }

        private string StripComments(string input)
        {
            if (!_removeComments) return input;
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"//(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";
            string noComments = Regex.Replace(input, blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings, me => {
                if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                    return me.Value.StartsWith("//") ? Environment.NewLine : "";
                // Keep the literal strings
                return me.Value;
            }, RegexOptions.Singleline);
            return noComments;
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
                var name = Path.GetFileName(glslFile).Replace(Path.GetExtension(glslFile), "_min");
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
            File.WriteAllText(_shaderOutputFile, StripComments(sb.ToString()));
            MessageBox.Show("Shader header file generated");
        }

        private long GenerateFileBlock(StringBuilder sb, string filename, long ofs)
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

        private void AddHeader(StringBuilder sb)
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
        private void GenerateBoilerplate(StringBuilder sb)
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
        private string DataHeaderFilename => Path.ChangeExtension(_linkedOutputFile, ".h");
        private void SaveHeaderFile(StringBuilder sb)
        {
            File.WriteAllText(DataHeaderFilename, sb.ToString());
        }
        private void CreeateLinkedFile()
        {
            using (var destFile = new FileStream(_linkedOutputFile, FileMode.Create))
            {
                foreach (var file in Objects)
                {
                    using (var src = new FileStream(file, FileMode.Open))
                    {
                        var buf = new byte[src.Length];
                        src.Read(buf, 0, buf.Length);
                        destFile.Write(buf, 0, buf.Length);
                    }
                }
                foreach (var file in Textures)
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
        private void GenerateDataFile(object o)
        {
            // check if files exists
            var sb = new StringBuilder();
            AddHeader(sb);
            long ofs = 0;
            var idx = 0;
            sb.AppendLine($"FileObject objectFileObjects[{Objects.Count}] = {{");
            foreach (var file in Objects)
            {
                var l = GenerateFileBlock(sb, file, ofs);
                ofs += l;
                if (idx < Objects.Count - 1)
                {
                    sb.AppendLine(",");
                };
                idx++;
            }
            sb.AppendLine("};");

            idx = 0;
            sb.AppendLine($"FileObject textureFileObjects[{Textures.Count}] = {{");
            foreach (var file in Textures)
            {
                var l = GenerateFileBlock(sb, file, ofs);
                ofs += l;
                if (idx < Textures.Count - 1)
                {
                    sb.AppendLine(",");
                };
                idx++;
            }
            sb.AppendLine("};");

            GenerateBoilerplate(sb);
            SaveHeaderFile(sb);
            CreeateLinkedFile();
        }
    }
}
