using System;
using System.Windows.Forms;
using JetBrains.Interop.WinApi.Interfaces;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    public class OleWin32Window : IWin32Window
    {
        private OleWin32Window(IntPtr handle)
        {
            Handle = handle;
        }

        public static IWin32Window FromIOleWindow(object o)
        {
            var handle = IntPtr.Zero;
            var oleWindow = o as IOleWindow;
            if (oleWindow != null)
                handle = oleWindow.GetWindow();
            return new OleWin32Window(handle);
        }

        public IntPtr Handle { get; private set; }
    }
}