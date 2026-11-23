using System.Windows;
using System.Windows.Controls;

namespace PicSlicer
{
    /// <summary>「縦 n 段 × 横 m 列」を入力する小さなダイアログ</summary>
    public static class EvenGridDialog
    {
        public static (int Rows, int Cols)? Show(Window owner)
        {
            var rowsBox = new TextBox { Text = "2", Width = 60, Margin = new Thickness(4) };
            var colsBox = new TextBox { Text = "2", Width = 60, Margin = new Thickness(4) };
            var ok = new Button { Content = "OK", Width = 70, Margin = new Thickness(4), IsDefault = true };
            var cancel = new Button { Content = "キャンセル", Width = 70, Margin = new Thickness(4), IsCancel = true };

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int i = 0; i < 3; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            void Place(UIElement el, int r, int c) { Grid.SetRow(el, r); Grid.SetColumn(el, c); grid.Children.Add(el); }

            Place(new TextBlock { Text = "行数（縦に何段）:", VerticalAlignment = VerticalAlignment.Center }, 0, 0);
            Place(rowsBox, 0, 1);
            Place(new TextBlock { Text = "列数（横に何列）:", VerticalAlignment = VerticalAlignment.Center }, 1, 0);
            Place(colsBox, 1, 1);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            Place(buttons, 2, 0);
            Grid.SetColumnSpan(buttons, 2);

            var win = new Window
            {
                Title = "均等に分割",
                Content = grid,
                Owner = owner,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
            };

            (int, int)? result = null;
            ok.Click += (_, _) =>
            {
                if (int.TryParse(rowsBox.Text, out int r) && int.TryParse(colsBox.Text, out int c) &&
                    r >= 1 && c >= 1 && r <= 100 && c <= 100)
                {
                    result = (r, c);
                    win.DialogResult = true;
                }
                else
                {
                    MessageBox.Show(win, "1〜100 の整数を入力してください。", "入力エラー",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            win.Loaded += (_, _) => { rowsBox.Focus(); rowsBox.SelectAll(); };
            win.ShowDialog();
            return result;
        }
    }
}
