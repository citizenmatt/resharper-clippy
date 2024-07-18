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
            var handle = o is IOleWindow oleWindow ? oleWindow.GetWindow() : IntPtr.Zero;
            return new OleWin32Window(handle);
        }

        public IntPtr Handle { get; }
    }
}