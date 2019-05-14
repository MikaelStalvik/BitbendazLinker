using BitbendazLinker.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            _viewModel.SelectedShaders = listBox.SelectedItems.Cast<string>().ToList();
        }

        private void ListBoxTextures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            _viewModel.SelectedTextures = listBox.SelectedItems.Cast<string>().ToList();
        }

        private void ListBoxObjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            _viewModel.SelectedObjects = listBox.SelectedItems.Cast<string>().ToList();
        }
    }
}
