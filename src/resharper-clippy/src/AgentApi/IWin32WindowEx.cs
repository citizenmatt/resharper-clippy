using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JetBrains.Extension;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    // ReSharper disable InconsistentNaming
    public static class IWin32WindowEx
    {
        public static void SetOwner(this IWin32Window window, IWin32Window newOwner)
        {
            if (!window.IsValidWindow() || !newOwner.IsValidWindow())
                return;

            // Yes, it says parent, but it actually sets the owner. Top level windows
            // have owners, child windows have parents. Owned top level windows minimise
            // with their owners
            SetWindowLong(window.Handle, GWLP_HWNDPARENT, newOwner.Handle);
        }

        private const int GWLP_HWNDPARENT = -8;

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }
    // ReSharper restore InconsistentNaming
}