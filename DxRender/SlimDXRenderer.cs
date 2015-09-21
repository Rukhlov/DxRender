using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.Windows;

using GDI = System.Drawing;

using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace DxRender
{
    class SlimDXRenderer : RendererBase
    {
        private Device GraphicDevice = null;
        private PresentParameters PresentParams = null;
        private Sprite SpriteBatch = null;
        private GDI.Rectangle BackBufferArea;

        private Texture BackBufferTexture = null;
        private Surface BackBufferTextureSurface = null;
        private Surface OffscreenSurface;
        private Font ScreenFont;

        private Direct3D Direct3D9 = null;
        private AdapterInformation AdapterInfo = null;
        private bool DeviceLost = false;

        private MemoryBuffer buffer = null;
        public SlimDXRenderer(IntPtr Handle, IFrameSource FrameSource)
            : base(Handle, FrameSource)
        {
            buffer = FrameSource.VideoBuffer;

            StartUp();
        }

        System.Drawing.Bitmap TestBmp = null;
        private void StartUp()
        {
            TestBmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile("Bitmap\\01.bmp");
            Direct3D9 = new Direct3D();
            AdapterInfo = Direct3D9.Adapters.DefaultAdapter;

            var Capabilities= Direct3D9.GetDeviceCaps(0, DeviceType.Hardware);


            PresentParams = CreatePresentParameters();

            //CreateFlags Flags =  CreateFlags.SoftwareVertexProcessing;
            //CreateFlags Flags = CreateFlags.HardwareVertexProcessing;
            CreateFlags Flags = CreateFlags.Multithreaded | CreateFlags.FpuPreserve | CreateFlags.HardwareVertexProcessing;
            GraphicDevice = new Device(Direct3D9, AdapterInfo.Adapter, DeviceType.Hardware, OwnerHandle, Flags, PresentParams);

            SpriteBatch = new Sprite(GraphicDevice);

            BackBufferTexture = new Texture(GraphicDevice,
                PresentParams.BackBufferWidth,
                PresentParams.BackBufferHeight,
                0,
                Usage.Dynamic,
                PresentParams.BackBufferFormat,
                Pool.Default);

 
            BackBufferTextureSurface = BackBufferTexture.GetSurfaceLevel(0);

            OffscreenSurface = Surface.CreateOffscreenPlain(GraphicDevice,
                PresentParams.BackBufferWidth,
                PresentParams.BackBufferHeight,
                PresentParams.BackBufferFormat,
                Pool.Default);

            ScreenFont = new Font(GraphicDevice, PerfCounter.Styler.Font);

            BackBufferArea = new GDI.Rectangle(0, 0, PresentParams.BackBufferWidth, PresentParams.BackBufferHeight);
            DeviceLost = false;

            base.FrameSource.FrameReceived += FrameSource_FrameReceived;

            //BackBufferTexture = BitmapToTexture(TestBmp);
        }

        private bool IsFullScreen = false;
        private PresentParameters CreatePresentParameters()
        {
            PresentParameters parameters = new PresentParameters();
            //parameters.SwapEffect = SwapEffect.Discard;

            parameters.DeviceWindowHandle = OwnerHandle;
            if (IsFullScreen == false)
            {
                parameters.Windowed = true;
                parameters.BackBufferWidth = Width;
                parameters.BackBufferHeight = Height;
            }
            else
            { // для включения полноэкранного режима нужен хендл топового окна(НЕ КОНТРОЛА!!!)
                parameters.Windowed = false;
                parameters.BackBufferWidth = AdapterInfo.CurrentDisplayMode.Width;
                parameters.BackBufferHeight = AdapterInfo.CurrentDisplayMode.Height;
            }

            parameters.BackBufferFormat = AdapterInfo.CurrentDisplayMode.Format;
            parameters.AutoDepthStencilFormat = Format.D16;
            parameters.Multisample = MultisampleType.None;
            parameters.MultisampleQuality = 0;
            parameters.PresentationInterval = PresentInterval.Immediate;
            parameters.PresentFlags = PresentFlags.Video;

            return parameters;
        }

        private void FrameSource_FrameReceived(object sender, FrameReceivedEventArgs e)
        {
            Draw();
            PerfCounter.UpdateStatistic(e.SampleTime);
        }
        /*
        public override void Draw(bool UpdateSurface = true)
        {
            if (GraphicDevice == null) return;
            if (ReDrawing == true) return;
            try
            {
                ReDrawing = true;
                GraphicDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, GDI.Color.Black, 1.0f, 0);

                GraphicDevice.BeginScene();

                SpriteBatch.Begin(SpriteFlags.AlphaBlend);
                SpriteBatch.Draw(BackBufferTexture, BackBufferArea, GDI.Color.White);
                ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, PerfCounter.Styler.Color);
                SpriteBatch.End();

                

                GraphicDevice.EndScene();
                GraphicDevice.Present();
            }
            catch (Direct3D9Exception ex)
            {
                if (ex.ResultCode == ResultCode.DeviceLost)
                    DeviceLost = true;

                Debug.WriteLine(ex.Message);
            }
            finally { ReDrawing = false; }
            
        }
        */
        
        public override void Draw(bool UpdateSurface = true )
        {
            if (GraphicDevice == null) return;
            if (ReDrawing == true) return;

            var r = GraphicDevice.TestCooperativeLevel();
            if (r != ResultCode.Success)
            {
                if (r == ResultCode.DeviceNotReset)
                {
                    CleanUp();
                    StartUp();
                    UpdateSurface = true;
                }
                if (r == ResultCode.DeviceLost)
                {
                    DeviceLost = true;
                    return;
                }
            }

            try
            {
                ReDrawing = true;
                GraphicDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, GDI.Color.Black, 1.0f, 0);

                GraphicDevice.BeginScene();

                if (UpdateSurface)
                {
                    //CopyToSurface(BackBufferTextureSurface, TestBmp, new System.Drawing.Rectangle(0 ,0 , TestBmp.Width, TestBmp.Height)/*this.ClientRectangle*/);
                    CopyToSurface(buffer, BackBufferTextureSurface);

                }
                if (buffer.UpsideDown)
                {// если изображение перевернуто
                    SpriteBatch.Begin(SpriteFlags.AlphaBlend);
                    // поворачивем изображение на 180 град, со смещением
                    SpriteBatch.Transform = Matrix.Translation(-Width, -Height, 0) * Matrix.RotationZ((float)Math.PI);
                    SpriteBatch.Draw(BackBufferTexture, BackBufferArea, GDI.Color.White);
                    // возвращаем все как было и рисуем дальше
                    SpriteBatch.Transform = Matrix.Translation(0, 0, 0) * Matrix.RotationZ((float)Math.PI*2 );
                    ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, PerfCounter.Styler.Color);
                    SpriteBatch.End();
                }
                else
                {
                    GDI.Rectangle CropRectangle = new GDI.Rectangle(320, 240, 320, 240); 

                    //GDI.Rectangle CropRectangle = new GDI.Rectangle(BackBufferArea.Location, BackBufferArea.Size);

                    SpriteBatch.Begin(SpriteFlags.AlphaBlend);


                    float ScaleX = BackBufferArea.Width / CropRectangle.Width;
                    float ScaleY = BackBufferArea.Height / CropRectangle.Height;

                    float TranslationX =  BackBufferArea.X -CropRectangle.X;
                    float TranslationY = BackBufferArea.Y - CropRectangle.Y;

                    SpriteBatch.Transform = Matrix.Scaling(ScaleX, ScaleY, 0) * Matrix.Translation(TranslationX, TranslationY, 0);

                    //Vector3 center = new Vector3(BackBufferArea.Width / 2, BackBufferArea.Height / 2, 0f);
                    //SpriteBatch.Draw(BackBufferTexture, BackBufferArea, center, null, GDI.Color.White);

                    SpriteBatch.Draw(BackBufferTexture, BackBufferArea, GDI.Color.White);
                    

                    ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, PerfCounter.Styler.Color);
                    SpriteBatch.End();

                }

                GraphicDevice.EndScene();
                GraphicDevice.Present();
                //Thread.Sleep(1000);
            }
            catch (Direct3D9Exception ex)
            {
                if (ex.ResultCode == ResultCode.DeviceLost)
                    DeviceLost = true;

                Debug.WriteLine(ex.Message);
            }
            finally { ReDrawing = false; }
        }
        
        private void CopyToSurface(IntPtr ptr, int Size, Surface surface)
        {
            lock (surface)
            {
                DataRectangle SurfaceRectangle = surface.LockRectangle(LockFlags.None);
                SurfaceRectangle.Data.WriteRange(ptr, Size);
                surface.UnlockRectangle();
            }
        }

        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private void __CopyToSurface(MemoryBuffer buf, Surface surface)
        {
            lock (buf)
            {
                MappedData data = buf.Data;
                DataRectangle SurfaceRectangle = surface.LockRectangle(LockFlags.None);
                SurfaceRectangle.Data.WriteRange(data.Scan0, data.Size);
                surface.UnlockRectangle();
            }
        }


        private void _____CopyToSurface(Surface surface, GDI.Bitmap bitmap, GDI.Rectangle SurfaceArea)
        {
            DataRectangle SurfaceRectangle = surface.LockRectangle(LockFlags.None);

            GDI.Imaging.BitmapData BitmapRectangle = bitmap.LockBits(SurfaceArea,
                GDI.Imaging.ImageLockMode.ReadOnly, GDI.Imaging.PixelFormat.Format32bppArgb);

            SurfaceRectangle.Data.WriteRange(BitmapRectangle.Scan0, BitmapRectangle.Stride * BitmapRectangle.Height);
            bitmap.UnlockBits(BitmapRectangle);

            surface.UnlockRectangle();
        }

        //http://stackoverflow.com/questions/16493702/copying-gdi-bitmap-into-directx-texture
        private void CopyToSurface(MemoryBuffer buf, Surface surface)
        {
            lock (buf)
            {
                int bufferSize = buf.Data.Size;

                byte[] bytes = new byte[bufferSize];

                System.Runtime.InteropServices.Marshal.Copy(buf.Data.Scan0, bytes, 0, bytes.Length);

                DataRectangle t_data = surface.LockRectangle(LockFlags.None);

                int NewStride = buf.Width * 4;
                int RestStride = t_data.Pitch - NewStride;
                for (int j = 0; j < buf.Height; j++)
                {
                    t_data.Data.Write(bytes, j * (NewStride), NewStride);
                    t_data.Data.Position = t_data.Data.Position + RestStride;
                }

                surface.UnlockRectangle();

            }
            
        }

        unsafe private void CopyToSurface(Surface surface, GDI.Bitmap bitmap, GDI.Rectangle SurfaceArea)
        {
            GDI.Imaging.BitmapData bitmapData = bitmap.LockBits(SurfaceArea,
                GDI.Imaging.ImageLockMode.ReadOnly, GDI.Imaging.PixelFormat.Format32bppArgb);

            int bitsPerPixel = 24;
            int Stride = 4 * ((bitmapData.Width * ((bitsPerPixel + 7) / 8) + 3) / 4);

            int bufferSize = bitmapData.Height * Stride;//bitmapData.Stride;
            byte[] bytes = new byte[bufferSize];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);
            DataRectangle t_data = surface.LockRectangle(LockFlags.None);
            int NewStride = bitmap.Width * 4;
            int RestStride = t_data.Pitch - NewStride;
            for (int j = 0; j < bitmap.Height; j++)
            {
                t_data.Data.Write(bytes, j * (NewStride), NewStride);
                t_data.Data.Position = t_data.Data.Position + RestStride;
            }

            //DataRectangle t_data = surface.LockRectangle(LockFlags.None);

            //int pitch = ((int)t_data.Pitch / sizeof(ushort));
            //int bitmapPitch = ((int)bitmapData.Stride / sizeof(ushort));

            //DataStream d_stream = t_data.Data;
            //ushort* to = (ushort*)d_stream.DataPointer;
            //ushort* from = (ushort*)bitmapData.Scan0.ToPointer();

            //for (int j = 0; j < bitmap.Height; j++)
            //    for (int i = 0; i < bitmapPitch; i++)
            //        to[i + j * pitch] = from[i + j * bitmapPitch];

            bitmap.UnlockBits(bitmapData);

            surface.UnlockRectangle();
        }

        private void ___CopyToSurface(Surface surface, GDI.Bitmap bitmap, GDI.Rectangle SurfaceArea)
        {
            GDI.Imaging.BitmapData bitmapData = bitmap.LockBits(SurfaceArea,
                GDI.Imaging.ImageLockMode.ReadOnly, GDI.Imaging.PixelFormat.Format32bppArgb);

            //SurfaceRectangle.Data.WriteRange(BitmapRectangle.Scan0, BitmapRectangle.Stride * BitmapRectangle.Height);

            int bufferSize = bitmapData.Height * bitmapData.Stride;

            //create data buffer 
            byte[] bytes = new byte[bufferSize];

            // copy bitmap data into buffer
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);

            DataRectangle t_data = surface.LockRectangle(LockFlags.None);
            //var t_data = _resulttexture.LockRectangle(0, LockFlags.None);

            //int NewStride = bitmap.Width * 4;
            //int RestStride = t_data.Pitch - NewStride;
            //for (int j = 0; j < bitmap.Height; j++)
            //{
            //    t_data.Data.Write(bytes, j * NewStride, NewStride);
            //    t_data.Data.Position = t_data.Data.Position + RestStride;
            //}

            int pitch = ((int)t_data.Pitch / sizeof(ushort));
            int bitmapPitch = ((int)bitmapData.Stride / sizeof(ushort));

            DataStream d_stream = t_data.Data;
            unsafe
            {
                ushort* to = (ushort*)d_stream.DataPointer;
                ushort* from = (ushort*)bitmapData.Scan0.ToPointer();

                for (int j = 0; j < bitmap.Height; j++)
                    for (int i = 0; i < bitmapPitch; i++)
                        to[i + j * pitch] = from[i + j * bitmapPitch];
            }

            bitmap.UnlockBits(bitmapData);
            surface.UnlockRectangle();
        }

        private Texture BitmapToTexture(GDI.Bitmap bitmap)
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


        public override void Dispose()
        {
            CleanUp();

            if (PerfCounter != null)
                PerfCounter.Dispose();

            base.Dispose();
        }

        private void CleanUp()
        {
            if (FrameSource != null)
                FrameSource.FrameReceived -= FrameSource_FrameReceived;

            if (Direct3D9 != null)
            {
                Direct3D9.Dispose();
                Direct3D9 = null;
            }

            if (GraphicDevice != null)
            {
                GraphicDevice.Dispose();
                GraphicDevice = null;
            }

            if (BackBufferTexture != null)
            {
                BackBufferTexture.Dispose();
                BackBufferTexture = null;
            }
            if (BackBufferTextureSurface != null)
            {
                BackBufferTextureSurface.Dispose();
                BackBufferTextureSurface = null;
            }
            if (OffscreenSurface != null)
            {
                OffscreenSurface.Dispose();
                OffscreenSurface = null;
            }
            if (SpriteBatch != null)
            {
                SpriteBatch.Dispose();
                SpriteBatch = null;
            }

            if (ScreenFont != null)
            {
                ScreenFont.Dispose();
                ScreenFont = null;
            }
        }


        internal int CopyToSurfaceTest()
        {
            CopyToSurface(buffer, BackBufferTextureSurface);
            return buffer.Size;
        }


    }

}
