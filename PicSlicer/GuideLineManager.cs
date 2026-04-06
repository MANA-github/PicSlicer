using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfLibrary1
{
    public class GuideLineManager
    {
        private readonly Canvas canvas;

        private readonly List<Border> hLines = new();
        private readonly List<Border> vLines = new();

        private bool isDragging;
        private Border? currentLine;
        private Point clickPosition;

        private int imageWidth;
        private int imageHeight;

        private const double HitArea = 20;
        private const double LineThickness = 1;
        private const double MaxLength = 10000;

        public GuideLineManager(Canvas canvas)
        {
            this.canvas = canvas;
        }

        public void SetImageSize(int width, int height)
        {
            imageWidth = width;
            imageHeight = height;
        }

        // ===============================
        // 共通ライン生成
        // ===============================
        private Border CreateLine(bool isHorizontal, double pos)
        {
            var line = new Border
            {
                Width = isHorizontal ? MaxLength : HitArea,
                Height = isHorizontal ? HitArea : MaxLength,
                Background = Brushes.Transparent,
                Tag = isHorizontal // ← 種別保持（重要）
            };

            var visual = new Border
            {
                Width = isHorizontal ? double.NaN : LineThickness,
                Height = isHorizontal ? LineThickness : double.NaN,
                Background = isHorizontal ? Brushes.Red : Brushes.Blue,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            line.Child = visual;

            if (isHorizontal)
            {
                Canvas.SetLeft(line, 0);
                Canvas.SetTop(line, pos);
            }
            else
            {
                Canvas.SetTop(line, 0);
                Canvas.SetLeft(line, pos);
            }

            AttachEvents(line);
            return line;
        }

        private void AttachEvents(Border line)
        {
            line.MouseLeftButtonDown += Line_MouseDown;
            line.MouseMove += Line_MouseMove;
            line.MouseLeftButtonUp += Line_MouseUp;
        }

        // ===============================
        // 水平ライン
        // ===============================
        public void Add_H_Line()
        {
            var line = CreateLine(true, 100);
            hLines.Add(line);
            canvas.Children.Add(line);
        }

        public void Remove_H_Line()
        {
            RemoveLine(hLines, Canvas.GetTop);
        }

        // ===============================
        // 垂直ライン
        // ===============================
        public void Add_V_Line()
        {
            var line = CreateLine(false, 100);
            vLines.Add(line);
            canvas.Children.Add(line);
        }

        public void Remove_V_Line()
        {
            RemoveLine(vLines, Canvas.GetLeft);
        }

        private void RemoveLine(List<Border> list, System.Func<Border, double> selector)
        {
            if (list.Count == 0) return;

            var target = list.OrderBy(selector).First();
            canvas.Children.Remove(target);
            list.Remove(target);
        }

        // ===============================
        // ドラッグ処理
        // ===============================
        private void Line_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border line) return;

            currentLine = line;
            isDragging = true;
            clickPosition = e.GetPosition(line);
            line.CaptureMouse();
        }

        private void Line_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || currentLine == null) return;

            var pos = e.GetPosition(canvas);
            bool isHorizontal = (bool)currentLine.Tag;

            if (isHorizontal)
            {
                double y = Math.Clamp(pos.Y - clickPosition.Y, 0, imageHeight);
                Canvas.SetTop(currentLine, y);
            }
            else
            {
                double x = Math.Clamp(pos.X - clickPosition.X, 0, imageWidth);
                Canvas.SetLeft(currentLine, x);
            }
        }

        private void Line_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (currentLine == null) return;

            isDragging = false;
            currentLine.ReleaseMouseCapture();
            currentLine = null;
        }

        // ===============================
        // 座標取得（追加）
        // ===============================
        public List<double> GetHorizontalPositions()
        {
            return hLines.Select(x => Canvas.GetTop(x)).ToList();
        }

        public List<double> GetVerticalPositions()
        {
            return vLines.Select(x => Canvas.GetLeft(x)).ToList();
        }
    }
}