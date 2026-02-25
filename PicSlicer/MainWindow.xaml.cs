using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MenuSample
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "画像を選択",
                Filter = "画像 (*.png;*.jpg)|*.png;*.jpg|すべて (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                PreviewImage.Source = new BitmapImage(new Uri(dlg.FileName));
            }
        }

        private void ToggleRuler_Click(object sender, RoutedEventArgs e)
        {

        }



        private void Save_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("保存が押されました");

        private void Exit_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
