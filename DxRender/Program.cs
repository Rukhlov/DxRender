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

            RenderMode RenderMode = RenderMode.SlimDX;
            SourceMode SourceMode = SourceMode.DSCap;

            int TestId = 0;
            foreach (string arg in args)
            {
                int Value = 0;
                if (CommandLine.GetCommandLineValue(arg, CommandLine.CaptureDevice, out Value))
                    CaptureDevice = Value;

                if (CommandLine.GetCommandLineValue(arg, CommandLine.FrameRate, out Value))
                    FrameRate = Value;

                if (CommandLine.GetCommandLineValue(arg, CommandLine.Width, out Value))
                    Width = Value;

                if (CommandLine.GetCommandLineValue(arg, CommandLine.Height, out Value))
                    Height = Value;

                if (CommandLine.GetCommandLineValue(arg, CommandLine.RenderMode, out Value))
                    RenderMode = (RenderMode)Value;

                if (CommandLine.GetCommandLineValue(arg, CommandLine.SourceMode, out Value))
                    SourceMode = (SourceMode)Value;

                if (CommandLine.GetCommandLineValue(arg, CommandLine.TestMode, out Value))
                    TestId = Value;
            }

            if (TestId > 0) Test.CPU2GPU();

            IFrameSource source = null;

            if (SourceMode == SourceMode.Bitmap)
                source = new BitmapSource(Width, Height);
            else
                source = new CaptureSource(CaptureDevice, FrameRate, Width, Height);

            RenderControl control = new RenderControl(source, RenderMode)
            {
                Dock = DockStyle.Fill
            };

            Form form = new Form
            {
                Width = 640,//source.VideoBuffer.Width,
                Height = 480,//source.VideoBuffer.Height,
                BackColor= Color.Black,
                Text = source.Info
            };

            
            form.Controls.Add(control);

            //control.ControlAspectRatio();

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
            public const string SourceMode = "-source=";
            public const string TestMode = "-test=";

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

        class Test
        {
            [Conditional("DEBUG")]
            public static void CPU2GPU()
            {
                NativeMethods.AllocConsole();
                var t = new Thread(() =>
                {
                    var r = new SlimDXRenderer(IntPtr.Zero, new BitmapSource(1920,1080));
                    if (r != null)
                    {
                        Console.WriteLine("CPU2GPU test start...");

                        long len = 0;
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Restart();
                        int count = 500;
                        while (count-- > 0) { len += r.CopyToSurfaceTest(); }
                        long msec = stopwatch.ElapsedMilliseconds;
                        double result = (double)len / (msec / 1000) / (1024 * 1024);

                        Console.WriteLine("Test CPU to GPU copy {0:0.0} Mb/sec", result);

                        r.Dispose();
                    }
                });
                t.Start();
                t.Join();

                Console.WriteLine("Press any key...");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }
    }

    enum RenderMode
    {
        SlimDX = 0,
        GDIPlus = 1,
    }

    enum SourceMode
    {
        DSCap = 1,
        Bitmap = 0,
    }

    abstract class RendererBase : IDisposable
    {
        public RendererBase(IntPtr Handle, IFrameSource FrameSource)
        {
            this.OwnerHandle = Handle;
            this.FrameSource = FrameSource;

            this.Width = FrameSource.VideoBuffer.Width;
            this.Height = FrameSource.VideoBuffer.Height;
            if (Width > 0 && Height > 0)
            {
                float FontSize = (float)Width / Height * 10;
                this.PerfCounter = new PerfCounter(FontSize);
            }
        }

        protected PerfCounter PerfCounter = null;

        protected int Width = 0;
        protected int Height = 0;
        public Rectangle ClientRectangle;

        protected IFrameSource FrameSource = null;
        protected IntPtr OwnerHandle = IntPtr.Zero;

        protected volatile bool ReDrawing = false;

        public abstract void Draw(bool UpdateSurface = true);

        public virtual void SetRectangle(Rectangle Rect)
        {

        }

        public virtual void Execute(string Command, params object [] Parameters)
        {

        }

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
        public IntPtr Ptr { get; set; }
        public int Size { get; set; }
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

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool AllocConsole();

    }
}
