using BitbendazLinker.Models;
using BitbendazLinkerLogic;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace BitbendazLinker.ViewModels
{
    public enum ListType
    {
        Shader,
        Object,
        Texture
    }

    public class LinkerViewModel : ViewModelBase
    {
        private const string HEADER_FILTER = "Header files|*.h";
        private const string JSON_FILTER = "JSON files|*.json";
        private const string BBDATA_FILTER = "Bitbendaz data files|*.bb";

        private int _shaderCount;
        public int ShaderCount
        {
            get => _shaderCount;
            set => SetProperty(ref _shaderCount, value);
        }
        private ObservableCollection<FileHolder> _shaders;
        public ObservableCollection<FileHolder> Shaders
        {
            get => _shaders;
            set
            {
                SetProperty(ref _shaders, value);
                ShaderCount = Shaders.Count;
            }
        }
        private List<FileHolder> _selectedShaders;
        public List<FileHolder> SelectedShaders
        {
            get => _selectedShaders;
            set
            {
                SetProperty(ref _selectedShaders, value);
                RemoveShadersCommand.InvokeCanExecuteChanged();
            }
        }

        private ObservableCollection<FileHolder> _objects;
        public ObservableCollection<FileHolder> Objects
        {
            get => _objects;
            set
            {
                SetProperty(ref _objects, value);
                ObjectCount = Objects.Count;
            }
        }
        private int _objectCount;
        public int ObjectCount
        {
            get => _objectCount;
            set => SetProperty(ref _objectCount, value);
        }
        private List<FileHolder> _selectedObjects;
        public List<FileHolder> SelectedObjects
        {
            get => _selectedObjects;
            set
            {
                SetProperty(ref _selectedObjects, value);
                RemoveObjectsCommand.InvokeCanExecuteChanged();
            }
        }

        private ObservableCollection<FileHolder> _textures;
        public ObservableCollection<FileHolder> Textures
        {
            get => _textures;
            set
            {
                SetProperty(ref _textures, value);
                TextureCount = Textures.Count;
            }
        }
        private int _textureCount;
        public int TextureCount
        {
            get => _textureCount;
            set => SetProperty(ref _textureCount, value);
        }
        private List<FileHolder> _selectedTextures;
        public List<FileHolder> SelectedTextures
        {
            get => _selectedTextures;
            set
            {
                SetProperty(ref _selectedTextures, value);
                RemoveTexturesCommand.InvokeCanExecuteChanged();
            }
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


        private string _linkedOutputHeaderFile;
        public string LinkedOutputHeaderFile
        {
            get => _linkedOutputHeaderFile;
            set => SetProperty(ref _linkedOutputHeaderFile, value);
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

        private bool _generateShaders;
        public bool GenerateShaders
        {
            get => _generateShaders;
            set => SetProperty(ref _generateShaders, value);
        }
        private bool _generateLinkedFiles;
        public bool GenerateLinkedFiles
        {
            get => _generateLinkedFiles;
            set => SetProperty(ref _generateLinkedFiles, value);
        }

        public RelayCommand BrowseCommand { get; }
        public RelayCommand LoadCommand { get; }
        public RelayCommand GenerateShadersCommand { get; }
        public RelayCommand BrowseShaderOutputFileCommand { get; }
        public RelayCommand AddShadersCommand { get; }
        public RelayCommand RemoveShadersCommand { get; }
        public RelayCommand BrowseLinkedOutputFileCommand { get; }
        public RelayCommand BrowseLinkedOutputHeaderFileCommand { get; }
        public RelayCommand GenerateLinkedFileCommand { get; }
        public RelayCommand AddTexturesCommand { get; }
        public RelayCommand RemoveTexturesCommand { get; }
        public RelayCommand AddObjectsCommand { get; }
        public RelayCommand RemoveObjectsCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand SaveProjectCommand { get; }
        public RelayCommand GenerateFilesCommand { get; }

        private void OpenFileDialog(string defaultExt, string filter, Action<string> completeAction)
        {
            var dlg = new OpenFileDialog();
            dlg.DefaultExt = defaultExt;
            dlg.Filter = filter;
            if (dlg.ShowDialog() == true) completeAction(dlg.FileName);
        }
        private void SaveFileDialog(string defaultExt, string filter, Action<string> completeAction)
        {
            var dlg = new SaveFileDialog();
            dlg.DefaultExt = defaultExt;
            dlg.Filter = filter;
            if (dlg.ShowDialog() == true) completeAction(dlg.FileName);
        }
        public LinkerViewModel()
        {
            Objects = new ObservableCollection<FileHolder>();
            Shaders = new ObservableCollection<FileHolder>();
            Textures = new ObservableCollection<FileHolder>();
            BrowseCommand = new RelayCommand(o =>
            {
                OpenFileDialog(".json", JSON_FILTER, filename => {
                    IndexFile = filename;
                    LoadCommand.InvokeCanExecuteChanged();
                    LoadCommand.Execute(null);
                });
            }, o => true);
            LoadCommand = new RelayCommand(LoadFromFile, o => File.Exists(_indexFile));
            GenerateShadersCommand = new RelayCommand(InvokeGenerateShaders, o => !string.IsNullOrWhiteSpace(_shaderOutputFile));
            BrowseShaderOutputFileCommand = new RelayCommand(o =>
            {
                SaveFileDialog(".h", HEADER_FILTER, filename => {
                    ShaderOutputFile = filename;
                    GenerateShadersCommand.InvokeCanExecuteChanged();
                });
            }, o => true);
            BrowseLinkedOutputFileCommand = new RelayCommand(o =>
            {
                SaveFileDialog(".bb", BBDATA_FILTER, filename =>
                {
                    LinkedOutputFile = filename;
                    GenerateLinkedFileCommand.InvokeCanExecuteChanged();
                });
            }, o => true);
            BrowseLinkedOutputHeaderFileCommand = new RelayCommand(o =>
            {
                SaveFileDialog(".h", HEADER_FILTER, filename => {
                    LinkedOutputHeaderFile = filename;
                    GenerateLinkedFileCommand.InvokeCanExecuteChanged();
                });
            }, o => true);
            GenerateLinkedFileCommand = new RelayCommand(GenerateDataFile, o => !string.IsNullOrWhiteSpace(_linkedOutputFile) && !string.IsNullOrWhiteSpace(_linkedOutputHeaderFile));

            AddShadersCommand = new RelayCommand(o =>
            {
                SelectFilesAndAddToList(Shaders, RemoveShadersCommand);
                ShaderCount = Shaders.Count;
            }, o => true);
            RemoveShadersCommand = new RelayCommand(o =>
            {
                RemoveSelectedItemsFromList(Shaders, SelectedShaders, RemoveShadersCommand, ListType.Shader);
                ShaderCount = Shaders.Count;
            }, o => SelectedShaders?.Count > 0);

            AddObjectsCommand = new RelayCommand(o =>
            {
                SelectFilesAndAddToList(Objects, RemoveObjectsCommand);
                ObjectCount = Objects.Count;
            }, o => true);
            RemoveObjectsCommand = new RelayCommand(o =>
            {
                RemoveSelectedItemsFromList(Objects, SelectedObjects, RemoveObjectsCommand, ListType.Object);
                ObjectCount = Objects.Count;
            }, o => SelectedObjects?.Count > 0);


            AddTexturesCommand = new RelayCommand(o =>
            {
                SelectFilesAndAddToList(Textures, RemoveTexturesCommand);
                TextureCount = Textures.Count;
            }, o => true);
            RemoveTexturesCommand = new RelayCommand(o =>
            {
                RemoveSelectedItemsFromList(Textures, SelectedTextures, RemoveTexturesCommand, ListType.Texture);
                TextureCount = Textures.Count;
            }, o => SelectedTextures?.Count > 0);

            CloseCommand = new RelayCommand(o =>
            {
                Application.Current.MainWindow.Close();
            }, o => true);

            SaveProjectCommand = new RelayCommand(o =>
            {
                SaveFileDialog(".json", JSON_FILTER, filename => {
                    var contentData = new ContentData
                    {
                        Objects = Objects.ToList(),
                        Shaders = Shaders.ToList(),
                        Textures = Textures.ToList(),
                        LinkedOutputFile = _linkedOutputFile,
                        RemoveComments = _removeComments,
                        ShaderOutputFile = _shaderOutputFile,
                        LinkedOutputHeaderFile = _linkedOutputHeaderFile,
                        GenerateShaders = _generateShaders,
                        GenerateLinkedFiles = _generateLinkedFiles
                    };
                    var json = JsonConvert.SerializeObject(contentData);
                    File.WriteAllText(filename, json);
                });
            }, o => true);
            GenerateFilesCommand = new RelayCommand(o =>
            {
                if (GenerateShaders) GenerateShadersCommand.Execute(null);
                if (GenerateLinkedFiles) GenerateLinkedFileCommand.Execute(null);
            }, o => GenerateShadersCommand.CanExecute(null) || GenerateLinkedFileCommand.CanExecute(null));
        }

        private void RemoveSelectedItemsFromList(ObservableCollection<FileHolder> targetList, List<FileHolder> removeList, RelayCommand command, ListType listType)
        {
            var tmp = new List<FileHolder>();
            foreach (var item in targetList)
            {
                if (removeList.FirstOrDefault(x => x.Filename.Equals(item.Filename)) == null)
                {
                    tmp.Add(item);
                }
            }
            switch (listType)
            {
                case ListType.Shader: Shaders = new ObservableCollection<FileHolder>(tmp); break;
                case ListType.Object: Objects = new ObservableCollection<FileHolder>(tmp); break;
                case ListType.Texture: Textures = new ObservableCollection<FileHolder>(tmp); break;
            }
            removeList.Clear();
            command.InvokeCanExecuteChanged();
        }

        private void SelectFilesAndAddToList(ObservableCollection<FileHolder> targetList, RelayCommand command)
        {
            var dlg = new OpenFileDialog { Multiselect = true };
            if (dlg.ShowDialog() == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    if (targetList.FirstOrDefault(x => x.Filename == file) == null)
                        targetList.Add(new FileHolder { Filename = file, Size = Extensions.GetFileSize(file) });
                }
                command.InvokeCanExecuteChanged();
            }
        }
        private void LoadFromFile(object o)
        {
            var json = File.ReadAllText(_indexFile);
            var contentData = JsonConvert.DeserializeObject<ContentData>(json);
            Shaders = contentData.Shaders.ToFileHolder();
            Objects = contentData.Objects.ToFileHolder();
            Textures = contentData.Textures.ToFileHolder();
            ShaderOutputFile = contentData.ShaderOutputFile;
            LinkedOutputFile = contentData.LinkedOutputFile;
            RemoveComments = contentData.RemoveComments;
            LinkedOutputHeaderFile = contentData.LinkedOutputHeaderFile;
            GenerateShaders = contentData.GenerateShaders;
            GenerateLinkedFiles = contentData.GenerateLinkedFiles;
            GenerateShadersCommand.InvokeCanExecuteChanged();
            GenerateLinkedFileCommand.InvokeCanExecuteChanged();
            GenerateFilesCommand.InvokeCanExecuteChanged();
        }

        private void InvokeGenerateShaders(object o)
        {
            var (result, message) = LinkerLogic.GenerateShaders(Shaders.ToList(), _shaderOutputFile, _removeComments);
            MessageBox.Show(result ? message : $"Error: {message}");
        }

        private void GenerateDataFile(object o)
        {
            var (result, message) = LinkerLogic.GenerateLinkedFile(Objects.ToList(), Textures.ToList(), _linkedOutputFile, _linkedOutputHeaderFile);
            MessageBox.Show(result ? message : $"Error: {message}");
        }
    }
}
