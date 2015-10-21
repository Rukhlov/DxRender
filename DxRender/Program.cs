using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

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


            //RenderControl control = new RenderControl(source, RenderMode)
            //{
            //    Dock = DockStyle.Fill
            //};

            RendererForm form = new RendererForm(source, RenderMode)
            {
                Width = 640,//source.VideoBuffer.Width,
                Height = 480,//source.VideoBuffer.Height,
                BackColor= Color.Black,
                Text = source.Info
            };

            
            //form.Controls.Add(control);

           // RendererBase renderer = new SlimDXRenderer(form.Handle, source);

            //form.FormClosing += (o, e) =>
            //{
            //    if (source != null)
            //        source.Stop();
            //};

            //form.KeyUp += (o, e) =>
            //{
            //    if (e.KeyCode == Keys.R)
            //        renderer.Execute("ChangeAspectRatio", true);
            //    if (e.KeyCode == Keys.F)
            //        renderer.Execute("ChangeFullScreen", true);
            //};

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

    public enum RenderMode
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

    public interface IFrameSource
    {
        void Start();
        void Pause();
        void Stop();
        MemoryBuffer VideoBuffer { get; }
        event EventHandler<FrameReceivedEventArgs> FrameReceived;

        string Info { get; }
    }

    public class FrameReceivedEventArgs : EventArgs
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

    class GlobalMouse
    {
        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr HookID = IntPtr.Zero;

        public static event MouseEventHandler MouseMove;
        public static event MouseEventHandler MouseDown;
        public static event MouseEventHandler MouseUp;

        public static event MouseEventHandler MouseWheel;

        private static void OnMouseDown(MouseEventArgs MouseArgs)
        {
            if (MouseDown != null)
                MouseDown(null, MouseArgs);
        }
        private static void OnMouseUp(MouseEventArgs MouseArgs)
        {
            if (MouseUp != null)
                MouseUp(null, MouseArgs);
        }
        private static void OnMouseMove(MouseEventArgs MouseArgs)
        {
            if (MouseMove != null)
                MouseMove(null, MouseArgs);
        }
        private static void OnMouseWheel(MouseEventArgs MouseArgs)
        {
            if (MouseWheel != null)
                MouseWheel(null, MouseArgs);
        }


        public static void Start()
        {
            HookID = SetHook(_proc);
        }
        public static void Stop()
        {
            UnhookWindowsHookEx(HookID);
        }


        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                    MouseEventArgs args = new MouseEventArgs(MouseButtons.Left, 0, hookStruct.pt.x, hookStruct.pt.y, 0);
                    OnMouseDown(args);
                }
                if (MouseMessages.WM_LBUTTONUP == (MouseMessages)wParam)
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));


                    MouseEventArgs args = new MouseEventArgs(MouseButtons.Left, 0, hookStruct.pt.x, hookStruct.pt.y, 0);
                    OnMouseUp(args);
                }

                if (MouseMessages.WM_RBUTTONDOWN == (MouseMessages)wParam)
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                    MouseEventArgs args = new MouseEventArgs(MouseButtons.Right, 0, hookStruct.pt.x, hookStruct.pt.y, 0);
                    OnMouseDown(args);
                }
                if (MouseMessages.WM_RBUTTONUP == (MouseMessages)wParam)
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));


                    MouseEventArgs args = new MouseEventArgs(MouseButtons.Right, 0, hookStruct.pt.x, hookStruct.pt.y, 0);
                    OnMouseUp(args);
                }

                if (MouseMessages.WM_MOUSEMOVE == (MouseMessages)wParam)
                {
                    MSLLHOOKSTRUCT HookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                    MouseEventArgs args = new MouseEventArgs(MouseButtons.None, 0, HookStruct.pt.x, HookStruct.pt.y, 0);
                    OnMouseMove(args);
                }
            }

            return CallNextHookEx(HookID, nCode, wParam, lParam);
        }

        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
