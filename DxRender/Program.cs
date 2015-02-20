using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Diagnostics;

namespace DxRender
{
    class Program
    {
        static void Main(string[] args)
        {
            int CaptureDevice = 0;
            int FrameRate = 30;
            int Width = 1920;
            int Height = 1080;

            RenderMode RenderMode = DxRender.RenderMode.SlimDX;
            bool TestMode = false;

            foreach (string arg in args)
            {
                int value = 0;
                if (CommandLine.GetCommandLineValue(arg, CommandLine.CaptureDevice, out value))
                    CaptureDevice = value;

                if (CommandLine.GetCommandLineValue(arg, CommandLine.FrameRate, out value))
                    FrameRate = value;

                if (CommandLine.GetCommandLineValue(arg, CommandLine.Width, out value))
                    Width = value;

                if (CommandLine.GetCommandLineValue(arg, CommandLine.Height, out value))
                    Height = value;

                if (CommandLine.GetCommandLineValue(arg, CommandLine.RenderMode, out value))
                    RenderMode = (RenderMode)value;

            }


            if (RenderMode == RenderMode.Test)
            {
                Stopwatch stopwatch =new Stopwatch();
                var r = new SlimDXRenderer(IntPtr.Zero, new BitmapSource());
                if (r != null)
                {
                    long len = 0;
                    stopwatch.Restart();
                    int count = 10000;
                    while (count-- > 0)
                    {
                        len+=r.Test();
                    }
                    long msec = stopwatch.ElapsedMilliseconds;
                    double result = (double)len / (msec / 1000) / (1024 * 1024);

                    Console.WriteLine("Test CPU->GPU copy {0:0.0} Mb/sec", result);

                    r.Dispose();

                    return;
                }
            }

            //IFrameSource source = new BitmapSource();
            IFrameSource source = new CaptureSource(CaptureDevice, FrameRate, Width, Height);
            RenderControl control = new RenderControl(source, RenderMode) 
            { 
                Dock = DockStyle.Fill 
            };

            Form form = new Form 
            { 
                Width = source.VideoBuffer.Width, 
                Height = source.VideoBuffer.Height, 
                Text = source.Info 
            };

            form.Controls.Add(control);

            form.FormClosing += (o, e) =>
            {
                if (source != null)
                    source.Stop();
            };
            source.Start();           

            Application.ApplicationExit += (o, a) => { };
            Application.Run(form);
        }

        class CommandLine
        {
            public const string Width = "-w=";
            public const string Height = "-h=";
            public const string FrameRate = "-fps=";
            public const string CaptureDevice = "-device=";
            public const string RenderMode = "-render=";

            public static bool GetCommandLineValue(string Param, string Command, out int Value)
            {
                bool Result = false;
                Value = 0;
                Param = Param.Trim();
                if (Param.StartsWith(Command, StringComparison.CurrentCultureIgnoreCase))
                {
                    string SubStr = Param.Substring(Command.Length, Param.Length - Command.Length);
                    if (string.IsNullOrEmpty(SubStr) == false)
                        Result = int.TryParse(SubStr, out Value);
                }
                return Result;
            }
        }
    }

    enum RenderMode
    {
        SlimDX = 0,
        GDIPlus = 1,
        Test=2,
    }

    abstract class RendererBase : IDisposable
    {
        public RendererBase(IntPtr Handle, IFrameSource FrameSource)
        {
            this.OwnerHandle = Handle;
            this.FrameSource = FrameSource;

            this.Width = FrameSource.VideoBuffer.Width;
            this.Height = FrameSource.VideoBuffer.Height;
            this.PerfCounter = new PerfCounter();
        }

        protected PerfCounter PerfCounter = null;

        protected int Width = 0;
        protected int Height = 0;
        public Rectangle ClientRectangle;

        protected IFrameSource FrameSource = null;
        protected IntPtr OwnerHandle = IntPtr.Zero;

        protected volatile bool ReDrawing = false;

        public abstract void Draw(bool UpdateSurface = true);

        public virtual void Dispose()
        {
            if (PerfCounter != null)
            {
                PerfCounter.Dispose();
                PerfCounter = null;
            }
        }
    }

    interface IFrameSource
    {
        void Start();
        void Pause();
        void Stop();
        MemoryBuffer VideoBuffer { get; }
        event EventHandler<FrameReceivedEventArgs> FrameReceived;

        string Info { get; }
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
