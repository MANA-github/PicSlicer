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
        private Canvas canvas;

        private List<Border> hLines = new List<Border>();
        private List<Border> vLines = new List<Border>();

        private bool isDragging = false;
        private Border? currentLine;
        private Point clickPosition;

        public GuideLineManager(Canvas canvas)
        {
            this.canvas = canvas;
        }

        // ===============================
        // 水平ライン
        // ===============================

        private Border CreateHorizontalLine(double y)
        {
            Border line = new Border
            {
                Width = 10000,
                Height = 20,
                Background = Brushes.Transparent
            };

            Border visual = new Border
            {
                Height = 1,
                Background = Brushes.Red,
                VerticalAlignment = VerticalAlignment.Center
            };

            line.Child = visual;

            Canvas.SetLeft(line, 0);
            Canvas.SetTop(line, y);

            line.MouseLeftButtonDown += Line_MouseDown;
            line.MouseMove += Line_MouseMove;
            line.MouseLeftButtonUp += Line_MouseUp;

            return line;
        }

        public void Add_H_Line()
        {
            var line = CreateHorizontalLine(100);

            hLines.Add(line);
            canvas.Children.Add(line);
        }

        public void Remove_H_Line()
        {
            if (hLines.Count == 0) return;

            var lowest = hLines.OrderBy(x => Canvas.GetTop(x)).First();

            canvas.Children.Remove(lowest);
            hLines.Remove(lowest);
        }

        // ===============================
        // 垂直ライン
        // ===============================

        private Border CreateVerticalLine(double x)
        {
            Border line = new Border
            {
                Width = 20,
                Height = 10000,
                Background = Brushes.Transparent
            };

            Border visual = new Border
            {
                Width = 1,
                Background = Brushes.Blue,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            line.Child = visual;

            Canvas.SetTop(line, 0);
            Canvas.SetLeft(line, x);

            line.MouseLeftButtonDown += Line_MouseDown;
            line.MouseMove += Line_MouseMove;
            line.MouseLeftButtonUp += Line_MouseUp;

            return line;
        }

        public void Add_V_Line()
        {
            var line = CreateVerticalLine(100);

            vLines.Add(line);
            canvas.Children.Add(line);
        }

        public void Remove_V_Line()
        {
            if (vLines.Count == 0) return;

            var leftMost = vLines.OrderBy(x => Canvas.GetLeft(x)).First();

            canvas.Children.Remove(leftMost);
            vLines.Remove(leftMost);
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

            var position = e.GetPosition(canvas);

            if (hLines.Contains(currentLine))
            {
                double newY = position.Y - clickPosition.Y;
                newY = Math.Clamp(newY, 20, 300);
                Canvas.SetTop(currentLine, newY);
            }
            else if (vLines.Contains(currentLine))
            {
                double newX = position.X - clickPosition.X;
                newX = Math.Clamp(newX, 20, 300);
                Canvas.SetLeft(currentLine, newX);
            }
        }

        private void Line_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (currentLine == null) return;

            isDragging = false;
            currentLine.ReleaseMouseCapture();
        }
    }
}