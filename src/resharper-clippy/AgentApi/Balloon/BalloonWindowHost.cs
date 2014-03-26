using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using JetBrains.Interop.WinApi;
using Key = System.Windows.Input.Key;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    // Based on code from http://www.codeproject.com/Articles/2681/Balloon-Windows-for-NET
    public class BalloonWindowHost : CustomChromeForm
    {
        private const int BalloonMargin = 10;
        private const int TailLength = 37;
        private const int CornerRadius = 15;

        private GraphicsPath balloonPath;
        private bool showWithoutActivation;
        private Rectangle anchorBounds;
        private TailLocation tailLocation = TailLocation.Bottom;

        private enum TailLocation
        {
            Left, Bottom, Right
        }

        // ReSharper disable DoNotCallOverridableMethodsInConstructor
        public BalloonWindowHost(BalloonWindow balloonWindow)
        {
            SuspendLayout();

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowOnly;
            BackColor = Color.FromArgb(255, 255, 0xCD);
            ForeColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(0);
            ShowInTaskbar = false;
            Size = new Size(10, 10);    // We need a small size so that GrowOnly doesn't give us a window with too large a default
            //TopMost = true;   // We can't set this, as it overrides ShowWithoutActivation
            Visible = false;

            var host = new ElementHost
            {
                AutoSize = true,
                Child = balloonWindow,
                Dock = DockStyle.Fill,
                Margin = Margin,
                MinimumSize = new Size(50, 10)
            };
            balloonWindow.KeyUp += balloonWindow_KeyUp;
            Controls.Add(host);

            ResumeLayout();
        }
        // ReSharper restore DoNotCallOverridableMethodsInConstructor

        void balloonWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        public void Show(IWin32Window owner, bool showActivated)
        {
            showWithoutActivation = !showActivated;
            Show(owner);
            //TopMost = true;   // Causes owning window to go BACK in the z-order?
        }

        protected override bool ShowWithoutActivation
        {
            get { return showWithoutActivation; }
        }

        protected override void OnLoad(EventArgs e)
        {
            UpdateRegion();
            base.OnLoad(e);
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateRegion();
            base.OnResize(e);
        }

        protected override Rectangle OnCalculateNonClientSize(Rectangle windowRectangle)
        {
            // Amusingly, WM_CALCNCSIZE actually wants to get the size of the client
            // area, not the non-client area
            windowRectangle.Inflate(-BalloonMargin, -BalloonMargin);

            switch (tailLocation)
            {
                case TailLocation.Left:
                    windowRectangle.X += TailLength;
                    windowRectangle.Width -= TailLength;
                    break;

                case TailLocation.Right:
                    windowRectangle.Width -= TailLength;
                    break;

                case TailLocation.Bottom:
                    windowRectangle.Height -= TailLength;
                    break;
            }

            return windowRectangle;
        }

        protected override HitTestResult OnNonClientHitTest(HitTestResult result)
        {
            return result != HitTestResult.HTCLIENT ? HitTestResult.HTNOWHERE : result;
        }

        protected override void OnPaintNonClient(Graphics graphics, Rectangle windowRectangle)
        {
            using (var b = new SolidBrush(BackColor))
                graphics.FillRectangle(b, windowRectangle);

            using(var p = new Pen(ForeColor, 2))
            {
                p.Alignment = PenAlignment.Inset;
                graphics.DrawPath(p, balloonPath);
            }
        }

        protected override Size SizeFromClientSize(Size clientSize)
        {
            return SizeFromClientSize(clientSize, tailLocation);
        }

        private Size SizeFromClientSize(Size clientSize, TailLocation tail)
        {
            // This would normally adjust the size based on the border styles. Since
            // we don't have a system border, adjust for our non-client area
            var rc = new Rectangle(new Point(0, 0), clientSize);
            rc.Inflate(BalloonMargin, BalloonMargin);
            switch (tail)
            {
                case TailLocation.Left:
                    rc.Width += TailLength;
                    break;
                case TailLocation.Bottom:
                    rc.Height += TailLength;
                    break;
                case TailLocation.Right:
                    rc.Width += TailLength;
                    break;
            }
            return rc.Size;
        }

        public void SetAnchorBounds(int x, int y, int w, int h)
        {
            anchorBounds = new Rectangle(x, y, w, h);
            UpdateRegion();
        }

        private void UpdateLocation()
        {
            var screen = Screen.FromControl(this);

            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var clientSize = ClientRectangle.Size;
            if (CanBalloonFitOnTopOfTarget(clientSize, screen))
            {
                tailLocation = TailLocation.Bottom;

                var windowSize = SizeFromClientSize(clientSize, tailLocation);
                var x = anchorBounds.Left - ((windowSize.Width - anchorBounds.Width)/2);
                x = Math.Max(x, screen.Bounds.Left);
                x = Math.Min(x, screen.Bounds.Right - windowSize.Width);
                SetBounds(x, anchorBounds.Top - windowSize.Height, windowSize.Width, windowSize.Height);
            }
            else if (CanBalloonFitOnRightOfTarget(clientSize, screen))
            {
                tailLocation = TailLocation.Left;

                var windowSize = SizeFromClientSize(clientSize, tailLocation);
                var y = anchorBounds.Top - ((windowSize.Height - anchorBounds.Height)/2);
                y = Math.Max(y, screen.Bounds.Top);
                y = Math.Min(y, screen.Bounds.Bottom - windowSize.Height);
                SetBounds(anchorBounds.Right, y, windowSize.Width, windowSize.Height);
            }
            else
            {
                tailLocation = TailLocation.Right;

                var windowSize = SizeFromClientSize(clientSize, tailLocation);
                var y = anchorBounds.Top - ((windowSize.Height - anchorBounds.Height) / 2);
                y = Math.Max(y, screen.Bounds.Top);
                y = Math.Min(y, screen.Bounds.Bottom - windowSize.Height);
                SetBounds(anchorBounds.Left - windowSize.Width, y, windowSize.Width, windowSize.Height);
            }

            AutoSizeMode = AutoSizeMode.GrowOnly;
        }

        private bool CanBalloonFitOnTopOfTarget(Size clientSize, Screen screen)
        {
            var windowSize = SizeFromClientSize(clientSize, TailLocation.Bottom);
            return anchorBounds.Top - windowSize.Height > screen.Bounds.Top;
        }

        private bool CanBalloonFitOnRightOfTarget(Size clientSize, Screen screen)
        {
            var windowSize = SizeFromClientSize(clientSize, TailLocation.Left);
            return anchorBounds.Right + windowSize.Width < screen.Bounds.Right;
        }

        private void UpdateRegion()
        {
            if (!IsHandleCreated)
                return;

            UpdateLocation();

            var balloonBounds = new Rectangle(Point.Empty, Size);

            switch (tailLocation)
            {
                case TailLocation.Left:
                    balloonBounds.X += TailLength;
                    balloonBounds.Width -= TailLength;
                    break;
                case TailLocation.Bottom:
                    balloonBounds.Height -= TailLength;
                    break;
                case TailLocation.Right:
                    balloonBounds.Width -= TailLength;
                    break;
            }


            var radius = Math.Min(CornerRadius, balloonBounds.Height / 2);

            var figure = new GeometryPathFigure(balloonBounds.Left + radius, balloonBounds.Top);
            figure.LineTo(balloonBounds.Right - radius, balloonBounds.Top);
            figure.AddArc(-radius, 0, radius, 270, 90);
            InsertBalloonTailRight(figure, balloonBounds);
            figure.LineTo(balloonBounds.Right, balloonBounds.Bottom - radius);
            figure.AddArc(-(radius*2), -radius, radius, 0, 90);
            InsertBalloonTailBottom(figure, balloonBounds);
            figure.LineTo(balloonBounds.Left + radius, balloonBounds.Bottom);
            figure.AddArc(-radius, -(radius*2), radius, 90, 90);
            InsertBalloonTailLeft(figure, balloonBounds);
            figure.LineTo(balloonBounds.Left, balloonBounds.Top + radius);
            figure.AddArc(0, -radius, radius, 180, 90);

            if (balloonPath != null)
            {
                balloonPath.Dispose();
                balloonPath = null;
            }

            balloonPath = figure.GetPath();

            Region = new Region(balloonPath);
        }

        private void InsertBalloonTailRight(GeometryPathFigure figure, Rectangle bounds)
        {
            if (tailLocation != TailLocation.Right)
                return;

            // TODO: Tail location needs to remain close to anchor point
            var tailStart = bounds.Height / 2;
            var tailEnd = tailStart + (int) (TailLength/1.5);
            figure.LineTo(bounds.Right, tailStart);
            figure.LineTo(bounds.Right + TailLength, tailStart);
            figure.LineTo(bounds.Right, tailEnd);
        }

        private void InsertBalloonTailBottom(GeometryPathFigure figure, Rectangle bounds)
        {
            if (tailLocation != TailLocation.Bottom)
                return;

            // TODO: Tail location needs to remain close to anchor point
            var tailStart = bounds.Width / 2;
            var tailEnd = tailStart - (int)(TailLength/1.5);
            figure.LineTo(tailStart, bounds.Bottom);
            figure.LineTo(tailStart, bounds.Bottom + TailLength);
            figure.LineTo(tailEnd, bounds.Bottom);
        }

        private void InsertBalloonTailLeft(GeometryPathFigure figure, Rectangle bounds)
        {
            if (tailLocation != TailLocation.Left)
                return;

            // TODO: Tail location needs to remain close to anchor point
            var tailStart = bounds.Height / 2;
            var tailEnd = tailStart - (int) (TailLength/1.5);
            figure.LineTo(bounds.Left, tailStart);
            figure.LineTo(bounds.Left - TailLength, tailStart);
            figure.LineTo(bounds.Left, tailEnd);
        }

        private class GeometryPathFigure
        {
            private readonly GraphicsPath path;
            private int lastX;
            private int lastY;

            public GeometryPathFigure(int startX, int startY)
            {
                path = new GraphicsPath(FillMode.Alternate);
                path.StartFigure();
                lastX = startX;
                lastY = startY;
            }

            public void LineTo(int x, int y)
            {
                path.AddLine(lastX, lastY, x, y);
                lastX = x;
                lastY = y;
            }

            public void AddArc(int xoffset, int yoffset, int radius, int startAngle, int sweepAngle)
            {
                var diameter = radius * 2;
                path.AddArc(lastX + xoffset, lastY + yoffset, diameter, diameter, startAngle, sweepAngle);

                var centreX = lastX + xoffset + radius;
                var centreY = lastY + yoffset + radius;
                var angleInRadians = (startAngle + sweepAngle)*(Math.PI/180.0);
                lastX = centreX + (int)(radius*Math.Cos(angleInRadians));
                lastY = centreY + (int)(radius*Math.Sin(angleInRadians));
            }

            public GraphicsPath GetPath()
            {
                path.CloseFigure();
                return path;
            }
        }
    }
}
  