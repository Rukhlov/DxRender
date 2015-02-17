using DxRender;
using System.Threading;
using System.Drawing.Imaging;
using System;
using System.Drawing;
namespace DxRender
{
    class BitmapSource : IFrameSource, IDisposable
    {
        public BitmapSource()
        {
            buffer = new MemoryBuffer(TestBMP[0].Width, TestBMP[0].Height, 32);
        }

        Bitmap[] TestBMP = 
        { 
            (Bitmap)Bitmap.FromFile("bitmap\\01.bmp"), 
            (Bitmap)Bitmap.FromFile("bitmap\\02.bmp"),
            (Bitmap)Bitmap.FromFile("bitmap\\03.bmp") 
        };

        public readonly object locker = new object();

        Thread thread = null;
        MemoryBuffer buffer = null;

        MemoryBuffer IFrameSource.VideoBuffer
        {
            get { return buffer; }
        }

        public event Action<double> FrameRecieved;

        private void OnFrameRecieved(double Timestamp)
        {
            if (FrameRecieved != null)
                FrameRecieved(Timestamp);
        }

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        void IFrameSource.Start()
        {
            thread = new Thread(() =>
            {
                int Counter = 0;
                while (true)
                {
                    double FPS = 0;
                    long TimerEllapsed = stopwatch.ElapsedMilliseconds;
                    stopwatch.Restart();
                    if (TimerEllapsed > 0)
                        FPS = 1000.0 / TimerEllapsed;

                    Counter++;
                    int index = Counter % TestBMP.Length;
                    CopyIntoBuffer(TestBMP[index]);

                    OnFrameRecieved(FPS);

                    Thread.Sleep(16);
                    //Thread.Sleep(1000/100);
                }
            });

            thread.Start();
        }
        void IFrameSource.Pause()
        { }

        void IFrameSource.Stop()
        {
            this.Dispose();
        }

        private void CopyIntoBuffer(Bitmap bitmap)
        {
            var map = buffer.Data;
            BitmapData bits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            NativeMethods.CopyMemory(map.Scan0, bits.Scan0, map.Size);
            bitmap.UnlockBits(bits);
        }

        public void Dispose()
        {
            if (thread != null)
                thread.Abort();

            if (buffer != null)
                buffer.Dispose();
        }
    }
}