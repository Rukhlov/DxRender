using System;

using System.Windows.Forms;

namespace DxRender
{

    class PlotView : UserControl
    {
        public PlotView(SlimDXRenderer Renderer)
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.DoubleBuffered = false;
            this.Renderer = Renderer;
        }

        private SlimDXRenderer Renderer = null;

        protected override void OnPaint(PaintEventArgs e)
        {
            Renderer.Draw();
        } 
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            //do nothing
        }
    }

    class Program
    {
        static void Main()
        {
            int CaptureDevice = 0;
            int FrameRate = 30;
            int Width = 1920; //1280
            int Height = 1080; //720

            SlimDXRenderer renderer = new SlimDXRenderer();

            //RenderForm form = new RenderForm("TEST_DX_APP");
            Form form = new Form { Width = Width, Height = Height };
            PlotView view = new PlotView(renderer) { Dock = DockStyle.Fill };

            form.Controls.Add(view);

           //IFrameSource source = new BitmapSource();
            IFrameSource source = new CaptureSource(0, 30, Width, Height);

            view.KeyDown += (o, e) =>
            {
                //if (e.KeyCode == Keys.Enter)
                //{
                //    renderer.Start(view.Handle, source);

                //}

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


            renderer.Start(view.Handle, source);

            Application.ApplicationExit += (o, a) => { };
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
