﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.Windows;

using GDI = System.Drawing;

using System.Windows.Forms;
using System.Diagnostics;

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

        private PerfCounter PerfCounter = null;

        private IFrameSource FrameSource = null;
        private IntPtr DeviceWindowHandle = IntPtr.Zero;

        bool DeviceBusy = false;

        public void Start(IntPtr Handle, IFrameSource FrameSource)
        {
            this.DeviceWindowHandle = Handle;
            this.FrameSource = FrameSource;

            PerfCounter = new DxRender.PerfCounter();

            PresentParams = new PresentParameters();
            PresentParams.SwapEffect = SwapEffect.Discard;
            PresentParams.DeviceWindowHandle = DeviceWindowHandle;
            PresentParams.Windowed = true;
            PresentParams.BackBufferWidth = FrameSource.VideoBuffer.Width;
            PresentParams.BackBufferHeight = FrameSource.VideoBuffer.Height;

            PresentParams.BackBufferFormat = Format.A8R8G8B8;
            PresentParams.AutoDepthStencilFormat = Format.D16;
            PresentParams.Multisample = MultisampleType.None;
            PresentParams.MultisampleQuality = 0;
            PresentParams.PresentationInterval = PresentInterval.One;
            PresentParams.PresentFlags = PresentFlags.Video;

            GraphicDevice = new Device(new Direct3D(), 0, DeviceType.Hardware,
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

            this.FrameSource.FrameReceived += new EventHandler<FrameReceivedEventArgs>(FrameSource_FrameReceived);

            this.FrameSource.Start();

        }

        private void FrameSource_FrameReceived(object sender, FrameReceivedEventArgs e)
        {
            if (GraphicDevice == null) return;

            var r = GraphicDevice.TestCooperativeLevel();
            if (r != ResultCode.Success) return;

            if (DeviceBusy == true) return;

            try
            {
                DeviceBusy = true;
                GraphicDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, GDI.Color.Black, 1.0f, 0);
                
                GraphicDevice.BeginScene();

                var data = this.FrameSource.VideoBuffer.Data;
                CopyToSurface(data.Scan0, data.Size, BackBufferTextureSurface);

                SpriteBatch.Begin(SpriteFlags.AlphaBlend);
                SpriteBatch.Draw(BackBufferTexture, BackBufferArea, GDI.Color.White);
                ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, GDI.Color.Red);
                SpriteBatch.End();

                GraphicDevice.EndScene();

                GraphicDevice.Present();
            }
            finally
            {
                DeviceBusy = false;
            }

            PerfCounter.UpdateStatistic(e.SampleTime);
        }

        public void Draw()
        {
            if (GraphicDevice == null) return;

            var r = GraphicDevice.TestCooperativeLevel();
            if (r != ResultCode.Success) return;

            if (DeviceBusy == true) return;

            try
            {
                DeviceBusy = true;
                GraphicDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, GDI.Color.Black, 1.0f, 0);

                Present();
            }
            finally
            {
                DeviceBusy = false;
            }
        }

        private void Present()
        {
            SpriteBatch.Begin(SpriteFlags.AlphaBlend);
            SpriteBatch.Draw(BackBufferTexture, BackBufferArea, GDI.Color.White);
            ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, GDI.Color.Red);
            SpriteBatch.End();

            GraphicDevice.Present();
        }

        private void CopyToSurface(IntPtr Ptr, int Size, Surface surface)
        {
            DataRectangle SurfaceRectangle = surface.LockRectangle(LockFlags.None);

            SurfaceRectangle.Data.WriteRange(Ptr, Size);

            surface.UnlockRectangle();
        }

        public void Dispose()
        {
            if (GraphicDevice != null)
            {
                GraphicDevice.Dispose();
                GraphicDevice = null;
            }

            if (PerfCounter != null)
                PerfCounter.Dispose();
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



    }

}
