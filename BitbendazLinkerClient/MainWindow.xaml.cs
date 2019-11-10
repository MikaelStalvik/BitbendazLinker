using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BitbendazLinkerClient.Models;
using BitbendazLinkerClient.ViewModels;

namespace BitbendazLinkerClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly LinkerViewModel _viewModel = new LinkerViewModel();
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
