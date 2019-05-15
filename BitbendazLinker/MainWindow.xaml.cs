using BitbendazLinker.Models;
using BitbendazLinker.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BitbendazLinker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LinkerViewModel _viewModel = new LinkerViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            _viewModel.SelectedShaders = listBox.SelectedItems.Cast<FileHolder>().ToList();
        }

        private void ListBoxTextures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            _viewModel.SelectedTextures = listBox.SelectedItems.Cast<FileHolder>().ToList();
        }

        private void ListBoxObjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            _viewModel.SelectedObjects = listBox.SelectedItems.Cast<FileHolder>().ToList();
        }
    }
}
