using BitbendazLinkerClient.Models;
using BitbendazLinkerLogic;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AdonisUI;

namespace BitbendazLinkerClient.ViewModels
{
    public enum ListType
    {
        Shader,
        Object,
        Texture,
        EmbeddedObjects
    }

    public class LinkerViewModel : ViewModelBase
    {
        private const string HEADER_FILTER = "Header files|*.h";
        private const string JSON_FILTER = "JSON files|*.json";
        private const string BBDATA_FILTER = "Bitbendaz data files|*.bb";
        private const string ALLFILES_FILTER = "All files|*.*";
        private const int TIMER_THRESHOLD = 750;

        private DispatcherTimer ShaderFilterTimer { get; set; }
        private DispatcherTimer TextureFilterTimer { get; set; }
        private DispatcherTimer EmbeddedFilterTimer { get; set; }

        private bool _isDark;
        public bool IsDark
        {
            get => _isDark;
            set
            {
                SetProperty(ref _isDark, value);
                var colorScheme = _isDark ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme;
                ResourceLocator.SetColorScheme(Application.Current.Resources, colorScheme);
            }
        }

        private int _shaderCount;
        public int ShaderCount
        {
            get => _shaderCount;
            set => SetProperty(ref _shaderCount, value);
        }

        private void UpdateShaderFilter()
        {
            FilteredShaders = _shaderFilter != null && _shaderFilter.Length >= 3 ? new ObservableCollection<FileHolder>(Shaders.Where(x => x.Filename.Contains(_shaderFilter, StringComparison.CurrentCultureIgnoreCase))) : new ObservableCollection<FileHolder>(Shaders);
        }
        private string _shaderFilter;
        public string ShaderFilter
        {
            get => _shaderFilter;
            set => SetProperty(ref _shaderFilter, value);
        }
        private ObservableCollection<FileHolder> _shaders;
        public ObservableCollection<FileHolder> Shaders
        {
            get => _shaders;
            set
            {
                SetProperty(ref _shaders, value);
                FilteredShaders = new ObservableCollection<FileHolder>(Shaders);
            }
        }
        private ObservableCollection<FileHolder> _filteredShaders;
        public ObservableCollection<FileHolder> FilteredShaders
        {
            get => _filteredShaders;
            set
            {
                SetProperty(ref _filteredShaders, value);
                ShaderCount = FilteredShaders.Count;
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

        private void UpdateTextureFilter()
        {
            FilteredTextures = _textureFilter != null && _textureFilter.Length >= 3 ? new ObservableCollection<FileHolder>(Textures.Where(x => x.Filename.Contains(_textureFilter, StringComparison.CurrentCultureIgnoreCase))) : new ObservableCollection<FileHolder>(Textures);
        }
        private string _textureFilter;

        public string TextureFilter
        {
            get => _textureFilter;
            set => SetProperty(ref _textureFilter, value);
        }

        private ObservableCollection<FileHolder> _textures;
        public ObservableCollection<FileHolder> Textures
        {
            get => _textures;
            set
            {
                SetProperty(ref _textures, value);
                FilteredTextures = new ObservableCollection<FileHolder>(Textures);
            }
        }
        private ObservableCollection<FileHolder> _filteredTextures;
        public ObservableCollection<FileHolder> FilteredTextures
        {
            get => _filteredTextures;
            set
            {
                SetProperty(ref _filteredTextures, value);
                TextureCount = Textures.Count;
                TexturesSize = _filteredTextures.Sum(x => x.Size);
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


        private void UpdateEmbeddedFilter()
        {
            FilteredEmbedded = _embeddedFilter != null && _embeddedFilter.Length >= 3 ? new ObservableCollection<FileHolder>(Embedded.Where(x => x.Filename.Contains(_embeddedFilter, StringComparison.CurrentCultureIgnoreCase))) : new ObservableCollection<FileHolder>(Embedded);
        }
        private string _embeddedFilter;
        public string EmbeddedFilter
        {
            get => _embeddedFilter;
            set => SetProperty(ref _embeddedFilter, value);
        }
        private ObservableCollection<FileHolder> _embedded;
        public ObservableCollection<FileHolder> Embedded
        {
            get => _embedded;
            set
            {
                SetProperty(ref _embedded, value);
                UpdateEmbeddedFilter();
            }
        }
        private ObservableCollection<FileHolder> _filteredEmbedded;
        public ObservableCollection<FileHolder> FilteredEmbedded
        {
            get => _filteredEmbedded;
            set
            {
                SetProperty(ref _filteredEmbedded, value);
                EmbeddedCount = FilteredEmbedded.Count;
            }
        }

        private int _embeddedCount;
        public int EmbeddedCount
        {
            get => _embeddedCount;
            set => SetProperty(ref _embeddedCount, value);
        }
        private List<FileHolder> _selectedEmbedded;
        public List<FileHolder> SelectedEmbedded
        {
            get => _selectedEmbedded;
            set
            {
                SetProperty(ref _selectedEmbedded, value);
                RemoveEmbeddedCommand.InvokeCanExecuteChanged();
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

        private bool _useCompression;
        public bool UseCompression
        {
            get => _useCompression;
            set => SetProperty(ref _useCompression, value);
        }

        private long _texturesSize;
        public long TexturesSize
        {
            get => _texturesSize;
            set => SetProperty(ref _texturesSize, value);
        }

        public RelayCommand BrowseCommand { get; }
        public RelayCommand LoadCommand { get; }
        public RelayCommand GenerateShadersCommand { get; }
        public RelayCommand BrowseShaderOutputFileCommand { get; }
        public RelayCommand AddShadersCommand { get; }
        public RelayCommand LoadShadersCommand { get; }
        public RelayCommand RemoveShadersCommand { get; }
        public RelayCommand BrowseLinkedOutputFileCommand { get; }
        public RelayCommand BrowseLinkedOutputHeaderFileCommand { get; }
        public RelayCommand GenerateLinkedFileCommand { get; }
        public RelayCommand AddTexturesCommand { get; }
        public RelayCommand LoadTexturesCommand { get; }
        public RelayCommand RemoveTexturesCommand { get; }
        public RelayCommand AddObjectsCommand { get; }
        public RelayCommand LoadObjectsCommand { get; }
        public RelayCommand RemoveObjectsCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand SaveProjectCommand { get; }
        public RelayCommand GenerateFilesCommand { get; }
        public RelayCommand AddEmbeddedCommand { get; }
        public RelayCommand LoadEmbeddedCommand { get; }
        public RelayCommand RemoveEmbeddedCommand { get; }
        public RelayCommand CleanEmbeddedCommand { get; }
        public RelayCommand ImportShadersCommand { get; }
        public RelayCommand ImportTexturesCommand { get; }
        public RelayCommand ImportEmbeddedCommand { get; }
        public RelayCommand ShaderFilterTextChangedCommand { get; }
        public RelayCommand TextureFilterTextChangedCommand { get; }
        public RelayCommand EmbeddedFilterTextChangedCommand { get; }
        public RelayCommand ShaderListboxSelectionChangedCommand { get; }
        public RelayCommand TexturesListboxSelectionChangedCommand { get; }
        public RelayCommand ObjectsListboxSelectionChangedCommand { get; }
        public RelayCommand EmbeddedListboxSelectionChangedCommand { get; }

        private void OpenFileDialog(string defaultExt, string filter, Action<string> completeAction)
        {
            var dlg = new OpenFileDialog { DefaultExt = defaultExt, Filter = filter };
            if (dlg.ShowDialog() == true) completeAction(dlg.FileName);
        }
        private void SaveFileDialog(string defaultExt, string filter, Action<string> completeAction)
        {
            var dlg = new SaveFileDialog { DefaultExt = defaultExt, Filter = filter };
            if (dlg.ShowDialog() == true) completeAction(dlg.FileName);
        }
        public LinkerViewModel()
        {
            Objects = new ObservableCollection<FileHolder>();
            Shaders = new ObservableCollection<FileHolder>();
            Textures = new ObservableCollection<FileHolder>();
            Embedded = new ObservableCollection<FileHolder>();
            IsDark = true;
            ShaderFilterTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(TIMER_THRESHOLD)};
            ShaderFilterTimer.Tick += (sender, args) =>
            {
                ShaderFilterTimer.Stop();
                UpdateShaderFilter();
            };
            TextureFilterTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(TIMER_THRESHOLD) };
            TextureFilterTimer.Tick += (sender, args) =>
            {
                TextureFilterTimer.Stop();
                UpdateTextureFilter();
            };
            EmbeddedFilterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(TIMER_THRESHOLD) };
            EmbeddedFilterTimer.Tick += (sender, args) =>
            {
                EmbeddedFilterTimer.Stop();
                UpdateEmbeddedFilter();
            };

            BrowseCommand = new RelayCommand(o =>
            {
                OpenFileDialog(".json", JSON_FILTER, filename =>
                {
                    IndexFile = filename;
                    LoadCommand.InvokeCanExecuteChanged();
                    LoadCommand.Execute(null);
                });
            }, o => true);
            LoadCommand = new RelayCommand(LoadFromFile, o => File.Exists(_indexFile));
            GenerateShadersCommand = new RelayCommand(InvokeGenerateShaders, o => !string.IsNullOrWhiteSpace(_shaderOutputFile));
            BrowseShaderOutputFileCommand = new RelayCommand(o =>
            {
                SaveFileDialog(".h", HEADER_FILTER, filename =>
                {
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
                SaveFileDialog(".h", HEADER_FILTER, filename =>
                {
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
            LoadShadersCommand = new RelayCommand(o =>
            {
                LoadFilesAndAddToList(Shaders, RemoveShadersCommand);
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
            LoadObjectsCommand = new RelayCommand(o =>
            {
                LoadFilesAndAddToList(Objects, RemoveObjectsCommand);
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
            LoadTexturesCommand = new RelayCommand(o =>
            {
                LoadFilesAndAddToList(Textures, RemoveTexturesCommand);
                TextureCount = Textures.Count;
            }, o => true);
            RemoveTexturesCommand = new RelayCommand(o =>
            {
                RemoveSelectedItemsFromList(Textures, SelectedTextures, RemoveTexturesCommand, ListType.Texture);
                TextureCount = Textures.Count;
            }, o => SelectedTextures?.Count > 0);

            AddEmbeddedCommand = new RelayCommand(o =>
            {
                SelectFilesAndAddToList(Embedded, RemoveEmbeddedCommand);
                EmbeddedCount = Embedded.Count;
            }, o => true);
            LoadEmbeddedCommand = new RelayCommand(o =>
            {
                LoadFilesAndAddToList(Embedded, RemoveEmbeddedCommand);
                EmbeddedCount = Embedded.Count;
            }, o => true);
            RemoveEmbeddedCommand = new RelayCommand(o =>
            {
                RemoveSelectedItemsFromList(Embedded, SelectedEmbedded, RemoveEmbeddedCommand, ListType.EmbeddedObjects);
                EmbeddedCount = Embedded.Count;
            }, o => SelectedEmbedded?.Count > 0);

            CloseCommand = new RelayCommand(o =>
            {
                Application.Current.MainWindow?.Close();
            }, o => true);

            SaveProjectCommand = new RelayCommand(o =>
            {
                SaveFileDialog(".json", JSON_FILTER, filename =>
                {
                    var contentData = new ContentData
                    {
                        Objects = Objects.ToList(),
                        Shaders = Shaders.ToList(),
                        Textures = Textures.ToList(),
                        Embedded = Embedded.ToList(),
                        LinkedOutputFile = _linkedOutputFile,
                        RemoveComments = _removeComments,
                        ShaderOutputFile = _shaderOutputFile,
                        LinkedOutputHeaderFile = _linkedOutputHeaderFile,
                        GenerateShaders = _generateShaders,
                        GenerateLinkedFiles = _generateLinkedFiles,
                        UseCompression = _useCompression
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
            ImportShadersCommand = new RelayCommand(o =>
            {
                OpenFileDialog("*.*", ALLFILES_FILTER, s =>
                {
                    var basePath = Path.GetDirectoryName(s);
                    if (basePath == null) return;
                    var lines = File.ReadAllLines(s);
                    foreach (var line in lines)
                    {
                        var absolutePath = Path.GetFullPath(line, basePath);
                        if (File.Exists(absolutePath))
                        {
                            if (Shaders.FirstOrDefault(x => x.Filename == absolutePath) == null)
                                Shaders.Add(new FileHolder { Filename = absolutePath, Size = Extensions.GetFileSize(absolutePath) });
                        }
                        RemoveShadersCommand.InvokeCanExecuteChanged();
                        ShaderCount = Shaders.Count;
                        UpdateShaderFilter();
                    }
                });
            }, o => true);
            ImportTexturesCommand = new RelayCommand(o =>
            {
                OpenFileDialog("*.*", ALLFILES_FILTER, s =>
                {
                    var basePath = Path.GetDirectoryName(s);
                    if (basePath == null) return;
                    var lines = File.ReadAllLines(s);
                    foreach (var line in lines)
                    {
                        var absolutePath = Path.GetFullPath(line, basePath);
                        if (File.Exists(absolutePath))
                        {
                            if (Textures.FirstOrDefault(x => x.Filename == absolutePath) == null)
                                Textures.Add(new FileHolder { Filename = absolutePath, Size = Extensions.GetFileSize(absolutePath) });
                        }
                        RemoveTexturesCommand.InvokeCanExecuteChanged();
                        TextureCount = Textures.Count;
                        UpdateTextureFilter();
                    }
                });
            }, o => true);
            ImportEmbeddedCommand = new RelayCommand(o =>
            {
                OpenFileDialog("*.*", ALLFILES_FILTER, s =>
                {
                    var basePath = Path.GetDirectoryName(s);
                    if (basePath == null) return;
                    var lines = File.ReadAllLines(s);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        if (line.StartsWith("###")) continue;
                        var absolutePath = Path.GetFullPath(line, basePath);
                        if (File.Exists(absolutePath))
                        {
                            if (Embedded.FirstOrDefault(x => x.Filename == absolutePath) == null)
                                Embedded.Add(new FileHolder { Filename = absolutePath, Size = Extensions.GetFileSize(absolutePath) });
                        }
                        RemoveEmbeddedCommand.InvokeCanExecuteChanged();
                        EmbeddedCount = Embedded.Count;
                        UpdateEmbeddedFilter();
                    }
                });
            }, o => true);
            ShaderFilterTextChangedCommand = new RelayCommand(o =>
            {
                if (!ShaderFilterTimer.IsEnabled)
                {
                    ShaderFilterTimer.Start();
                }
            }, o => true);
            TextureFilterTextChangedCommand = new RelayCommand(o =>
            {
                if (!TextureFilterTimer.IsEnabled)
                {
                    TextureFilterTimer.Start();
                }
            }, o => true);
            EmbeddedFilterTextChangedCommand = new RelayCommand(o =>
            {
                if (!EmbeddedFilterTimer.IsEnabled)
                {
                    EmbeddedFilterTimer.Start();
                }
            }, o => true);
            ShaderListboxSelectionChangedCommand = new RelayCommand(o =>
            {
                var items = (System.Collections.IList)o;
                SelectedShaders = items.Cast<FileHolder>().ToList();
            }, o => true);
            TexturesListboxSelectionChangedCommand = new RelayCommand(o =>
            {
                var items = (System.Collections.IList)o;
                SelectedTextures = items.Cast<FileHolder>().ToList();
            }, o => true);
            ObjectsListboxSelectionChangedCommand = new RelayCommand(o =>
            {
                var items = (System.Collections.IList)o;
                SelectedObjects = items.Cast<FileHolder>().ToList();
            }, o => true);
            EmbeddedListboxSelectionChangedCommand = new RelayCommand(o =>
            {
                var items = (System.Collections.IList)o;
                SelectedEmbedded = items.Cast<FileHolder>().ToList();
            }, o => true);
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
                case ListType.Shader:
                    Shaders = new ObservableCollection<FileHolder>(tmp);
                    UpdateShaderFilter();
                    break;
                case ListType.Object:
                    Objects = new ObservableCollection<FileHolder>(tmp);
                    break;
                case ListType.Texture:
                    Textures = new ObservableCollection<FileHolder>(tmp);
                    UpdateTextureFilter();
                    UpdateTextureFilter();
                    break;
                case ListType.EmbeddedObjects:
                    Embedded = new ObservableCollection<FileHolder>(tmp);
                    UpdateEmbeddedFilter();
                    break;
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
                UpdateShaderFilter();
                UpdateTextureFilter();
                UpdateEmbeddedFilter();
            }
        }
        private void LoadFilesAndAddToList(ObservableCollection<FileHolder> targetList, RelayCommand command)
        {
            var dlg = new OpenFileDialog { Multiselect = false, DefaultExt = ".txt" };
            if (dlg.ShowDialog() == true)
            {
                var lines = File.ReadAllLines(dlg.FileName);
                foreach (var file in lines)
                {
                    if (File.Exists(file))
                    {
                        if (targetList.FirstOrDefault(x => x.Filename == file) == null)
                            targetList.Add(new FileHolder { Filename = file, Size = Extensions.GetFileSize(file) });
                    }
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
            Embedded = contentData.Embedded == null ? new ObservableCollection<FileHolder>() : contentData.Embedded.ToFileHolder();
            ShaderOutputFile = contentData.ShaderOutputFile;
            LinkedOutputFile = contentData.LinkedOutputFile;
            RemoveComments = contentData.RemoveComments;
            LinkedOutputHeaderFile = contentData.LinkedOutputHeaderFile;
            GenerateShaders = contentData.GenerateShaders;
            GenerateLinkedFiles = contentData.GenerateLinkedFiles;
            UseCompression = contentData.UseCompression;
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
            var (result, message) = LinkerLogic.GenerateLinkedFile(Objects.ToList(), Textures.ToList(), Embedded.ToList(), _linkedOutputFile, _linkedOutputHeaderFile, _useCompression);
            MessageBox.Show(result ? message : $"Error: {message}");
        }
    }
}
