using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Syncfusion.UI.Xaml.Diagram.Controls;
using Syncfusion.UI.Xaml.Diagram;
using Syncfusion.Licensing;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using WpfLibrary1;

/*
 H...Horizontal
 V...Vertical
 */


namespace HomeMenu
{
    public partial class MainWindow : Window
    {
        private GuideLineManager guideManager;

        public MainWindow()
        {
            InitializeComponent();
            guideManager = new GuideLineManager(GuidLineCanvas);

            diagram.HorizontalRuler = new Ruler()
            {
                Orientation = Orientation.Horizontal
            };

            diagram.VerticalRuler = new Ruler()
            {
                Orientation = Orientation.Vertical
            };
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewImage.Source is BitmapSource bitmap)
            {
                SaveImage(bitmap);
            }
            else
            {
                MessageBox.Show("保存する画像がありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
            => Close();

        private void Add_H_Click(object sender, RoutedEventArgs e)
        {
            guideManager.AddLine();
        }

        private void Remove_H_Click(object sender, RoutedEventArgs e)
        {
            guideManager.RemoveTopLine();
        }

        private void Add_V_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Remove_V_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveImage(BitmapSource image)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Filter =
                "PNG Image (*.png)|*.png|" +
                "JPEG Image (*.jpg)|*.jpg|" +
                "Bitmap Image (*.bmp)|*.bmp";

            if (dlg.ShowDialog() != true)
                return;

            BitmapEncoder encoder;

            string ext = Path.GetExtension(dlg.FileName).ToLower();

            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder()
                    {
                        QualityLevel = 90
                    };
                    break;

                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;

                default:
                    encoder = new PngBitmapEncoder();
                    break;
            }

            encoder.Frames.Add(BitmapFrame.Create(image));

            using (FileStream stream = new FileStream(dlg.FileName, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }
    }

}
