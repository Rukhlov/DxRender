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


        private Control control = null;
        public SlimDXRenderer(IntPtr Handle, IFrameSource FrameSource)
            : base(Handle, FrameSource)
        {
            buffer = FrameSource.VideoBuffer;

            StartUp();

            control = Control.FromHandle(Handle);


            control.Resize += (o, a) =>
            {
                //Control ctr = o as Control;
                //if (ctr != null)
                //{
                //    if (GraphicDevice != null)
                //    {
                //        PresentParams.BackBufferWidth = ctr.Width;
                //        PresentParams.BackBufferHeight = ctr.Width;
                //        //ReleaseBackBuffer();
                //        //OnDeviceLost(EventArgs.Empty);
                //        GraphicDevice.Reset(PresentParams);

                //        //GraphicDevice.BackBufferHeight 
                //    }
                //}
            };
        }

        private void StartUp()
        {
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
                Width,//PresentParams.BackBufferWidth,
                Height,//PresentParams.BackBufferHeight,
                0,
                Usage.Dynamic,
                PresentParams.BackBufferFormat,
                Pool.Default);

 
            BackBufferTextureSurface = BackBufferTexture.GetSurfaceLevel(0);

            OffscreenSurface = Surface.CreateOffscreenPlain(GraphicDevice,
                Width,//PresentParams.BackBufferWidth,
                Height,//PresentParams.BackBufferHeight,
                PresentParams.BackBufferFormat,
                Pool.Default);

            ScreenFont = new Font(GraphicDevice, PerfCounter.Styler.Font);

            VertexElement[]  vertexElems = new[] {
            	new VertexElement(0, 0,  DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.PositionTransformed, 0),
        		new VertexElement(0, 8, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.Color, 0),
				VertexElement.VertexDeclarationEnd };

            GraphicDevice.VertexDeclaration = new VertexDeclaration(GraphicDevice, vertexElems);

            BackBufferArea = new GDI.Rectangle(0, 0, PresentParams.BackBufferWidth, PresentParams.BackBufferHeight);
            DeviceLost = false;

            base.FrameSource.FrameReceived += FrameSource_FrameReceived;



            BitmapRectangle = BackBufferArea;
            CropRectangle = BackBufferArea;

            GraphicDevice.SetRenderState(RenderState.AntialiasedLineEnable, false);

            GraphicDevice.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
            GraphicDevice.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
            GraphicDevice.SetRenderState(RenderState.DestinationBlendAlpha, Blend.InverseSourceAlpha);
            GraphicDevice.SetRenderState(RenderState.SourceBlendAlpha, Blend.SourceAlpha);

            GraphicDevice.SetRenderState(RenderState.AntialiasedLineEnable, false);
            GraphicDevice.SetRenderState(RenderState.AlphaBlendEnable, true);
            GraphicDevice.SetRenderState(RenderState.AlphaTestEnable, true);
            GraphicDevice.SetRenderState(RenderState.SeparateAlphaBlendEnable, true);
            GraphicDevice.SetRenderState(RenderState.BlendOperation, BlendOperation.Add);
            GraphicDevice.SetRenderState(RenderState.BlendOperationAlpha, BlendOperation.Add);


            GraphicDevice.SetRenderState(RenderState.Clipping, true);
            GraphicDevice.SetRenderState(RenderState.ClipPlaneEnable, 0);
            GraphicDevice.SetRenderState(RenderState.ColorVertex, true);
            GraphicDevice.SetRenderState(RenderState.CullMode, Cull.None);
            //graphicDevice.SetRenderState(RenderState.DebugMonitorToken, RenderState.DebugMonitorToken.Enabled);
            GraphicDevice.SetRenderState(RenderState.DepthBias, 0);
            GraphicDevice.SetRenderState(RenderState.DitherEnable, false);
            GraphicDevice.SetRenderState(RenderState.EnableAdaptiveTessellation, false);
            GraphicDevice.SetRenderState(RenderState.FillMode, FillMode.Solid);

            GraphicDevice.SetRenderState(RenderState.FogEnable, false);
            GraphicDevice.SetRenderState(RenderState.LastPixel, true);
            //GraphicDevice.SetRenderState(RenderState.Lighting, true);
            GraphicDevice.SetRenderState(RenderState.Lighting, false);
            GraphicDevice.SetRenderState(RenderState.LocalViewer, true);
            GraphicDevice.SetRenderState(RenderState.MultisampleAntialias, false);

            GraphicDevice.SetRenderState(RenderState.PointSpriteEnable, false);
            GraphicDevice.SetRenderState(RenderState.ScissorTestEnable, false);
            //   graphicDevice.SetRenderState(RenderState.SeparateAlphaBlendEnable, false);

            //graphicDevice.SetRenderState(RenderState.ShadeMode, ShadeMode.Flat);
            GraphicDevice.SetRenderState(RenderState.ShadeMode, ShadeMode.Gouraud);

            GraphicDevice.SetRenderState(RenderState.SpecularEnable, false);
            GraphicDevice.SetRenderState(RenderState.StencilEnable, false);
            GraphicDevice.SetRenderState(RenderState.VertexBlend, VertexBlend.Disable);
            GraphicDevice.SetRenderState(RenderState.ZEnable, ZBufferType.DontUseZBuffer);
            GraphicDevice.SetRenderState(RenderState.ZWriteEnable, false);

            
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
                parameters.BackBufferWidth = AdapterInfo.CurrentDisplayMode.Width;
                parameters.BackBufferHeight = AdapterInfo.CurrentDisplayMode.Height;
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
        GDI.RectangleF CropRectangle;// = new GDI.Rectangle(0, 0, 640, 480);

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

                GDI.Rectangle DisplayRectangle = control.DisplayRectangle;
                Matrix Transform = Matrix.Identity;

                if (DisplayRectangle.Width != Width || DisplayRectangle.Height != Height)
                {
                    float ControlScaleX = (float)DisplayRectangle.Width / Width;//BackBufferArea.Width;
                    float ControlScaleY = (float)DisplayRectangle.Height / Height;//BackBufferArea.Height;

                    Transform = Matrix.Scaling(ControlScaleX, ControlScaleY, 1);
                }

                if (buffer.UpsideDown)
                {// поворачивем изображение на 180 град, со смещением
                    Transform = Matrix.Translation(-Width, -Height, 0) * Matrix.RotationZ((float)Math.PI) * Transform;
                }

                SpriteBatch.Begin(SpriteFlags.AlphaBlend);
                
                SpriteBatch.Transform = Transform;
                SpriteBatch.Draw(BackBufferTexture, BackBufferArea, GDI.Color.White);

                // возвращаем все как было и рисуем дальше
                SpriteBatch.Transform = Matrix.Identity;

                ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, PerfCounter.Styler.Color);           

                SpriteBatch.End();
                
                DrawRectangle(SelectionRectangle);

                #region MyRegion
                //if (buffer.UpsideDown)
                //{// если изображение перевернуто
                //    SpriteBatch.Begin(SpriteFlags.AlphaBlend);

                //    // поворачивем изображение на 180 град, со смещением
                //    SpriteBatch.Transform = Matrix.Translation(-Width, -Height, 0) * Matrix.RotationZ((float)Math.PI) * ScaleTransform;
                //    SpriteBatch.Draw(BackBufferTexture, BackBufferArea, GDI.Color.White);
                //    // возвращаем все как было и рисуем дальше
                //    SpriteBatch.Transform = Matrix.Identity;//Matrix.Translation(0, 0, 0) * Matrix.RotationZ((float)Math.PI * 2);

                //    ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, PerfCounter.Styler.Color);
                //    SpriteBatch.End();

                //}
                //else
                //{

                //    SpriteBatch.Begin(SpriteFlags.AlphaBlend);


                //    ////GDI.Rectangle ControlCropRectangle = new GDI.Rectangle(320, 220, 320, 240);
                //    ////GDI.Rectangle ControlCropRectangle = new GDI.Rectangle(110, 30, 148, 65);

                //    ////GDI.Rectangle ControlDisplayRectangle = control.DisplayRectangle; //new GDI.Rectangle(110, 30, 148, 65);

                //    ////float ControlScaleX = (float)BackBufferArea.Width / ControlDisplayRectangle.Width;
                //    ////float ControlScaleY = (float)BackBufferArea.Height / ControlDisplayRectangle.Height;

                //    ////GDI.RectangleF CropRectangle = new GDI.RectangleF(ControlCropRectangle.X * ControlScaleX, ControlCropRectangle.Y * ControlScaleY,
                //    ////    ControlCropRectangle.Width * ControlScaleX, ControlCropRectangle.Height * ControlScaleY);

                //    //if (CropRectangle.IsEmpty == false)
                //    //{
                //    //    float ScaleX = BackBufferArea.Width / CropRectangle.Width;
                //    //    float ScaleY = BackBufferArea.Height / CropRectangle.Height;

                //    //    float TranslationX = (BackBufferArea.X - CropRectangle.X) * ScaleX;
                //    //    float TranslationY = (BackBufferArea.Y - CropRectangle.Y) * ScaleY;

                //    //    SpriteBatch.Transform = Matrix.Scaling(ScaleX, ScaleY, 0) * Matrix.Translation(TranslationX, TranslationY, 0);
                //    //}

                //    //GDI.Rectangle ControlDisplayRectangle = control.DisplayRectangle;

                //    //float ControlScaleX = (float)ControlDisplayRectangle.Width / Width;//BackBufferArea.Width;
                //    //float ControlScaleY = (float)ControlDisplayRectangle.Height / Height;//BackBufferArea.Height;

                //    SpriteBatch.Transform = ScaleTransform;//Matrix.Scaling(ControlScaleX, ControlScaleY, 1);

                //    //GDI.RectangleF CropRectangle = new GDI.RectangleF(ControlCropRectangle.X * ControlScaleX, ControlCropRectangle.Y * ControlScaleY,
                //    //    ControlCropRectangle.Width * ControlScaleX, ControlCropRectangle.Height * ControlScaleY);

                //    //if (CropRectangle.IsEmpty == false)
                //    //{
                //    //    float ScaleX = BackBufferArea.Width / CropRectangle.Width;
                //    //    float ScaleY = BackBufferArea.Height / CropRectangle.Height;

                //    //    float TranslationX = (BackBufferArea.X - CropRectangle.X) * ScaleX;
                //    //    float TranslationY = (BackBufferArea.Y - CropRectangle.Y) * ScaleY;

                //    //    SpriteBatch.Transform = Matrix.Scaling(ScaleX, ScaleY, 0) * Matrix.Translation(TranslationX, TranslationY, 0);
                //    //}


                //    SpriteBatch.Draw(BackBufferTexture, BackBufferArea, GDI.Color.White);

                //    SpriteBatch.Transform = Matrix.Identity;

                //    ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, PerfCounter.Styler.Color);
                //    SpriteBatch.End();

                //}

                //DrawRectangle(SelectionRectangle);

                #endregion

                GraphicDevice.EndScene();

                GraphicDevice.Present(DisplayRectangle, DisplayRectangle);

                //GraphicDevice.Present();

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

        GDI.Rectangle SelectionRectangle = new GDI.Rectangle();

        public override void Execute(string Command, params object[] Parameters)
        {
            switch (Command)
            {
                case "MouseMove":
                    GDI.Rectangle Rect = (GDI.Rectangle)Parameters[0];
                    SelectionRectangle = Rect;

                    break;

                default:
                    break;
            }
        }
        public void DrawRectangle(GDI.Rectangle Rectangle)
        {
            if (Rectangle.IsEmpty) return;

            //GDI.Rectangle ControlDisplayRectangle = control.DisplayRectangle; //new GDI.Rectangle(110, 30, 148, 65);

            //float ControlScaleX = (float)BackBufferArea.Width / ControlDisplayRectangle.Width;
            //float ControlScaleY = (float)BackBufferArea.Height / ControlDisplayRectangle.Height;


            GDI.Rectangle ControlDisplayRectangle = control.DisplayRectangle;

            float ControlScaleX = 1;//(float)ControlDisplayRectangle.Width / BackBufferArea.Width;
            float ControlScaleY = 1;//(float)ControlDisplayRectangle.Height / BackBufferArea.Height;

            GDI.RectangleF CropRectangle = new GDI.RectangleF(Rectangle.X * ControlScaleX, Rectangle.Y * ControlScaleY,
                Rectangle.Width * ControlScaleX, Rectangle.Height * ControlScaleY);


            Vertex[] v = new Vertex[5];
            v[0].Position.X = CropRectangle.X;
            v[0].Position.Y = CropRectangle.Y;
            v[0].Color = 0x3FFF0000;

            v[1].Position.X = CropRectangle.X + CropRectangle.Width;
            v[1].Position.Y = CropRectangle.Y;
            v[1].Color = 0x3FFF0000;

            v[3].Position.X = CropRectangle.X + CropRectangle.Width;
            v[3].Position.Y = CropRectangle.Y + CropRectangle.Height;
            v[3].Color = 0x3FFF0000;

            v[2].Position.X = CropRectangle.X;
            v[2].Position.Y = CropRectangle.Y + CropRectangle.Height;
            v[2].Color = 0x3FFF0000;

            v[4].Position.X = CropRectangle.X;
            v[4].Position.Y = CropRectangle.Y;
            v[4].Color = 0x3FFF0000;

            GraphicDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, 0, 2, v);

        }

        public void __DrawRectangle(GDI.Rectangle Rectangle)
        {
            if (Rectangle.IsEmpty) return;

            //GDI.Rectangle ControlDisplayRectangle = control.DisplayRectangle; //new GDI.Rectangle(110, 30, 148, 65);

            //float ControlScaleX = (float)BackBufferArea.Width / ControlDisplayRectangle.Width;
            //float ControlScaleY = (float)BackBufferArea.Height / ControlDisplayRectangle.Height;


            GDI.Rectangle ControlDisplayRectangle = control.DisplayRectangle;

            float ControlScaleX = 1;//(float)ControlDisplayRectangle.Width / BackBufferArea.Width;
            float ControlScaleY = 1;//(float)ControlDisplayRectangle.Height / BackBufferArea.Height;

            GDI.RectangleF CropRectangle = new GDI.RectangleF(Rectangle.X * ControlScaleX, Rectangle.Y * ControlScaleY,
                Rectangle.Width * ControlScaleX, Rectangle.Height * ControlScaleY);


            Vertex[] v = new Vertex[5];
            v[0].Position.X = CropRectangle.X;
            v[0].Position.Y = CropRectangle.Y;
            v[0].Color = 0xFFFF0000;

            v[1].Position.X = CropRectangle.X + CropRectangle.Width;
            v[1].Position.Y = CropRectangle.Y;
            v[1].Color = 0xFFFF0000;

            v[2].Position.X = CropRectangle.X + CropRectangle.Width;
            v[2].Position.Y = CropRectangle.Y + CropRectangle.Height;
            v[2].Color = 0xFFFF0000;

            v[3].Position.X = CropRectangle.X;
            v[3].Position.Y = CropRectangle.Y + CropRectangle.Height;
            v[3].Color = 0xFFFF0000;

            v[4].Position.X = CropRectangle.X;
            v[4].Position.Y = CropRectangle.Y;
            v[4].Color = 0xFFFF0000;

            GraphicDevice.DrawUserPrimitives(PrimitiveType.LineStrip, 0, v.Count()-1, v);

        }

        public void PolyLine(System.Drawing.Point [] points, int Count)
        {

            //if (Captured == true)
            {
                if (Count < 2) return;
                Vertex[] v = new Vertex[6000];
                for (int i = 0; i < Count; i++)
                {
                    v[i].Position.X = points[i].X;
                    v[i].Position.Y = points[i].Y;
                    v[i].Color = 0;
                }


                /*DataStream ds = poly_vertices.Lock(0, Count * VertexStrideBytes, LockFlags.Discard);
                // ds.Position = 0;

                for (int i = 0; i < Count; i++)
                {
                    v[i].Position.X = points[i].x;
                    v[i].Position.Y = points[i].y;
                    v[i].Color = PenColor;
                }
                ds.WriteRange(v, 0, Count);

                //for (int i = 0; i < Count; i++)
                //ds.Write<Vertex>(new Vertex(points[i].x, points[i].y, PenColor));

                //Заполнение массива и загрузка или загрузка по одной точке по скорости одинаково
                //Но в этом случае хоть не надо создавать дополнительные массивы

                poly_vertices.Unlock();

                graphicDevice.SetStreamSource(0, poly_vertices, 0, VertexStrideBytes);*/
                GraphicDevice.DrawUserPrimitives(PrimitiveType.LineStrip, 0, Count - 1, v);
            }
        }//Line

        GDI.RectangleF BitmapRectangle = new GDI.RectangleF();

        public override void SetRectangle(GDI.Rectangle Rect)
        {
            SelectionRectangle = new GDI.Rectangle();
            if (Rect.IsEmpty)
            {
                CropRectangle = BackBufferArea;
                return;
            }

            GDI.Rectangle ControlRectangle = control.ClientRectangle;
            BitmapRectangle = CropRectangle; //new GDI.RectangleF(0,0, CropRectangle.Width, CropRectangle.Height);

            float ScaleX = (float)BitmapRectangle.Width / ControlRectangle.Width;
            float ScaleY = (float)BitmapRectangle.Height / ControlRectangle.Height;

            float TranslationX = (Rect.X + BitmapRectangle.X);
            float TranslationY = (Rect.Y + BitmapRectangle.Y);

            CropRectangle = new GDI.RectangleF(TranslationX, TranslationY,
                Rect.Width * ScaleX, Rect.Height * ScaleY);

            //BitmapRectangle = CropRectangle;

            Debug.WriteLine(CropRectangle.ToString());

            base.SetRectangle(Rect);
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

        struct Vertex
        {
            public Vector2 Position;
            //public Color4 Color;
            public uint Color;

            ///Color4 IS NOT THE SAME AS INT!!!

            public Vertex(int X, int Y, uint Color)
            {
                this.Position.X = X;
                this.Position.Y = Y;
                this.Color = Color;
            }
        }

    }

}
