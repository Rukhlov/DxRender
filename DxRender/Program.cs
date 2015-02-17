using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.Windows;
using WebCamService;
using GDI = System.Drawing;

namespace DeviceCreation
{
    class Program
    {
        static void Main()
        {
            RenderForm form = new RenderForm("TEST_DX_APP");
            SlimDXPresenter presenter = new SlimDXPresenter();
            //IFrameSource source = new BitmapSource();
            CaptureSource source = new CaptureSource(0, 0, 1920, 1080);
            //CaptureSource source = new CaptureSource(0, 0, 800, 600);
            presenter.Start(form, source);

            Application.ApplicationExit += (o, a) =>
            {
                source.Dispose();
                presenter.Dispose();            
            };
            Application.Run(form);
        }
    }

    public interface IFrameSource
    {
        void Start();
        MemoryBuffer VideoBuffer { get; }
        event EventHandler FrameRecieved; 
    }


    class NativeMethods
    {
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(IntPtr destination, IntPtr source, [MarshalAs(UnmanagedType.I4)] int length);
    }
}
