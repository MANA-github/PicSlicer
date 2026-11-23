using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PicSlicer
{
    /// <summary>分割線1本分。Position は画像のピクセル座標（整数）。</summary>
    public class GuideLine
    {
        public bool IsHorizontal { get; init; }
        public int Position { get; set; }
    }

    /// <summary>
    /// 分割線の管理と描画を担当する。
    /// 位置は常に「画像ピクセル」で保持し、描画時だけ Zoom を掛けて画面座標に変換する。
    /// こうすることで表示倍率を変えても分割位置がずれない。
    /// </summary>
    public class GuideLineManager
    {
        private readonly Canvas canvas;
        private readonly List<GuideLine> lines = new();

        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public double Zoom { get; set; } = 1.0;

        public GuideLine? Selected { get; set; }

        /// <summary>この画面px以内なら線をつかめる</summary>
        public const double HitTolerance = 5;

        public event Action? Changed;

        public GuideLineManager(Canvas canvas)
        {
            this.canvas = canvas;
        }

        public IReadOnlyList<GuideLine> Lines => lines;

        public void SetImageSize(int width, int height)
        {
            ImageWidth = width;
            ImageHeight = height;
            lines.Clear();
            Selected = null;
            NotifyChanged();
        }

        public GuideLine Add(bool isHorizontal, int position)
        {
            var line = new GuideLine { IsHorizontal = isHorizontal, Position = Clamp(isHorizontal, position) };
            lines.Add(line);
            Selected = line;
            NotifyChanged();
            return line;
        }

        public void Remove(GuideLine line)
        {
            lines.Remove(line);
            if (Selected == line) Selected = null;
            NotifyChanged();
        }

        public void Clear()
        {
            lines.Clear();
            Selected = null;
            NotifyChanged();
        }

        /// <summary>横 rows 段 × 縦 cols 列に均等分割する線を作り直す</summary>
        public void SetEvenGrid(int rows, int cols)
        {
            lines.Clear();
            Selected = null;
            for (int i = 1; i < rows; i++)
                lines.Add(new GuideLine { IsHorizontal = true, Position = (int)Math.Round(ImageHeight * i / (double)rows) });
            for (int i = 1; i < cols; i++)
                lines.Add(new GuideLine { IsHorizontal = false, Position = (int)Math.Round(ImageWidth * i / (double)cols) });
            NotifyChanged();
        }

        public void Move(GuideLine line, int position)
        {
            line.Position = Clamp(line.IsHorizontal, position);
            NotifyChanged();
        }

        private int Clamp(bool isHorizontal, int position)
            => Math.Clamp(position, 0, isHorizontal ? ImageHeight : ImageWidth);

        public GuideLine? HitTest(double screenX, double screenY)
        {
            GuideLine? best = null;
            double bestDist = HitTolerance;

            foreach (var line in lines)
            {
                double p = line.Position * Zoom;
                double d = Math.Abs((line.IsHorizontal ? screenY : screenX) - p);
                if (d <= bestDist)
                {
                    best = line;
                    bestDist = d;
                }
            }
            return best;
        }

        /// <summary>境界（0 と端を含む、重複なし・昇順）</summary>
        public List<int> GetCuts(bool isHorizontal)
        {
            int max = isHorizontal ? ImageHeight : ImageWidth;
            return lines.Where(l => l.IsHorizontal == isHorizontal)
                        .Select(l => l.Position)
                        .Append(0).Append(max)
                        .Distinct().OrderBy(v => v).ToList();
        }

        /// <summary>切り出す矩形の一覧（行ごと、左から右）</summary>
        public List<(int Row, int Col, System.Windows.Int32Rect Rect)> GetSlices()
        {
            var ys = GetCuts(true);
            var xs = GetCuts(false);
            var result = new List<(int, int, System.Windows.Int32Rect)>();

            for (int r = 0; r < ys.Count - 1; r++)
                for (int c = 0; c < xs.Count - 1; c++)
                    result.Add((r + 1, c + 1,
                        new System.Windows.Int32Rect(xs[c], ys[r], xs[c + 1] - xs[c], ys[r + 1] - ys[r])));

            return result;
        }

        private void NotifyChanged()
        {
            Render();
            Changed?.Invoke();
        }

        public void Render()
        {
            canvas.Children.Clear();
            double w = ImageWidth * Zoom;
            double h = ImageHeight * Zoom;

            foreach (var line in lines)
            {
                double p = Math.Round(line.Position * Zoom) + 0.5;
                bool selected = line == Selected;

                var shape = new Line
                {
                    Stroke = selected ? Brushes.Orange : (line.IsHorizontal ? Brushes.Red : Brushes.DeepSkyBlue),
                    StrokeThickness = selected ? 2 : 1,
                    IsHitTestVisible = false,
                    SnapsToDevicePixels = true,
                };

                if (line.IsHorizontal)
                {
                    shape.X1 = 0; shape.X2 = w;
                    shape.Y1 = shape.Y2 = p;
                }
                else
                {
                    shape.Y1 = 0; shape.Y2 = h;
                    shape.X1 = shape.X2 = p;
                }

                canvas.Children.Add(shape);
            }
        }
    }
}
