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
        private List<Border> lines = new List<Border>();

        private bool isDragging = false;
        private Border? currentLine;
        private Point clickPosition;

        public GuideLineManager(Canvas canvas)
        {
            this.canvas = canvas;
        }

        private Border CreateLine(double y)
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

        public void AddLine()
        {
            var line = CreateLine(100);

            lines.Add(line);
            canvas.Children.Add(line);
        }

        public void RemoveTopLine()
        {
            if (lines.Count == 0) return;

            var lowest = lines.OrderBy(x => Canvas.GetTop(x)).First();

            canvas.Children.Remove(lowest);
            lines.Remove(lowest);
        }

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

            double newY = position.Y - clickPosition.Y;
            newY = Math.Clamp(newY, 20, 300);

            Canvas.SetTop(currentLine, newY);
        }

        private void Line_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (currentLine == null) return;

            isDragging = false;
            currentLine.ReleaseMouseCapture();
        }
    }
}