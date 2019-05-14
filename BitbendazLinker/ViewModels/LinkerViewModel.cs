using BitbendazLinkerLogic;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
            get => _selectedTextures;
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
        public RelayCommand CloseCommand { get; }
        public RelayCommand SaveProjectCommand { get; }

        public LinkerViewModel()
        {
            Objects = new ObservableCollection<string>();
            Shaders = new ObservableCollection<string>();
            Textures = new ObservableCollection<string>();
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
            GenerateShadersCommand = new RelayCommand(GenerateShaders, o => !string.IsNullOrWhiteSpace(_shaderOutputFile));
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
                SelectFilesAndAddToList(Shaders, RemoveShadersCommand);
            }, o => true);
            RemoveShadersCommand = new RelayCommand(o =>
            {
                RemoveSelectedItemsFromList(Shaders, SelectedShaders, RemoveShadersCommand, ListType.Shader);
            }, o => SelectedShaders?.Count > 0);

            AddObjectsCommand = new RelayCommand(o =>
            {
                SelectFilesAndAddToList(Objects, RemoveObjectsCommand);
            }, o => true);
            RemoveObjectsCommand = new RelayCommand(o =>
            {
                RemoveSelectedItemsFromList(Objects, SelectedObjects, RemoveObjectsCommand, ListType.Object);
            }, o => SelectedObjects?.Count > 0);


            AddTexturesCommand = new RelayCommand(o =>
            {
                SelectFilesAndAddToList(Textures, RemoveTexturesCommand);
            }, o => true);
            RemoveTexturesCommand = new RelayCommand(o =>
            {
                RemoveSelectedItemsFromList(Textures, SelectedTextures, RemoveTexturesCommand, ListType.Texture);
            }, o => SelectedTextures?.Count > 0);

            CloseCommand = new RelayCommand(o =>
            {
                Application.Current.MainWindow.Close();
            }, o => true);

            SaveProjectCommand = new RelayCommand(o =>
            {
                var dlg = new SaveFileDialog();
                dlg.Filter = "Project files (json)|*.json";
                dlg.DefaultExt = ".json";
                if (dlg.ShowDialog() == true)
                {
                    var contentData = new ContentData
                    {
                        Objects = Objects.ToList(),
                        Shaders = Shaders.ToList(),
                        Textures = Textures.ToList(),
                        LinkedOutputFile = _linkedOutputFile,
                        RemoveComments = _removeComments,
                        ShaderOutputFile = _shaderOutputFile
                    };
                    var json = JsonConvert.SerializeObject(contentData);
                    File.WriteAllText(dlg.FileName, json);
                }
            }, o => true);
        }

        private void RemoveSelectedItemsFromList(ObservableCollection<string> targetList, IList<string> removeList, RelayCommand command, ListType listType)
        {
            var tmp = new List<string>();
            foreach (var item in targetList)
            {
                if (!removeList.Contains(item))
                {
                    tmp.Add(item);
                }
            }
            switch (listType)
            {
                case ListType.Shader: Shaders = new ObservableCollection<string>(tmp); break;
                case ListType.Object: Objects = new ObservableCollection<string>(tmp); break;
                case ListType.Texture: Textures = new ObservableCollection<string>(tmp); break;
            }
            removeList.Clear();
            command.InvokeCanExecuteChanged();
        }

        private void SelectFilesAndAddToList(ObservableCollection<string> targetList, RelayCommand command)
        {
            var dlg = new OpenFileDialog { Multiselect = true };
            if (dlg.ShowDialog() == true)
            {
                foreach (var file in dlg.FileNames) targetList.Add(file);
                command.InvokeCanExecuteChanged();
            }
        }
        private void LoadFromFile(object o)
        {
            var json = File.ReadAllText(_indexFile);
            var contentData = JsonConvert.DeserializeObject<ContentData>(json);
            Shaders = new ObservableCollection<string>(contentData.Shaders);
            Objects = new ObservableCollection<string>(contentData.Objects);
            Textures = new ObservableCollection<string>(contentData.Textures);
            ShaderOutputFile = contentData.ShaderOutputFile;
            LinkedOutputFile = contentData.LinkedOutputFile;
            RemoveComments = contentData.RemoveComments;
        }

        private void GenerateShaders(object o)
        {
            var (result, message) = LinkerLogic.GenerateShaders(_shaders, _shaderOutputFile, _removeComments);
            MessageBox.Show(result ? message : $"Error: {message}");
        }

        private void GenerateDataFile(object o)
        {
            var (result, message) = LinkerLogic.GenerateLinkedFile(Objects, Textures, _linkedOutputFile);
            MessageBox.Show(result ? message : $"Error: {message}");
        }
    }
}
