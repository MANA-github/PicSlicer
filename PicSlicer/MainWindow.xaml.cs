using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Syncfusion.UI.Xaml.Diagram;
using WpfLibrary1;
using Syncfusion.UI.Xaml.Diagram.Controls;
using System.Windows.Controls;

namespace HomeMenu
{
    public partial class MainWindow : Window
    {
        private readonly GuideLineManager guideManager;

        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            guideManager = new GuideLineManager(GuidLineCanvas);

            diagram.HorizontalRuler = CreateRuler(Orientation.Horizontal);
            diagram.VerticalRuler = CreateRuler(Orientation.Vertical);
        }

        private Ruler CreateRuler(Orientation orientation)
        {
            return new Ruler { Orientation = orientation };
        }

        // =========================
        // 画像読み込み
        // =========================
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "画像を選択",
                Filter = "画像 (*.png;*.jpg)|*.png;*.jpg|すべて (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            LoadImage(dlg.FileName);
        }

        private void LoadImage(string path)
        {
            var bitmap = new BitmapImage(new Uri(path));

            PreviewImage.Source = bitmap;

            ImageWidth = bitmap.PixelWidth;
            ImageHeight = bitmap.PixelHeight;

            guideManager.SetImageSize(ImageWidth, ImageHeight);

            ImageSizeText.Text = $"画像サイズ: {ImageWidth} x {ImageHeight}";
        }

        // =========================
        // 保存
        // =========================
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewImage.Source is not BitmapSource bitmap)
            {
                ShowError("保存する画像がありません。");
                return;
            }

            SaveImage(bitmap);
        }

        private void SaveImage(BitmapSource image)
        {
            var dlg = new SaveFileDialog
            {
                Filter =
                    "PNG Image (*.png)|*.png|" +
                    "JPEG Image (*.jpg)|*.jpg|" +
                    "Bitmap Image (*.bmp)|*.bmp"
            };

            if (dlg.ShowDialog() != true) return;

            var encoder = CreateEncoder(Path.GetExtension(dlg.FileName));

            encoder.Frames.Add(BitmapFrame.Create(image));

            using var stream = new FileStream(dlg.FileName, FileMode.Create);
            encoder.Save(stream);
        }

        private BitmapEncoder CreateEncoder(string ext)
        {
            return ext.ToLower() switch
            {
                ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 90 },
                ".bmp" => new BmpBitmapEncoder(),
                _ => new PngBitmapEncoder()
            };
        }

        // =========================
        // ガイドライン操作
        // =========================
        private void Add_H_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckImageLoaded()) return;
            guideManager.Add_H_Line();
        }

        private void Remove_H_Click(object sender, RoutedEventArgs e)
            => guideManager.Remove_H_Line();

        private void Add_V_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckImageLoaded()) return;
            guideManager.Add_V_Line();
        }

        private void Remove_V_Click(object sender, RoutedEventArgs e)
            => guideManager.Remove_V_Line();

        // =========================
        // 共通処理
        // =========================
        private bool CheckImageLoaded()
        {
            if (PreviewImage.Source != null) return true;

            ShowError("画像が読み込まれていません");
            return false;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}