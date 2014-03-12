using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.SxS
{
    // See http://blog.functionalfun.net/2012/09/a-quick-guide-to-registration-free-com.html
    internal static class ActivationContext
    {
        public static T Using<T>(string manifest, Func<T> action)
        {
            var context = new UnsafeNativeMethods.ACTCTX
            {
                cbSize = Marshal.SizeOf(typeof (UnsafeNativeMethods.ACTCTX)),
                lpSource = manifest
            };

            if (context.cbSize != 0x20)
            {
                throw new Exception("ACTCTX.cbSize is wrong");
            }

            var hActCtx = UnsafeNativeMethods.CreateActCtx(ref context);
            if (hActCtx == (IntPtr) (-1))
            {
                throw new Win32Exception();
            }

            try
            {
                var cookie = IntPtr.Zero;
                if (!UnsafeNativeMethods.ActivateActCtx(hActCtx, out cookie))
                {
                    throw new Win32Exception();
                }

                try
                {
                    return action();
                }
                finally
                {
                    UnsafeNativeMethods.DeactivateActCtx(0, cookie);
                }
            }
            finally
            {
                UnsafeNativeMethods.ReleaseActCtx(hActCtx);
            }
        }

        // ReSharper disable FieldCanBeMadeReadOnly.Global
        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable InconsistentNaming
        [SuppressUnmanagedCodeSecurity]
        internal static class UnsafeNativeMethods
        {
            [DllImport("Kernel32.dll", SetLastError = true, EntryPoint = "CreateActCtxW")]
            internal static extern IntPtr CreateActCtx(ref ACTCTX actctx);

            [DllImport("Kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ActivateActCtx(IntPtr hActCtx, out IntPtr lpCookie);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DeactivateActCtx(int dwFlags, IntPtr lpCookie);

            [DllImport("Kernel32.dll", SetLastError = true)]
            internal static extern void ReleaseActCtx(IntPtr hActCtx);

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            internal struct ACTCTX
            {
                public Int32 cbSize;
                public UInt32 dwFlags;
                public string lpSource;
                public UInt16 wProcessorArchitecture;
                public UInt16 wLangId;
                public string lpAssemblyDirectory;
                public string lpResourceName;
                public string lpApplicationName;
                public IntPtr hModule;
            }
        }

        // ReSharper restore InconsistentNaming
        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper restore FieldCanBeMadeReadOnly.Global
    }
}