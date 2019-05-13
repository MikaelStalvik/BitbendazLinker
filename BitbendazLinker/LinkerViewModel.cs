using BitbendazLinker.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

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

        public RelayCommand BrowseCommand { get; }
        public RelayCommand LoadCommand { get; }

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
        }
        public void LoadFromFile(object o)
        {
            var json = File.ReadAllText(_indexFile);
            var contentData = JsonConvert.DeserializeObject<ContentData>(json);
            Shaders = contentData.Shaders;
            Objects = contentData.Objects;
            Textures = contentData.Textures;
        }
    }
}
