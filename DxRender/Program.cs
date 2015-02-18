using System;

using System.Windows.Forms;

namespace DxRender
{
    class Program
    {
        static void Main()
        {
            int CaptureDevice = 0;
            int FrameRate = 30;
            int Width = 640; //1280
            int Height = 480; //720

            SlimDXRenderer renderer = new SlimDXRenderer();

            //RenderForm form = new RenderForm("TEST_DX_APP");
            Form form = new Form { Width = Width, Height = Height };
            PlotView view = new PlotView(renderer) { Dock = DockStyle.Fill };

            form.Controls.Add(view);

           //IFrameSource source = new BitmapSource();
            IFrameSource source = new CaptureSource(CaptureDevice, FrameRate, Width, Height);


            view.KeyDown += (o, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    form.Close();

                if (e.KeyCode == Keys.Space)
                {
                    if (source != null)
                        source.Pause();
                }
            };

            form.FormClosing += (o, e) =>
            {
                if (source != null)
                    source.Stop();
            };


            renderer.Setup(view.Handle, source);
            source.Start();

            Application.ApplicationExit += (o, a) =>
            {
                if (renderer != null)
                {
                    renderer.Dispose();
                    renderer = null;
                }
            };
            Application.Run(form);
        }
    }

    interface IFrameSource
    {
        void Start();
        void Pause();
        void Stop();
        MemoryBuffer VideoBuffer { get; }
        event EventHandler<FrameReceivedEventArgs> FrameReceived;
    }

    class FrameReceivedEventArgs : EventArgs
    {
        public double SampleTime { get; set; }
    }

    class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [System.Runtime.InteropServices.DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(IntPtr destination, IntPtr source, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I4)] int length);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

    }
}
