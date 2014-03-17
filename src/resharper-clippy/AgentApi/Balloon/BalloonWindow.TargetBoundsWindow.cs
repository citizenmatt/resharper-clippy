using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JetBrains.UI.Avalon;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
#if DEBUG
    public partial class BalloonWindow
    {
        private Window boundsWindow;

        partial void CreateTargetBoundsWindow()
        {
            boundsWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                Topmost = true,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowActivated = false
            };
            var grid = new Grid();
            grid.AddChild(new Border
            {
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            });
            boundsWindow.Content = grid;
        }

        partial void UpdateTargetBoundsWindow()
        {
            boundsWindow.Top = targetBounds.Top;
            boundsWindow.Left = targetBounds.Left;
            boundsWindow.Width = targetBounds.Width;
            boundsWindow.Height = targetBounds.Height;
            if (Math.Abs(targetBounds.Width) > double.Epsilon && Math.Abs(targetBounds.Height) > double.Epsilon)
                boundsWindow.Show();
        }

        partial void HideTargetBoundsWindow()
        {
            boundsWindow.Hide();
        }
    }
#endif
}
