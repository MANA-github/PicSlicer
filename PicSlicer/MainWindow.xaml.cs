using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PicSlicer
{
    public partial class MainWindow : Window
    {
        private readonly GuideLineManager guides;

        private BitmapSource? image;
        private string? imagePath;

        private double zoom = 1.0;
        private const double MinZoom = 0.05;
        private const double MaxZoom = 32;

        private GuideLine? dragging;

        public MainWindow()
        {
            InitializeComponent();

            guides = new GuideLineManager(GuideCanvas);
            guides.Changed += UpdateSliceText;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "画像を選択",
                Filter = "画像 (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|すべて (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;
            LoadImage(dlg.FileName);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                LoadImage(files[0]);
        }

        private void LoadImage(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.EndInit();
                bitmap.Freeze();

                image = bitmap;
                imagePath = path;
            }
            catch (Exception ex)
            {
                ShowError($"画像を読み込めませんでした。\n{ex.Message}");
                return;
            }

            PreviewImage.Source = image;
            DropHint.Visibility = Visibility.Collapsed;

            guides.SetImageSize(image.PixelWidth, image.PixelHeight);

            Title = $"PicSlicer - {Path.GetFileName(path)}";
            ImageSizeText.Text = $"画像サイズ: {image.PixelWidth} x {image.PixelHeight}";

            Dispatcher.BeginInvoke(ZoomToFit, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckImageLoaded()) return;

            var slices = guides.GetSlices();

            var dlg = new SaveFileDialog
            {
                Title = $"分割して保存（{slices.Count} 枚）— ファイル名の末尾に _行_列 が付きます",
                FileName = Path.GetFileNameWithoutExtension(imagePath) ?? "slice",
                Filter =
                    "PNG Image (*.png)|*.png|" +
                    "JPEG Image (*.jpg)|*.jpg|" +
                    "Bitmap Image (*.bmp)|*.bmp"
            };

            if (dlg.ShowDialog() != true) return;

            string dir = Path.GetDirectoryName(dlg.FileName)!;
            string baseName = Path.GetFileNameWithoutExtension(dlg.FileName);
            string ext = Path.GetExtension(dlg.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".png";

            int rows = slices.Max(s => s.Row);
            int cols = slices.Max(s => s.Col);
            string rowFmt = new('0', rows.ToString().Length);
            string colFmt = new('0', cols.ToString().Length);

            var names = slices.Select(s =>
                Path.Combine(dir, $"{baseName}_{s.Row.ToString(rowFmt)}_{s.Col.ToString(colFmt)}{ext}")).ToList();

            int existing = names.Count(File.Exists);
            if (existing > 0 &&
                MessageBox.Show($"{existing} 個のファイルが既に存在します。上書きしますか？", "確認",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                for (int i = 0; i < slices.Count; i++)
                {
                    var piece = new CroppedBitmap(image!, slices[i].Rect);
                    SaveBitmap(piece, names[i]);
                }
            }
            catch (Exception ex)
            {
                ShowError($"保存に失敗しました。\n{ex.Message}");
                return;
            }

            MessageBox.Show($"{slices.Count} 枚の画像を保存しました。\n{dir}", "完了",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void SaveBitmap(BitmapSource bitmap, string path)
        {
            var encoder = CreateEncoder(Path.GetExtension(path));

            if (encoder is not PngBitmapEncoder && HasAlpha(bitmap))
                bitmap = FlattenOnWhite(bitmap);

            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new FileStream(path, FileMode.Create);
            encoder.Save(stream);
        }

        private static BitmapEncoder CreateEncoder(string ext) => ext.ToLower() switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 90 },
            ".bmp" => new BmpBitmapEncoder(),
            _ => new PngBitmapEncoder()
        };

        private static bool HasAlpha(BitmapSource b) =>
            b.Format == PixelFormats.Bgra32 || b.Format == PixelFormats.Pbgra32 ||
            b.Format == PixelFormats.Rgba64 || b.Format == PixelFormats.Prgba64 ||
            b.Format == PixelFormats.Rgba128Float || b.Format == PixelFormats.Prgba128Float;

        private static BitmapSource FlattenOnWhite(BitmapSource src)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var rect = new Rect(0, 0, src.PixelWidth, src.PixelHeight);
                dc.DrawRectangle(Brushes.White, null, rect);
                dc.DrawImage(src, rect);
            }
            var rtb = new RenderTargetBitmap(src.PixelWidth, src.PixelHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            return rtb;
        }

        private void Add_H_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckImageLoaded()) return;
            guides.Add(true, VisibleCenterPixel(true));
        }

        private void Add_V_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckImageLoaded()) return;
            guides.Add(false, VisibleCenterPixel(false));
        }

        private int VisibleCenterPixel(bool isHorizontal)
        {
            double center = isHorizontal
                ? Scroller.VerticalOffset + Math.Min(Scroller.ViewportHeight, guides.ImageHeight * zoom) / 2
                : Scroller.HorizontalOffset + Math.Min(Scroller.ViewportWidth, guides.ImageWidth * zoom) / 2;
            return (int)Math.Round(center / zoom);
        }

        private void Remove_Selected_Click(object sender, RoutedEventArgs e)
        {
            if (guides.Selected != null) guides.Remove(guides.Selected);
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => guides.Clear();

        private void EvenGrid_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckImageLoaded()) return;

            var result = EvenGridDialog.Show(this);
            if (result is (int rows, int cols))
                guides.SetEvenGrid(rows, cols);
        }

        private void ImageCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (image == null) return;

            var p = e.GetPosition(ImageCanvas);
            var hit = guides.HitTest(p.X, p.Y);

            guides.Selected = hit;
            guides.Render();

            if (hit != null)
            {
                dragging = hit;
                ImageCanvas.CaptureMouse();
                e.Handled = true;
            }
            Focus();
        }

        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (image == null) return;

            var p = e.GetPosition(ImageCanvas);
            int px = (int)Math.Floor(p.X / zoom);
            int py = (int)Math.Floor(p.Y / zoom);

            TopRuler.Marker = p.X / zoom;
            LeftRuler.Marker = p.Y / zoom;
            CursorText.Text = px >= 0 && py >= 0 && px < image.PixelWidth && py < image.PixelHeight
                ? $"カーソル: {px}, {py}"
                : "カーソル: -";

            if (dragging != null)
            {
                double v = dragging.IsHorizontal ? p.Y / zoom : p.X / zoom;
                guides.Move(dragging, (int)Math.Round(v));
                CursorText.Text = $"{(dragging.IsHorizontal ? "Y" : "X")} = {dragging.Position}";
                Mouse.OverrideCursor = dragging.IsHorizontal ? Cursors.SizeNS : Cursors.SizeWE;
                return;
            }

            var hover = guides.HitTest(p.X, p.Y);
            ImageCanvas.Cursor = hover == null ? null : hover.IsHorizontal ? Cursors.SizeNS : Cursors.SizeWE;
        }

        private void ImageCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (dragging == null) return;

            var p = e.GetPosition(ImageCanvas);
            bool outside = dragging.IsHorizontal
                ? p.Y < 0 || p.Y > guides.ImageHeight * zoom
                : p.X < 0 || p.X > guides.ImageWidth * zoom;

            if (outside) guides.Remove(dragging);

            dragging = null;
            Mouse.OverrideCursor = null;
            ImageCanvas.ReleaseMouseCapture();
        }

        private void ImageCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (dragging != null) return;
            TopRuler.Marker = null;
            LeftRuler.Marker = null;
            CursorText.Text = "カーソル: -";
        }

        private void TopRuler_MouseDown(object sender, MouseButtonEventArgs e) => StartDragFromRuler(false, e);
        private void LeftRuler_MouseDown(object sender, MouseButtonEventArgs e) => StartDragFromRuler(true, e);

        private void StartDragFromRuler(bool isHorizontal, MouseButtonEventArgs e)
        {
            if (image == null) return;

            var p = e.GetPosition(ImageCanvas);
            double v = isHorizontal ? p.Y : p.X;
            dragging = guides.Add(isHorizontal, (int)Math.Round(v / zoom));
            ImageCanvas.CaptureMouse();
            e.Handled = true;
        }
        
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (image == null || Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;

            var sel = guides.Selected;
            int step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;

            switch (e.Key)
            {
                case Key.H: Add_H_Click(sender, e); break;
                case Key.V: Add_V_Click(sender, e); break;
                case Key.Delete or Key.Back when sel != null: guides.Remove(sel); break;
                case Key.Up when sel is { IsHorizontal: true }: guides.Move(sel, sel.Position - step); break;
                case Key.Down when sel is { IsHorizontal: true }: guides.Move(sel, sel.Position + step); break;
                case Key.Left when sel is { IsHorizontal: false }: guides.Move(sel, sel.Position - step); break;
                case Key.Right when sel is { IsHorizontal: false }: guides.Move(sel, sel.Position + step); break;
                case Key.Escape: guides.Selected = null; guides.Render(); break;
                default: return;
            }
            e.Handled = true;
        }

        private void SetZoom(double newZoom, Point? anchorInViewport = null)
        {
            if (image == null) return;

            newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

            var anchor = anchorInViewport ?? new Point(Scroller.ViewportWidth / 2, Scroller.ViewportHeight / 2);
            double imgX = (Scroller.HorizontalOffset + anchor.X) / zoom;
            double imgY = (Scroller.VerticalOffset + anchor.Y) / zoom;

            zoom = newZoom;

            double w = image.PixelWidth * zoom;
            double h = image.PixelHeight * zoom;
            ImageCanvas.Width = w;
            ImageCanvas.Height = h;
            PreviewImage.Width = w;
            PreviewImage.Height = h;

            RenderOptions.SetBitmapScalingMode(PreviewImage,
                zoom >= 2 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);

            guides.Zoom = zoom;
            guides.Render();
            TopRuler.Zoom = zoom;
            LeftRuler.Zoom = zoom;
            ZoomText.Text = $"{zoom * 100:0}%";

            Scroller.UpdateLayout();
            Scroller.ScrollToHorizontalOffset(imgX * zoom - anchor.X);
            Scroller.ScrollToVerticalOffset(imgY * zoom - anchor.Y);
        }

        private void ZoomToFit()
        {
            if (image == null) return;
            double fit = Math.Min(Scroller.ActualWidth / image.PixelWidth,
                                  Scroller.ActualHeight / image.PixelHeight);
            SetZoom(Math.Min(fit * 0.98, 1.0));
        }

        private static readonly double[] ZoomSteps =
            { 0.05, 0.1, 0.25, 0.33, 0.5, 0.67, 0.75, 1, 1.5, 2, 3, 4, 6, 8, 12, 16, 24, 32 };

        private void StepZoom(int dir, Point? anchor = null)
        {
            double next = dir > 0
                ? ZoomSteps.FirstOrDefault(z => z > zoom + 1e-6, MaxZoom)
                : ZoomSteps.LastOrDefault(z => z < zoom - 1e-6, MinZoom);
            SetZoom(next, anchor);
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e) => StepZoom(+1);
        private void ZoomOut_Click(object sender, RoutedEventArgs e) => StepZoom(-1);
        private void ZoomActual_Click(object sender, RoutedEventArgs e) => SetZoom(1);
        private void ZoomFit_Click(object sender, RoutedEventArgs e) => ZoomToFit();

        private void Scroller_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;
            StepZoom(e.Delta > 0 ? +1 : -1, e.GetPosition(Scroller));
            e.Handled = true;
        }

        private void Scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            TopRuler.Offset = Scroller.HorizontalOffset;
            LeftRuler.Offset = Scroller.VerticalOffset;
        }

        private void UpdateSliceText()
        {
            if (image == null) { SliceText.Text = "分割数: -"; return; }
            int rows = guides.GetCuts(true).Count - 1;
            int cols = guides.GetCuts(false).Count - 1;
            SliceText.Text = $"分割数: {rows} 行 × {cols} 列 = {rows * cols} 枚";
        }

        private bool CheckImageLoaded()
        {
            if (image != null) return true;
            ShowError("画像が読み込まれていません");
            return false;
        }

        private static void ShowError(string message)
            => MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
    }
}
