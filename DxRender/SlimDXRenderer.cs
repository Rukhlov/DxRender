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
    class SlimDXRenderer : IDisposable
    {
        private Device GraphicDevice = null;
        private PresentParameters PresentParams = null;
        private Sprite SpriteBatch = null;
        private GDI.Rectangle BackBufferArea;

        private Texture BackBufferTexture = null;
        private Surface BackBufferTextureSurface = null;
        private Surface OffscreenSurface;
        private Font ScreenFont;

        private Direct3D D3D = null;

        private PerfCounter PerfCounter = null;

        private IFrameSource FrameSource = null;
        private IntPtr DeviceWindowHandle = IntPtr.Zero;

        bool DeviceBusy = false;
        bool DeviceLost = false;

        private int Width = 0;
        private int Height = 0;

        public void Setup(IntPtr Handle, IFrameSource FrameSource)
        {
            this.DeviceWindowHandle = Handle;
            this.FrameSource = FrameSource;
            this.Width = FrameSource.VideoBuffer.Width;
            this.Height = FrameSource.VideoBuffer.Height;

            PerfCounter = new DxRender.PerfCounter();

            Setup();
        }

        private void Setup()
        {
            PresentParams = CreatePresentParameters();

            D3D = new Direct3D();
            GraphicDevice = new Device(D3D, 0, DeviceType.Hardware,
                DeviceWindowHandle, CreateFlags.Multithreaded | CreateFlags.FpuPreserve | CreateFlags.HardwareVertexProcessing,
                PresentParams);

            SpriteBatch = new Sprite(GraphicDevice);

            //SpriteBatch.Transform = Matrix.RotationZ(0.5f);
            //GraphicDevice.SetTransform(TransformState.Projection, Matrix.RotationZ(0.5f));

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
            DeviceLost = false;

            FrameSource.FrameReceived += FrameSource_FrameReceived;
        }

        private PresentParameters CreatePresentParameters()
        {
            PresentParameters parameters = new PresentParameters();

            parameters.SwapEffect = SwapEffect.Discard;
            parameters.DeviceWindowHandle = DeviceWindowHandle;
            parameters.Windowed = true;
            parameters.BackBufferWidth = Width;
            parameters.BackBufferHeight = Height;

            parameters.BackBufferFormat = Format.A8R8G8B8;
            parameters.AutoDepthStencilFormat = Format.D16;
            parameters.Multisample = MultisampleType.None;
            parameters.MultisampleQuality = 0;
            parameters.PresentationInterval = PresentInterval.One;
            parameters.PresentFlags = PresentFlags.Video;

            return parameters;
        }

        private void FrameSource_FrameReceived(object sender, FrameReceivedEventArgs e)
        {
            Draw();

            PerfCounter.UpdateStatistic(e.SampleTime);
        }

        public void Draw(bool UpdateSurface = true)
        {
            if (GraphicDevice == null) return;

            if (DeviceBusy == true) return;

            var r = GraphicDevice.TestCooperativeLevel();
            if (r != ResultCode.Success)
            {
                if (r == ResultCode.DeviceNotReset)
                {
                    CleanUp();
                    Setup();
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
                DeviceBusy = true;
                GraphicDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, GDI.Color.Black, 1.0f, 0);

                GraphicDevice.BeginScene();
                if (UpdateSurface)
                {
                    var data = this.FrameSource.VideoBuffer.Data;
                    CopyToSurface(data.Scan0, data.Size, BackBufferTextureSurface);
                }
                SpriteBatch.Begin(SpriteFlags.AlphaBlend);
                SpriteBatch.Draw(BackBufferTexture, BackBufferArea, GDI.Color.White);
                ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, GDI.Color.Red);
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
            finally
            {
                DeviceBusy = false;
            }
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

            GDI.Imaging.BitmapData BitmapRectangle = bitmap.LockBits(SurfaceArea,
                GDI.Imaging.ImageLockMode.ReadOnly, GDI.Imaging.PixelFormat.Format32bppArgb);

            SurfaceRectangle.Data.WriteRange(BitmapRectangle.Scan0, BitmapRectangle.Stride * BitmapRectangle.Height);
            bitmap.UnlockBits(BitmapRectangle);

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


        public void Dispose()
        {
            CleanUp();

            if (PerfCounter != null)
                PerfCounter.Dispose();
        }

        private void CleanUp()
        {
            if (FrameSource != null)
                FrameSource.FrameReceived -= FrameSource_FrameReceived;

            if (D3D != null)
            {
                D3D.Dispose();
                D3D = null;
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

    }

}
