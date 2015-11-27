using DxRender;
using System.Threading;
using System.Drawing.Imaging;
using System;
using System.Drawing;
namespace DxRender
{
    class BitmapSource : IFrameSource, IDisposable
    {
        public BitmapSource(int Width, int Height)
        {
            TestBMP = new Bitmap[] { 
                //new Bitmap(Bitmap.FromFile("bitmap\\04.bmp"),  Width, Height),//1920, 1080),
               new Bitmap(Bitmap.FromFile("bitmap\\01.bmp"), 1280, 960),
                //new Bitmap(Bitmap.FromFile("bitmap\\03.bmp"), Width, Height)
            };
            buffer = new MemoryBuffer(TestBMP[0].Width, TestBMP[0].Height, 32);
        }

        Bitmap[] TestBMP = null;

        Thread thread = null;
        MemoryBuffer buffer = null;

        MemoryBuffer IFrameSource.VideoBuffer
        {
            get { return buffer; }
        }
        private event EventHandler<FrameReceivedEventArgs> FrameReceived;
        event EventHandler<FrameReceivedEventArgs> IFrameSource.FrameReceived
        {
            add { FrameReceived += value; }
            remove { FrameReceived -= value; }
        }
        private void OnFrameReceived(double Timestamp)
        {
            if (FrameReceived != null)
                FrameReceived(this, new FrameReceivedEventArgs { SampleTime = Timestamp});
        }

        private string info = "BitmapSource";
        string IFrameSource.Info { get { return info; } }

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        void IFrameSource.Start()
        {
            stopwatch.Restart();
            thread = new Thread(() =>
            {
                int Counter = 0;
                while (true)
                {
                    Counter++;
                    int index = Counter % TestBMP.Length;
                    CopyIntoBuffer(TestBMP[index]);

                    OnFrameReceived(stopwatch.ElapsedMilliseconds/1000.0);

                    Thread.Sleep(30);
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
            lock (buffer)
            {
                var map = buffer.Data;
                BitmapData bits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    /*PixelFormat.Format32bppArgb*/PixelFormat.Format32bppArgb);

                NativeMethods.CopyMemory(map.Scan0, bits.Scan0, map.Size);
                bitmap.UnlockBits(bits);
            }
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