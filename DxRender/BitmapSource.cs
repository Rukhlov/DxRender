using DeviceCreation;
using System.Threading;
using System.Drawing.Imaging;
using System;
using GDI = System.Drawing;

class BitmapSource : IFrameSource, IDisposable
{

    //Capture capture = null;

    public BitmapSource()
    {
        buffer = new MemoryBuffer(TestBMP[0].Width, TestBMP[0].Height, 32);
        //buffer = new MemoryBuffer(1280, 720, 32);
    }

    GDI.Bitmap[] TestBMP = 
        { 
            (GDI.Bitmap)GDI.Bitmap.FromFile("bitmap\\01.bmp"), 
            (GDI.Bitmap)GDI.Bitmap.FromFile("bitmap\\02.bmp"),
            (GDI.Bitmap)GDI.Bitmap.FromFile("bitmap\\03.bmp") 
        };

    public readonly object locker = new object();

    Thread thread = null;
    MemoryBuffer buffer = null;
    public MemoryBuffer VideoBuffer
    {
        get { return buffer; }
    }

    public event EventHandler FrameRecieved;

    private void OnFrameRecieved()
    {
        if (FrameRecieved != null)
            FrameRecieved(this, new EventArgs());
    }

    public void Start()
    {
        thread = new Thread(() =>
        {
            int Counter = 0;
            while (true)
            {
                //lock (locker)
                {
                    Counter++;
                    int index = Counter % TestBMP.Length;
                    CopyIntoBuffer(TestBMP[index]);

                    OnFrameRecieved();
                }

                Thread.Sleep(16);
                //Thread.Sleep(1000/100);
            }
        });

        thread.Start();
    }

    private void CopyIntoBuffer(GDI.Bitmap bitmap)
    {
        var map = buffer.Data;
        BitmapData bits = bitmap.LockBits(new GDI.Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        NativeMethods.CopyMemory(map.Scan0, bits.Scan0, map.Size);
        bitmap.UnlockBits(bits);
    }

    public void Dispose()
    {
        if (thread != null)
            thread.Abort();

        if (VideoBuffer != null)
            VideoBuffer.Dispose();
    }
}