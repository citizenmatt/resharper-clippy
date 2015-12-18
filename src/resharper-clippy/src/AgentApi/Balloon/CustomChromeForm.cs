using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JetBrains.Interop.WinApi;
using JetBrains.Interop.WinApi.Constants;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    public abstract class CustomChromeForm : Form
    {
        // ReSharper disable InconsistentNaming
        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_NCHITTEST = 0x0084;

        private const int DCX_WINDOW = 0x1;
        private const int DCX_INTERSECTRGN = 0x80;
        // ReSharper restore InconsistentNaming

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCCALCSIZE:
                    ProcessWmNcCalcSize(ref m);
                    break;

                case WM_NCPAINT:
                    ProcessWmNcPaint(ref m);
                    break;

                case WM_NCHITTEST:
                    ProcessWmNcHitTest(ref m);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void ProcessWmNcCalcSize(ref Message message)
        {
            message.Result = IntPtr.Zero;

            var rc = (RECT) Marshal.PtrToStructure(message.LParam, typeof (RECT));
            rc = OnCalculateNonClientSize(rc);
            Marshal.StructureToPtr(rc, message.LParam, false);
        }

        private void ProcessWmNcPaint(ref Message message)
        {
            // GetDCEx usually returns 0, so just get the windows DC
            var hdc = GetDCEx(message.HWnd, message.WParam, DCX_WINDOW | DCX_INTERSECTRGN);
            if (hdc == IntPtr.Zero)
                hdc = GetWindowDC(message.HWnd);

            try
            {
                using (var graphics = Graphics.FromHdc(hdc))
                {
                    var windowRectangle = new Rectangle(0, 0, Width, Height);
                    var clientRectangle = OnCalculateNonClientSize(windowRectangle);
                    graphics.ExcludeClip(clientRectangle);
                    OnPaintNonClient(graphics, windowRectangle);
                }
            }
            finally
            {
                ReleaseDC(message.HWnd, hdc);
            }
        }

        private void ProcessWmNcHitTest(ref Message message)
        {
            base.WndProc(ref message);

            message.Result = (IntPtr) OnNonClientHitTest((HitTestResult) message.Result);
        }

        protected abstract Rectangle OnCalculateNonClientSize(Rectangle windowRectangle);
        protected abstract void OnPaintNonClient(Graphics graphics, Rectangle windowRectangle);
        protected abstract HitTestResult OnNonClientHitTest(HitTestResult result);

        [DllImport("USER32.dll")]
        private static extern IntPtr GetDCEx(IntPtr hWnd, IntPtr hrgnClip, int flags);

        [DllImport("USER32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hwnd);

        [DllImport("USER32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);
    }
}