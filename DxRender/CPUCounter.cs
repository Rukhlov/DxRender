using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

using System.Runtime.InteropServices.ComTypes;

namespace DxRender
{

    class PerfCounter: IDisposable
    {
        public PerfCounter()
        {
            stopwatch = new Stopwatch();
            counter = new CPUCounter();

            timer = new Timer((o) =>
            {
                CPU = counter.GetUsage();
            }, 
            null, 0, 1000);

        }
        private double FPS = 0;
        private double FPS2 = 0;
        private short CPU = 0;

        private CPUCounter counter = null;
        private Timer timer = null;       
        private Stopwatch stopwatch = null;

        double PrevSampleTime = 0;

        public void UpdateStatistic(double SampleTime)
        {
            long TimerEllapsed = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            if (TimerEllapsed > 0)
                FPS = 1000.0 / TimerEllapsed;

            double SampleTimeEllapsed = SampleTime - PrevSampleTime;
            if(SampleTimeEllapsed>0)
                FPS2 = 1.0 /SampleTimeEllapsed;
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

        }

        class CPUCounter:IDisposable
        {

            FILETIME _prevSysKernel;
            FILETIME _prevSysUser;

            TimeSpan _prevProcTotal;

            Int16 _cpuUsage;
            DateTime _lastRun;
            long _runCount;

            Process process;

            public CPUCounter()
            {
                _cpuUsage = -1;
                _lastRun = DateTime.MinValue;
                _prevSysUser.dwHighDateTime = _prevSysUser.dwLowDateTime = 0;
                _prevSysKernel.dwHighDateTime = _prevSysKernel.dwLowDateTime = 0;
                _prevProcTotal = TimeSpan.MinValue;
                _runCount = 0;

                process = Process.GetCurrentProcess();
            }

            public short GetUsage()
            {
                short cpuCopy = _cpuUsage;
                if (Interlocked.Increment(ref _runCount) == 1)
                {
                    if (!EnoughTimePassed)
                    {
                        Interlocked.Decrement(ref _runCount);
                        return cpuCopy;
                    }

                    FILETIME sysIdle, sysKernel, sysUser;
                    TimeSpan procTime;

                    //Process process = Process.GetCurrentProcess();
                    procTime = process.TotalProcessorTime;

                    if (!NativeMethods.GetSystemTimes(out sysIdle, out sysKernel, out sysUser))
                    {
                        Interlocked.Decrement(ref _runCount);
                        return cpuCopy;
                    }

                    if (!IsFirstRun)
                    {
                        UInt64 sysKernelDiff = SubtractTimes(sysKernel, _prevSysKernel);
                        UInt64 sysUserDiff = SubtractTimes(sysUser, _prevSysUser);

                        UInt64 sysTotal = sysKernelDiff + sysUserDiff;

                        Int64 procTotal = procTime.Ticks - _prevProcTotal.Ticks;

                        if (sysTotal > 0)
                        {
                            _cpuUsage = (short)((100.0 * procTotal) / sysTotal);
                        }
                    }

                    _prevProcTotal = procTime;
                    _prevSysKernel = sysKernel;
                    _prevSysUser = sysUser;

                    _lastRun = DateTime.Now;

                    cpuCopy = _cpuUsage;
                }
                Interlocked.Decrement(ref _runCount);

                return cpuCopy;

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
                    TimeSpan sinceLast = DateTime.Now - _lastRun;
                    return sinceLast.TotalMilliseconds > minimumElapsedMS;
                }
            }

            private bool IsFirstRun
            {
                get
                {
                    return (_lastRun == DateTime.MinValue);
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
