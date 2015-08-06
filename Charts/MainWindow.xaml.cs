using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace Charts
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ViewModel.MainViewModel vm;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = vm = new ViewModel.MainViewModel();
            ViewModel.SynchronisationHelper.RegisterParentWindow(this);
        }
 
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!vm.Close())
                e.Cancel = true;
        }
    }
}
