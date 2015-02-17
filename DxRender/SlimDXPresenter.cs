using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D9;
using SlimDX.Windows;
using GDI = System.Drawing;
using SlimDX;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace DeviceCreation
{

    class SlimDXPresenter : IDisposable
    {
        private Device GraphicDevice = null;
        private PresentParameters PresentParams = null;
        private Sprite SpriteBatch = null;
        private GDI.Rectangle BackBufferArea;

        private Texture BackBufferTexture = null;
        private Surface BackBufferTextureSurface = null;
        private Surface OffscreenSurface;
        private Font ScreenFont;

        public void Start(RenderForm form, IFrameSource source)
        {
            form.Height = source.VideoBuffer.Height;
            form.Width = source.VideoBuffer.Width;

            PresentParams = new PresentParameters();
            PresentParams.SwapEffect = SwapEffect.Discard;
            PresentParams.DeviceWindowHandle = form.Handle;
            PresentParams.Windowed = true;
            PresentParams.BackBufferWidth = source.VideoBuffer.Width;
            PresentParams.BackBufferHeight = source.VideoBuffer.Height;

            PresentParams.BackBufferFormat = Format.A8R8G8B8;
            PresentParams.AutoDepthStencilFormat = Format.D16;
            PresentParams.Multisample = MultisampleType.None;
            PresentParams.MultisampleQuality = 0;
            PresentParams.PresentationInterval = PresentInterval.One;
            PresentParams.PresentFlags = PresentFlags.Video;

            GraphicDevice = new Device(new Direct3D(), 0, DeviceType.Hardware,
                form.Handle, CreateFlags.Multithreaded | CreateFlags.FpuPreserve | CreateFlags.HardwareVertexProcessing,
                PresentParams);

            SpriteBatch = new SlimDX.Direct3D9.Sprite(GraphicDevice);

            BackBufferTexture = new Texture(GraphicDevice,
                PresentParams.BackBufferWidth,
                PresentParams.BackBufferHeight,
                0,
                Usage.Dynamic,
                PresentParams.BackBufferFormat,
                //Format.X8B8G8R8,
                Pool.Default);

            BackBufferTextureSurface = BackBufferTexture.GetSurfaceLevel(0);


            OffscreenSurface = Surface.CreateOffscreenPlain(GraphicDevice,
                PresentParams.BackBufferWidth,
                PresentParams.BackBufferHeight,
                PresentParams.BackBufferFormat,
                Pool.Default);


            ScreenFont = new Font(GraphicDevice, new System.Drawing.Font("Arial", 30f, System.Drawing.FontStyle.Regular));

            BackBufferArea = new GDI.Rectangle(0, 0, PresentParams.BackBufferWidth, PresentParams.BackBufferHeight);

            form.KeyDown += (o, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    form.Close();

                if (e.Alt && e.KeyCode == Keys.Enter)
                { }
            };

            form.FormClosing += (o, e) =>
            {
                //System.IO.File.WriteAllText("statistics.txt", logger.ToString());

            };

            source.FrameRecieved += (o, a) =>
            {
                if (GraphicDevice == null) return;

                var r = GraphicDevice.TestCooperativeLevel();
                if (r != ResultCode.Success) return;

                GraphicDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, GDI.Color.Black, 1.0f, 0);
                GraphicDevice.BeginScene();

                var data = source.VideoBuffer.Data;
                CopyToSurface(data.Scan0, data.Size, BackBufferTextureSurface);

                //CopyToSurface(BackBufferTextureSurface, bitmap, BackBufferArea);
                //CopyToSurface(OffscreenSurface, source.GetBitmap(), BackBufferArea);

                SpriteBatch.Begin(SpriteFlags.AlphaBlend);
                SpriteBatch.Draw(BackBufferTexture, BackBufferArea, new Color4(1, 1, 1, 1));
                ScreenFont.DrawString(SpriteBatch, ReportString, 0, 0, new Color4(1, 0, 0, 0));
                SpriteBatch.End();
                //...
                GraphicDevice.EndScene();

                GraphicDevice.Present();

                UpdateFramerate();
            };

            source.Start();

            // MessagePumpRun(form, source);
        }

        private void CopyToSurface(IntPtr Ptr, int Size, Surface surface)
        {
            DataRectangle SurfaceRectangle = surface.LockRectangle(LockFlags.None);

            SurfaceRectangle.Data.WriteRange(Ptr, Size);

            surface.UnlockRectangle();
        }

        private void CopyToSurface(Surface surface, GDI.Bitmap bitmap, GDI.Rectangle SurfaceArea)
        {
            DataRectangle SurfaceRectangle = surface.LockRectangle(LockFlags.None);

            BitmapData BitmapRectangle = bitmap.LockBits(SurfaceArea,
                GDI.Imaging.ImageLockMode.ReadOnly, GDI.Imaging.PixelFormat.Format32bppArgb);

            SurfaceRectangle.Data.WriteRange(BitmapRectangle.Scan0, BitmapRectangle.Stride * BitmapRectangle.Height);
            bitmap.UnlockBits(BitmapRectangle);

            //SurfaceRectangle.Data.WriteRange(bitmap.Ptr, bitmap.Size);


            surface.UnlockRectangle();
        }

        double FPS = 0;
        double CPU = 0;
        long Counter = 0;
        //PerformanceCounter CPUCounter = new PerformanceCounter { CategoryName = "Processor", CounterName = "% Processor Time", InstanceName = Process.GetCurrentProcess().ProcessName };

        string ReportString = "";
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        private void UpdateFramerate()
        {
            Counter++;
            long TimerEllapsed = timer.ElapsedMilliseconds;
            timer.Restart();
            if (TimerEllapsed > 0)
                FPS = 1000.0 / TimerEllapsed;

            //CPU += CPUCounter.NextValue();

            ReportString = string.Format("FPS={0:0.00}, CPU={1}", FPS, CPU);
        }

        public void Dispose()
        {
            if (GraphicDevice != null)
            {
                GraphicDevice.Dispose();
                GraphicDevice = null;
            }
        }

        private void MessagePumpRun(RenderForm form, BitmapSource source)
        {
            //System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            //long Counter = 0;

            //MessagePump.Run(form, () =>
            //{

            //    if (source.Lock())
            //    {
            //        GraphicDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, GDI.Color.Black, 1.0f, 0);
            //        GraphicDevice.BeginScene();

            //        //CopyToSurface(BackBufferTextureSurface, source.GetBitmap(), BackBufferArea);
            //        //CopyToSurface(OffscreenSurface, source.GetBitmap(), BackBufferArea);

            //        SpriteBatch.Begin(SpriteFlags.AlphaBlend);
            //        SpriteBatch.Draw(BackBufferTexture, BackBufferArea, new Color4(1, 1, 1, 1));
            //        ScreenFont.DrawString(SpriteBatch, ReportString, 0, 0, new Color4(1, 0, 0, 0));
            //        SpriteBatch.End();
            //        //...
            //        GraphicDevice.EndScene();
            //    }

            //    GraphicDevice.Present();
            //    UpdateFramerate();

            //});

            //source.Dispose();
            //GraphicDevice.Dispose();
        }



        //private InteropBitmap BitmapSource;

        //private IntPtr map;
        //private int bmpSize;
        //private IntPtr section;

        //private IntPtr bmpPtr1;
        //private IntPtr bmpPtr2;

        //private void MappedFileTest(GDI.Bitmap bmp1, GDI.Bitmap bmp2)
        //{
        //    uint pcount = (uint)(bmp1.Width * bmp1.Height * System.Windows.Media.PixelFormats.Bgr32.BitsPerPixel / 8);

        //    section = NativeMethods.CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, pcount, null);

        //    map = NativeMethods.MapViewOfFile(section, 0xF001F, 0, 0, pcount);

        //    GDI.Rectangle rectangle = new GDI.Rectangle(0, 0, bmp1.Width, bmp1.Height);

        //    BitmapData bmpData1 = bmp1.LockBits(rectangle, System.Drawing.Imaging.ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        //    bmpPtr1 = bmpData1.Scan0;

        //    bmpSize = bmpData1.Stride * bmpData1.Height;
        //    int bmpStride = bmpData1.Stride;
        //    int bmpWidth = bmpData1.Width;
        //    int bmpHeight = bmpData1.Height;

        //    BitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromMemorySection(section, bmpWidth, bmpHeight, System.Windows.Media.PixelFormats.Rgb24,
        //        /*bmpWidth * PixelFormats.Bgr32.BitsPerPixel / 8*/bmpStride, 0) as InteropBitmap;

        //    NativeMethods.CopyMemory(map, bmpPtr1, bmpSize);

        //    bmp1.UnlockBits(bmpData1);


        //    BitmapData bmpData2 = bmp2.LockBits(rectangle, System.Drawing.Imaging.ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        //    bmpPtr2 = bmpData2.Scan0;

        //    bmpSize = bmpData2.Stride * bmpData2.Height;
        //    int bmpStride2 = bmpData2.Stride;
        //    int bmpWidth2 = bmpData2.Width;
        //    int bmpHeight2 = bmpData2.Height;

        //    NativeMethods.CopyMemory(map, bmpPtr2, bmpSize);

        //    bmp2.UnlockBits(bmpData2);
        //}

        Texture BitmapToTexture(GDI.Bitmap bitmap)
        {
            Texture _resulttexture = new Texture(GraphicDevice, bitmap.Width, bitmap.Height, 0,
               Usage.Dynamic,
                // Format.A8R8G8B8,
               PresentParams.BackBufferFormat,
               Pool.Default);

            System.Drawing.Imaging.BitmapData bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, GDI.Imaging.PixelFormat.Format32bppArgb);

            int bufferSize = bitmapData.Height * bitmapData.Stride;

            //create data buffer 
            byte[] bytes = new byte[bufferSize];

            // copy bitmap data into buffer
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);
            var t_data = _resulttexture.LockRectangle(0, LockFlags.None);

            int NewStride = bitmap.Width * 4;
            int RestStride = t_data.Pitch - NewStride;
            for (int j = 0; j < bitmap.Height; j++)
            {
                t_data.Data.Write(bytes, j * (NewStride), NewStride);
                t_data.Data.Position = t_data.Data.Position + RestStride;
            }

            _resulttexture.UnlockRectangle(0);

            bitmap.UnlockBits(bitmapData);

            return _resulttexture;
        }//BitmapToTexture
    }
}
