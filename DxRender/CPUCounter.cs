using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

using System.Runtime.InteropServices.ComTypes;

namespace DxRender
{
    class PerfCounter : IDisposable
    {
        public PerfCounter()
        {
            _PerfCounter();
            Styler = new PerfStyler();
        }

        public PerfCounter(float FontSize)
        {
            _PerfCounter();
            Styler = new PerfStyler(FontSize);
        }

        private void _PerfCounter()
        {
            stopwatch = new Stopwatch();
            counter = new CPUCounter();

            timer = new Timer((o) =>
            {
                CPU = counter.GetUsage();
            },
            null, 0, 1000);
        }

        public PerfStyler Styler { get; set; }
        private double FPS = 0;
        private double FPS2 = 0;
        private short CPU = 0;

        private CPUCounter counter = null;
        private Timer timer = null;
        private Stopwatch stopwatch = null;

        private double PrevSampleTime = 0;

        public void UpdateStatistic(double SampleTime)
        {
            long TimerEllapsed = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            if (TimerEllapsed > 0)
                FPS = 1000.0 / TimerEllapsed;

            double SampleTimeEllapsed = SampleTime - PrevSampleTime;
            if (SampleTimeEllapsed > 0)
                FPS2 = 1.0 / SampleTimeEllapsed;
            PrevSampleTime = SampleTime;
        }

        public string GetReport()
        {
            return string.Format("FPS={0:0.0} FPS2={1:0.0} CPU={2}%", FPS, FPS2, CPU);
        }

        public void Dispose()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            if (counter != null)
            {
                counter.Dispose();
                counter = null;
            }

            if (Styler != null)
            {
                Styler.Dispose();
                Styler = null;
            }
        }

        public class PerfStyler : IDisposable
        {
            public PerfStyler()
            {
                Color = System.Drawing.Color.Red;
                Font = new System.Drawing.Font("Arial", 14f, System.Drawing.FontStyle.Regular);
                Brush = new System.Drawing.SolidBrush(Color);
            }

            public PerfStyler(float FontSize)
            {
                Color = System.Drawing.Color.Red;
                Font = new System.Drawing.Font("Arial", FontSize, System.Drawing.FontStyle.Regular);
                Brush = new System.Drawing.SolidBrush(Color);
            }

            public System.Drawing.Font Font { get; private set; }
            public System.Drawing.Brush Brush { get; private set; }
            public System.Drawing.Color Color { get; private set; }

            public void Dispose()
            {
                if (Font != null)
                {
                    Font.Dispose();
                    Font = null;
                }

                if (Brush != null)
                {
                    Brush.Dispose();
                    Brush = null;
                }
            }
        }

        class CPUCounter : IDisposable
        {

            FILETIME PrevSysKernel;
            FILETIME PrevSysUser;

            TimeSpan PrevProcTotal;

            Int16 CPUUsage;
            DateTime LastRun;
            long RunCount;

            Process process;

            public CPUCounter()
            {
                CPUUsage = -1;
                LastRun = DateTime.MinValue;
                PrevSysUser.dwHighDateTime = PrevSysUser.dwLowDateTime = 0;
                PrevSysKernel.dwHighDateTime = PrevSysKernel.dwLowDateTime = 0;
                PrevProcTotal = TimeSpan.MinValue;
                RunCount = 0;

                process = Process.GetCurrentProcess();
            }

            public short GetUsage()
            {
                short CPUCopy = CPUUsage;
                if (Interlocked.Increment(ref RunCount) == 1)
                {
                    if (!EnoughTimePassed)
                    {
                        Interlocked.Decrement(ref RunCount);
                        return CPUCopy;
                    }

                    FILETIME SysIdle, SysKernel, SysUser;
                    TimeSpan ProcTime;

                    //Process process = Process.GetCurrentProcess();
                    ProcTime = process.TotalProcessorTime;

                    if (!NativeMethods.GetSystemTimes(out SysIdle, out SysKernel, out SysUser))
                    {
                        Interlocked.Decrement(ref RunCount);
                        return CPUCopy;
                    }

                    if (!IsFirstRun)
                    {
                        UInt64 SysKernelDiff = SubtractTimes(SysKernel, PrevSysKernel);
                        UInt64 SysUserDiff = SubtractTimes(SysUser, PrevSysUser);

                        UInt64 SysTotal = SysKernelDiff + SysUserDiff;

                        Int64 ProcTotal = ProcTime.Ticks - PrevProcTotal.Ticks;

                        if (SysTotal > 0)
                        {
                            CPUUsage = (short)((100.0 * ProcTotal) / SysTotal);
                        }
                    }

                    PrevProcTotal = ProcTime;
                    PrevSysKernel = SysKernel;
                    PrevSysUser = SysUser;

                    LastRun = DateTime.Now;

                    CPUCopy = CPUUsage;
                }
                Interlocked.Decrement(ref RunCount);

                return CPUCopy;

            }

            private UInt64 SubtractTimes(System.Runtime.InteropServices.ComTypes.FILETIME a, System.Runtime.InteropServices.ComTypes.FILETIME b)
            {
                UInt64 aInt = ((UInt64)(a.dwHighDateTime << 32)) | (UInt64)a.dwLowDateTime;
                UInt64 bInt = ((UInt64)(b.dwHighDateTime << 32)) | (UInt64)b.dwLowDateTime;

                return aInt - bInt;
            }

            private bool EnoughTimePassed
            {
                get
                {
                    const int minimumElapsedMS = 250;
                    TimeSpan sinceLast = DateTime.Now - LastRun;
                    return sinceLast.TotalMilliseconds > minimumElapsedMS;
                }
            }

            private bool IsFirstRun
            {
                get
                {
                    return (LastRun == DateTime.MinValue);
                }
            }

            public void Dispose()
            {
                if (process != null)
                {
                    process.Dispose();
                    process = null;
                }
            }
        }

    }

}
