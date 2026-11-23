using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PicSlicer
{
    /// <summary>
    /// 画像のピクセル座標を目盛りで表示する定規。
    /// Zoom（表示倍率）と Offset（スクロール量・画面px）に追従して描画する。
    /// </summary>
    public class RulerControl : FrameworkElement
    {
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        private double zoom = 1.0;
        private double offset;
        private double? marker;

        /// <summary>表示倍率（画面px / 画像px）</summary>
        public double Zoom
        {
            get => zoom;
            set { zoom = value; InvalidateVisual(); }
        }

        /// <summary>スクロール量（画面px）</summary>
        public double Offset
        {
            get => offset;
            set { offset = value; InvalidateVisual(); }
        }

        /// <summary>マウス位置（画像px）。null で非表示</summary>
        public double? Marker
        {
            get => marker;
            set { marker = value; InvalidateVisual(); }
        }

        private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
        private static readonly Pen TickPen = new(Brushes.DimGray, 1);
        private static readonly Pen MarkerPen = new(Brushes.Red, 1);
        private static readonly Typeface Font = new("Segoe UI");

        static RulerControl()
        {
            BackgroundBrush.Freeze();
            TickPen.Freeze();
            MarkerPen.Freeze();
        }

        protected override void OnRender(DrawingContext dc)
        {
            bool horizontal = Orientation == Orientation.Horizontal;
            double length = horizontal ? ActualWidth : ActualHeight;
            double thickness = horizontal ? ActualHeight : ActualWidth;

            dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));
            if (zoom <= 0 || length <= 0) return;

            // 主目盛り間隔（画像px）を、画面上で 50px 以上あくように決める
            double step = NiceStep(50 / zoom);
            double minor = step / 10;

            double startPx = offset / zoom;
            double endPx = (offset + length) / zoom;
            long firstIndex = (long)Math.Floor(startPx / minor);
            long lastIndex = (long)Math.Ceiling(endPx / minor);

            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            for (long i = firstIndex; i <= lastIndex; i++)
            {
                double v = i * minor;               // 画像px
                double s = v * zoom - offset;       // 画面上の位置
                bool isMajor = i % 10 == 0;
                bool isMid = i % 5 == 0;
                double tick = isMajor ? thickness : isMid ? thickness * 0.5 : thickness * 0.25;

                // 1px線をくっきり描くため 0.5 ずらす
                double p = Math.Round(s) + 0.5;
                if (horizontal)
                    dc.DrawLine(TickPen, new Point(p, thickness), new Point(p, thickness - tick));
                else
                    dc.DrawLine(TickPen, new Point(thickness, p), new Point(thickness - tick, p));

                if (isMajor)
                {
                    var text = new FormattedText(
                        Math.Round(v).ToString(CultureInfo.InvariantCulture),
                        CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        Font, 9, Brushes.Black, dpi);

                    if (horizontal)
                    {
                        dc.DrawText(text, new Point(p + 2, 0));
                    }
                    else
                    {
                        dc.PushTransform(new RotateTransform(-90, 0, p - 2));
                        dc.DrawText(text, new Point(0, p - 2));
                        dc.Pop();
                    }
                }
            }

            if (marker is double m)
            {
                double p = Math.Round(m * zoom - offset) + 0.5;
                if (horizontal)
                    dc.DrawLine(MarkerPen, new Point(p, 0), new Point(p, thickness));
                else
                    dc.DrawLine(MarkerPen, new Point(0, p), new Point(thickness, p));
            }

            // 画像側との境界線
            if (horizontal)
                dc.DrawLine(TickPen, new Point(0, thickness - 0.5), new Point(ActualWidth, thickness - 0.5));
            else
                dc.DrawLine(TickPen, new Point(thickness - 0.5, 0), new Point(thickness - 0.5, ActualHeight));
        }

        /// <summary>1, 2, 5 × 10^n のうち raw 以上で最小の値</summary>
        private static double NiceStep(double raw)
        {
            double pow = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            foreach (var f in new[] { 1.0, 2.0, 5.0, 10.0 })
                if (f * pow >= raw) return f * pow;
            return 10 * pow;
        }
    }
}
