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

        private Texture BitmapTexture = null;
        private Surface TextureSurface = null;

        //private Surface OffscreenSurface;
        private Font ScreenFont;

        private Direct3D Direct3D9 = null;
        private AdapterInformation AdapterInfo = null;
        private bool DeviceLost = false;

        private MemoryBuffer buffer = null;

        GDI.RectangleF ViewRectangle;

        GDI.Rectangle SelectionRectangle;

        bool AspectRatioMode = false;

        private bool IsFullScreen = false;

        private Control control = null;
        public SlimDXRenderer(IntPtr Handle, IFrameSource FrameSource)
            : base(Handle, FrameSource)
        {
            buffer = FrameSource.VideoBuffer;

            control = Control.FromHandle(Handle);

            StartUp();

            //control.Resize += (o, a) => { };
        }

        private FormState RendererFormState = new FormState();

        private SynchronizationContext ThisContext = null;

        private void StartUp()
        {
            Debug.WriteLine("SlimDXRenderer.StartUp()");

            ThisContext = SynchronizationContext.Current;

            Direct3D9 = new Direct3D();

            AdapterInfo = Direct3D9.Adapters.DefaultAdapter;

            //var Capabilities = Direct3D9.GetDeviceCaps(0, DeviceType.Hardware);


            PresentParams = CreatePresentParameters();

            //CreateFlags Flags =  CreateFlags.SoftwareVertexProcessing;
            //CreateFlags Flags = CreateFlags.HardwareVertexProcessing;
            CreateFlags Flags = CreateFlags.Multithreaded | CreateFlags.FpuPreserve | CreateFlags.HardwareVertexProcessing;
            GraphicDevice = new Device(Direct3D9, AdapterInfo.Adapter, DeviceType.Hardware, OwnerHandle, Flags, PresentParams);

            SetRendererState();

            SpriteBatch = new Sprite(GraphicDevice);

            InitializeTexture();

            InitializeVertex();

            ScreenFont = new Font(GraphicDevice, PerfCounter.Styler.Font);

            ViewRectangle = new GDI.RectangleF(0, 0, Width, Height);
            BackBufferArea = new GDI.Rectangle(0, 0, PresentParams.BackBufferWidth, PresentParams.BackBufferHeight);

            base.FrameSource.FrameReceived += FrameSource_FrameReceived;

        }


        private PresentParameters CreatePresentParameters()
        {
            PresentParameters parameters = new PresentParameters();
            parameters.SwapEffect = SwapEffect.Discard;


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

        private void SetRendererState()
        {
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

        private void InitializeVertex()
        {
            VertexElement[] VertexElems = new[] {
                new VertexElement(0, 0,  DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.PositionTransformed, 0),
                new VertexElement(0, 8, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.Color, 0),
                VertexElement.VertexDeclarationEnd };

            GraphicDevice.VertexDeclaration = new VertexDeclaration(GraphicDevice, VertexElems);
        }

        private void InitializeTexture()
        {

            //BackBufferTexture = Texture.FromFile(GraphicDevice, "d:\\01.bmp", Usage.Dynamic, Pool.Default);

            // TODO: текстура должна настраиватся в зависимости от размеров и формата битмапа
            BitmapTexture = new Texture(GraphicDevice, Width, Height, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);

            TextureSurface = BitmapTexture.GetSurfaceLevel(0);
        }


        object locker = new object();
        private void FrameSource_FrameReceived(object sender, FrameReceivedEventArgs e)
        {
            lock (locker)
            {
                Draw();
                PerfCounter.UpdateStatistic(e.SampleTime);
            }
        }

        public override void Draw(bool UpdateSurface = true)
        {
            if (GraphicDevice == null) return;
            if (ReDrawing == true) return;
            if (TogglingFullScreen == true) return;

            var r = GraphicDevice.TestCooperativeLevel();
            if (r != ResultCode.Success)
            {
                //DeviceReady = false;
                if (r == ResultCode.DeviceNotReset)
                {
                    ResetDevice();
                    UpdateSurface = true;
                }
                if (r == ResultCode.DeviceLost)
                {
                    lock (locker)
                    {
                        ThisContext.Send((_) =>
                        {
                            OnLostDevice();
                        }, null);
                    }
                    DeviceLost = true;
                    //....

                }
                Debug.WriteLine("GraphicDevice.TestCooperativeLevel() = " + r);
                return;
            }

            //if (DeviceLost)
            //{
            //    if (GraphicDevice.TestCooperativeLevel() == ResultCode.DeviceNotReset)
            //    {
            //        lock (locker)
            //        {
            //            ThisContext.Send((_) =>
            //            {
            //                GraphicDevice.Reset(PresentParams);
            //                DeviceLost = false;
            //                OnDeviceReset();
            //            }, null);
            //        }
            //    }
            //    else
            //    {
            //        Thread.Sleep(100);
            //        return;
            //    }
            //}

            try
            {
                ReDrawing = true;
                GraphicDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, GDI.Color.Black, 1.0f, 0);

                GraphicDevice.BeginScene();


                if (UpdateSurface)
                {
                    //CopyToSurface(BackBufferTextureSurface, TestBmp, new System.Drawing.Rectangle(0 ,0 , TestBmp.Width, TestBmp.Height)/*this.ClientRectangle*/);
                    CopyToSurface(buffer, TextureSurface);
                }

                //SpriteBatch.Begin(SpriteFlags.AlphaBlend);
                //SpriteBatch.Draw(BackBufferTexture, new GDI.Rectangle(0,0, Width, Height), GDI.Color.White);

                //SpriteBatch.End();
                //GraphicDevice.EndScene();
                //GraphicDevice.Present();



                GDI.Rectangle ControlRectangle = IsFullScreen ? BackBufferArea : control.ClientRectangle;

                //Debug.WriteLine(ControlRectangle);
                //Debug.WriteLine(ViewRectangle);

                //GDI.Rectangle ControlRectangle = control.DisplayRectangle;
                Matrix Transform = Matrix.Identity;


                //if (ControlRectangle.Width != Width || ControlRectangle.Height != Height)
                {
                    float ViewRatio = (float)ViewRectangle.Width / ViewRectangle.Height; //(float)Width / Height;

                    float ControlRatio = (float)ControlRectangle.Width / ControlRectangle.Height;

                    if (AspectRatioMode)
                    {
                        float ControlScaleX = (float)ControlRectangle.Width / ViewRectangle.Width;
                        float ControlScaleY = (float)ControlRectangle.Height / ViewRectangle.Height;

                        float TranslationX = 0;
                        float TranslationY = 0;

                        if (ControlRatio < ViewRatio)
                        {
                            ControlScaleX = (float)ControlRectangle.Width / ViewRectangle.Width;

                            float CorrectedControlHeight = (float)ControlRectangle.Width / ViewRatio;
                            ControlScaleY = (float)CorrectedControlHeight / ViewRectangle.Height;

                            TranslationY = (ControlRectangle.Height - ViewRectangle.Height * ControlScaleY) / 2;
                        }
                        else
                        {
                            float CorrectedControlWidth = (float)ControlRectangle.Height * ViewRatio;

                            ControlScaleX = CorrectedControlWidth / ViewRectangle.Width;

                            TranslationX = (ControlRectangle.Width - ViewRectangle.Width * ControlScaleX) / 2;

                            ControlScaleY = (float)ControlRectangle.Height / ViewRectangle.Height;
                        }

                        Transform *= Matrix.Scaling(ControlScaleX, ControlScaleY, 1) * Matrix.Translation(TranslationX, TranslationY, 0);
                    }
                    else
                    {
                        float ControlScaleX = (float)ControlRectangle.Width / ViewRectangle.Width;
                        float ControlScaleY = (float)ControlRectangle.Height / ViewRectangle.Height;

                        Transform *= Matrix.Scaling(ControlScaleX, ControlScaleY, 1);

                        //Debug.WriteLine("ControlScaleX = {0} ControlScaleY {1}", ControlScaleX, ControlScaleY);
                    }
                }

                //if (buffer.UpsideDown)
                //{// поворачивем изображение на 180 град, со смещением
                //    //Transform = Matrix.Translation(-Width, -Height, 0) * Matrix.RotationZ((float)Math.PI) * Transform;
                //    Transform = Matrix.Translation(-ViewRectangle.Width, -ViewRectangle.Height, 0) * Matrix.RotationZ((float)Math.PI) * Transform;

                //}

                SpriteBatch.Begin(SpriteFlags.AlphaBlend);

                SpriteBatch.Transform = Transform;

                GDI.Rectangle rect = GDI.Rectangle.Round(ViewRectangle);
                //rect.Inflate(1, 1);
                //var rect = new GDI.Rectangle((int)ViewRectangle.X, (int)ViewRectangle.Y, (int)ViewRectangle.Width+1, (int)ViewRectangle.Height+1);

                SpriteBatch.Draw(BitmapTexture, rect, GDI.Color.White);

                //SpriteBatch.Draw(BackBufferTexture, new GDI.Rectangle(0,0,Width,Height), GDI.Color.White);

                // возвращаем все как было и рисуем дальше
                SpriteBatch.Transform = Matrix.Identity;

                ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, PerfCounter.Styler.Color);

                SpriteBatch.End();

                //SelectionRectangle = new GDI.Rectangle(0, 0, 100, 100);//new GDI.Rectangle(0, 0, 100, 100);
                if (SelectionRectangle != null && SelectionRectangle.IsEmpty == false)
                {
                    uint SelectionColor = 0x3FFF0000;
                    DrawFilledRectangle(SelectionRectangle, SelectionColor);
                    DrawRectangle(SelectionRectangle, 0xFFFF0000);
                }



                //DrawLine(DisplayRectangle.Width / 2, 0, DisplayRectangle.Width / 2, DisplayRectangle.Height, 0xFF0000FF);
                //DrawLine(0, DisplayRectangle.Height/2, DisplayRectangle.Width , DisplayRectangle.Height/2, 0xFF0000FF);

                GraphicDevice.EndScene();

                if (IsFullScreen)
                    GraphicDevice.Present();
                else
                    GraphicDevice.Present(ControlRectangle, ControlRectangle);

                //GraphicDevice.Present(BackBufferArea, BackBufferArea);

                //Thread.Sleep(1000);
            }
            catch (Direct3D9Exception ex)
            {
                if (ex.ResultCode == ResultCode.DeviceLost)
                    DeviceLost = true;


                //if (ex.ResultCode == ResultCode.DeviceLost)
                //{
                //    lock (locker)
                //    {
                //        ThisContext.Send((_) =>
                //        {
                //            OnLostDevice();
                //            DeviceLost = true;
                //        }, null);
                //    }

                //}
                //else
                //{
                //    throw;
                //}


                Debug.WriteLine(ex.Message);
            }
            finally { ReDrawing = false; }
        }



        public override void Execute(string Command, params object[] Parameters)
        {
            switch (Command)
            {
                case "MouseMove":
                    {
                        GDI.Rectangle Rect = (GDI.Rectangle)Parameters[0];
                        SelectionRectangle = Rect;
                    }
                    break;

                case "SetSelection":
                    { // 
                        GDI.Rectangle Rect = (GDI.Rectangle)Parameters[0];
                        SelectionRectangle = new GDI.Rectangle();

                        if (Rect.IsEmpty)
                        {// отменяем масшабирование
                            ViewRectangle = new GDI.RectangleF(0, 0, Width, Height);
                            return;
                        }

                        if (ViewRectangle.IsEmpty) ViewRectangle = new GDI.RectangleF(0, 0, Width, Height);

                        //GDI.Rectangle ControlRectangle = control.ClientRectangle;
                        GDI.Rectangle ControlRectangle = IsFullScreen ? BackBufferArea : control.ClientRectangle;

                        Debug.WriteLine(ControlRectangle);

                        if (AspectRatioMode)
                        {// если включен режим отображения с учетом соотношения сторон 
                            // пересчитываем координаты области выделенной пользователем 
                            float ViewRatio = ViewRectangle.Width / ViewRectangle.Height;
                            float ControlRatio = (float)ControlRectangle.Width / ControlRectangle.Height;

                            if (ControlRatio > ViewRatio)
                            {// масштабируем по ширине

                                // получаем область выдимую пользователем с учетом соотношения сторон
                                float ViewWidth = ControlRectangle.Height * ViewRatio;
                                float ViewHeight = ViewRectangle.Height;

                                float ViewLeft = (ControlRectangle.Width - ViewWidth) / 2;
                                float ViewRight = ViewLeft + ViewWidth;

                                float ViewTop = ViewRectangle.Y;
                                float ViewBottom = ViewTop + ViewHeight;

                                float ScaleFactorX = ControlRectangle.Width / ViewWidth;

                                // коректируем область выделения
                                float RectLeft = Rect.X;
                                float RectRight = RectLeft + Rect.Width;

                                // проверяем границы 
                                if (RectRight < ViewLeft) return;
                                if (RectLeft > ViewRight) return;

                                if (RectLeft < ViewLeft && RectRight > ViewRight)
                                {// если область выделенная пользователем больше изображения с учетом масштабирования сторон
                                    Rect.Width = Convert.ToInt32((ViewRight - ViewLeft) * ScaleFactorX);
                                    Rect.X = 0;
                                }
                                else if (RectLeft <= ViewLeft)
                                { // левый край выходит за границу изображения                           
                                    Rect.X = 0;
                                    Rect.Width = Convert.ToInt32((Rect.Width - (ViewLeft - RectLeft)) * ScaleFactorX);
                                }
                                else if (RectRight >= ViewRight)
                                {// правый край выходит за границу изображения  
                                    RectLeft = RectLeft - ViewLeft;

                                    Rect.X = Convert.ToInt32(RectLeft * ScaleFactorX);
                                    Rect.Width = Convert.ToInt32((ViewRight - RectLeft - ViewLeft) * ScaleFactorX);
                                }
                                else
                                { //область в пределах видимого изображения
                                    Rect.X = Convert.ToInt32((RectLeft - ViewLeft) * ScaleFactorX);
                                    Rect.Width = Convert.ToInt32(Rect.Width * ScaleFactorX);
                                }
                            }
                            else
                            {// масштабируем по высоте
                                // получаем область выдимую пользователем с учетом соотношения сторон
                                float ViewHeight = ControlRectangle.Width / ViewRatio;
                                float ViewTop = (ControlRectangle.Height - ViewHeight) / 2;
                                float ViewBottom = ViewTop + ViewHeight;

                                float ScaleFactorY = ControlRectangle.Height / ViewHeight;

                                float RectTop = Rect.Y;
                                float RectBottom = RectTop + Rect.Height;

                                if (RectBottom < ViewTop) return;
                                if (RectTop > ViewBottom) return;

                                if (RectTop < ViewTop && RectBottom > ViewBottom)
                                {
                                    Rect.Height = Convert.ToInt32((ViewBottom - ViewTop) * ScaleFactorY);
                                    Rect.Y = 0;
                                }
                                else if (RectTop <= ViewTop)
                                {
                                    Rect.Y = 0;
                                    Rect.Height = Convert.ToInt32((Rect.Height - (ViewTop - RectTop)) * ScaleFactorY);
                                }
                                else if (RectBottom >= ViewBottom)
                                {
                                    RectTop = RectTop - ViewTop;

                                    Rect.Y = Convert.ToInt32(RectTop * ScaleFactorY);
                                    Rect.Height = Convert.ToInt32((ViewBottom - RectTop - ViewTop) * ScaleFactorY);
                                }
                                else
                                {
                                    Rect.Y = Convert.ToInt32((RectTop - ViewTop) * ScaleFactorY);
                                    Rect.Height = Convert.ToInt32(Rect.Height * ScaleFactorY);
                                }
                            }
                        }

                        float ScaleX = (float)ViewRectangle.Width / ControlRectangle.Width;
                        float ScaleY = (float)ViewRectangle.Height / ControlRectangle.Height;

                        float ViewX = (Rect.X * ScaleX + ViewRectangle.X);
                        float ViewY = (Rect.Y * ScaleY + ViewRectangle.Y);
                        float ScaledWidth = Rect.Width * ScaleX;
                        float ScaledHeight = Rect.Height * ScaleY;

                        float MinViewWidth = Width / 100f;
                        float MinViewHeight = Height / 100f;

                        if ((ScaledWidth > MinViewWidth) && (ScaledHeight > MinViewHeight))
                        {
                            ViewRectangle = new GDI.RectangleF(ViewX, ViewY, ScaledWidth, ScaledHeight);
                        }

                        //ViewRectangle = new GDI.RectangleF(320, 240,  100, 240);


                        Debug.WriteLine(ViewRectangle.ToString());
                    }

                    break;

                case "MousePan":
                    { // двигаем изображение в соответствии с полученными координатами
                        GDI.Point StartPoint = (GDI.Point)Parameters[0];
                        GDI.Point EndPoint = (GDI.Point)Parameters[1];

                        //GDI.Rectangle ControlRectangle = control.ClientRectangle;
                        GDI.Rectangle ControlRectangle = IsFullScreen ? BackBufferArea : control.ClientRectangle;

                        float ScaleX = (float)ViewRectangle.Width / ControlRectangle.Width;
                        float ScaleY = (float)ViewRectangle.Height / ControlRectangle.Height;

                        float ViewX = (ViewRectangle.X - (EndPoint.X - StartPoint.X) * ScaleX);
                        float ViewY = (ViewRectangle.Y - (EndPoint.Y - StartPoint.Y) * ScaleY);

                        // проверяем границы изображения 
                        if (ViewX < 0) ViewX = 0;
                        if (ViewY < 0) ViewY = 0;

                        if (ViewX + ViewRectangle.Width > Width) ViewX = ViewRectangle.X;
                        if (ViewY + ViewRectangle.Height > Height) ViewY = ViewRectangle.Y;

                        ViewRectangle = new GDI.RectangleF(ViewX, ViewY, ViewRectangle.Width, ViewRectangle.Height);
                    }
                    break;

                case "ChangeAspectRatio":
                    // изменение режим соотношения сторон
                    AspectRatioMode = !AspectRatioMode;
                    break;

                case "ChangeFullScreen":
                    {
                        
                        lock (locker)
                        {
                            ToggleFullScreen();
                            IsFullScreen = !IsFullScreen;
                        }
                        //CahngeFullScreenMode = true;
                    }
                    break;
                default:
                    break;
            }

           // Debug.WriteLine(Command);
        }


        public void DrawFilledRectangle(GDI.Rectangle Rectangle, uint Color)
        {
            if (Rectangle.IsEmpty) return;

            Vertex[] data = new Vertex[5];
            data[0].Position.X = Rectangle.Left;
            data[0].Position.Y = Rectangle.Top;
            data[0].Color = Color;

            data[1].Position.X = Rectangle.Right;
            data[1].Position.Y = Rectangle.Top;
            data[1].Color = Color;

            data[2].Position.X = Rectangle.Left;
            data[2].Position.Y = Rectangle.Bottom;
            data[2].Color = Color;

            data[3].Position.X = Rectangle.Right;
            data[3].Position.Y = Rectangle.Bottom;
            data[3].Color = Color;

            data[4].Position.X = Rectangle.Left;
            data[4].Position.Y = Rectangle.Top;
            data[4].Color = Color;

            GraphicDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, 0, 2, data);

        }

        public void DrawRectangle(GDI.Rectangle Rectangle, uint Color)
        {
            if (Rectangle.IsEmpty) return;

            Vertex[] v = new Vertex[5];
            v[0].Position.X = Rectangle.Left;
            v[0].Position.Y = Rectangle.Top;
            v[0].Color = Color;

            v[1].Position.X = Rectangle.Right;
            v[1].Position.Y = Rectangle.Top;
            v[1].Color = Color;

            v[2].Position.X = Rectangle.Right;
            v[2].Position.Y = Rectangle.Bottom;
            v[2].Color = Color;

            v[3].Position.X = Rectangle.Left;
            v[3].Position.Y = Rectangle.Bottom;
            v[3].Color = Color;

            v[4].Position.X = Rectangle.Left;
            v[4].Position.Y = Rectangle.Top;
            v[4].Color = Color;

            GraphicDevice.DrawUserPrimitives(PrimitiveType.LineStrip, 0, v.Count() - 1, v);
        }


        public void DrawLine(int x1, int y1, int x2, int y2, uint Color)
        {
            Vertex[] data = new Vertex[2];

            data[0].Position.X = x1;
            data[0].Position.Y = y1;
            data[0].Color = Color;

            data[1].Position.X = x2;
            data[1].Position.Y = y2;
            data[1].Color = Color;

            GraphicDevice.DrawUserPrimitives(PrimitiveType.LineList, 0, 1, data);

        }

        public void PolyLine(System.Drawing.Point[] points, uint Color)
        {
            int Count = points.Count();

            if (Count < 2) return;
            Vertex[] data = new Vertex[Count];
            for (int i = 0; i < Count; i++)
            {
                data[i].Position.X = points[i].X;
                data[i].Position.Y = points[i].Y;
                data[i].Color = Color;
            }
            GraphicDevice.DrawUserPrimitives(PrimitiveType.LineStrip, 0, Count - 1, data);

        }

        private void CopyToSurface(MemoryBuffer buf, Surface surface)
        {
            lock (locker)
            {
                //Debug.WriteLine("SlimDXRenderer.CopyToSurface()");
                DataRectangle t_data = surface.LockRectangle(LockFlags.None);

                if (buf.UpsideDown)
                {// если битмап перевернут копируем снизу вверх
                    int NewStride = buf.Width * 4;
                    int RestStride = t_data.Pitch - NewStride;

                    for (int j = buf.Height; j > 0; j--)
                    {
                        int Offset = (j - 1) * NewStride;
                        t_data.Data.WriteRange(buf.Data.Scan0 + Offset, NewStride);
                        t_data.Data.Position = t_data.Data.Position + RestStride;
                    }
                }
                else
                {
                    int NewStride = buf.Width * 4;
                    int RestStride = t_data.Pitch - NewStride;
                    for (int j = 0; j < buf.Height; j++)
                    {
                        int Offset = j * (NewStride);
                        t_data.Data.WriteRange(buf.Data.Scan0 + Offset, NewStride);
                        t_data.Data.Position = t_data.Data.Position + RestStride;
                    }
                }

                surface.UnlockRectangle();

            }
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


        #region BAK

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

        //private void CopyToSurface3(MemoryBuffer buf, Surface surface)
        //{
        //    lock (buf)
        //    {
        //        int bufferSize = buf.Data.Size;

        //        byte[] bytes = new byte[bufferSize];


        //        System.Runtime.InteropServices.Marshal.Copy(buf.Data.Scan0, bytes, 0, bytes.Length);

        //        DataRectangle t_data = surface.LockRectangle(LockFlags.None);

        //        int NewStride = buf.Width * 4;
        //        int RestStride = t_data.Pitch - NewStride;
        //        for (int j = 0; j < buf.Height; j++)
        //        {
        //            int Offset = j * (NewStride);

        //            t_data.Data.Write(bytes, Offset, NewStride);
        //            t_data.Data.Position = t_data.Data.Position + RestStride;
        //        }
        //        surface.UnlockRectangle();

        //    }

        //}

        //private void CopyToSurface(IntPtr ptr, int Size, Surface surface)
        //{
        //    lock (surface)
        //    {
        //        DataRectangle SurfaceRectangle = surface.LockRectangle(LockFlags.None);
        //        SurfaceRectangle.Data.WriteRange(ptr, Size);
        //        surface.UnlockRectangle();
        //    }
        //}

        ////[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        //private void __CopyToSurface(MemoryBuffer buf, Surface surface)
        //{
        //    lock (buf)
        //    {
        //        MappedData data = buf.Data;
        //        DataRectangle SurfaceRectangle = surface.LockRectangle(LockFlags.None);
        //        SurfaceRectangle.Data.WriteRange(data.Scan0, data.Size);
        //        surface.UnlockRectangle();
        //    }
        //}


        //private void _____CopyToSurface(Surface surface, GDI.Bitmap bitmap, GDI.Rectangle SurfaceArea)
        //{
        //    DataRectangle SurfaceRectangle = surface.LockRectangle(LockFlags.None);

        //    GDI.Imaging.BitmapData BitmapRectangle = bitmap.LockBits(SurfaceArea,
        //        GDI.Imaging.ImageLockMode.ReadOnly, GDI.Imaging.PixelFormat.Format32bppArgb);

        //    SurfaceRectangle.Data.WriteRange(BitmapRectangle.Scan0, BitmapRectangle.Stride * BitmapRectangle.Height);
        //    bitmap.UnlockBits(BitmapRectangle);

        //    surface.UnlockRectangle();
        //}

        ////http://stackoverflow.com/questions/16493702/copying-gdi-bitmap-into-directx-texture
        //private void __CopyToSurface____(MemoryBuffer buf, Surface surface)
        //{
        //    lock (buf)
        //    {
        //        int bufferSize = buf.Data.Size;

        //        byte[] bytes = new byte[bufferSize];


        //        System.Runtime.InteropServices.Marshal.Copy(buf.Data.Scan0, bytes, 0, bytes.Length);

        //        DataRectangle t_data = surface.LockRectangle(LockFlags.None);

        //        int NewStride = buf.Width * 4;
        //        int RestStride = t_data.Pitch - NewStride;
        //        for (int j = 0; j < buf.Height; j++)
        //        {
        //            t_data.Data.Write(bytes, j * (NewStride), NewStride);
        //            t_data.Data.Position = t_data.Data.Position + RestStride;
        //        }

        //        surface.UnlockRectangle();

        //    }

        //}




        //unsafe private void CopyToSurface(Surface surface, GDI.Bitmap bitmap, GDI.Rectangle SurfaceArea)
        //{
        //    GDI.Imaging.BitmapData bitmapData = bitmap.LockBits(SurfaceArea,
        //        GDI.Imaging.ImageLockMode.ReadOnly, GDI.Imaging.PixelFormat.Format32bppArgb);

        //    int bitsPerPixel = 24;
        //    int Stride = 4 * ((bitmapData.Width * ((bitsPerPixel + 7) / 8) + 3) / 4);

        //    int bufferSize = bitmapData.Height * Stride;//bitmapData.Stride;
        //    byte[] bytes = new byte[bufferSize];
        //    System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);
        //    DataRectangle t_data = surface.LockRectangle(LockFlags.None);
        //    int NewStride = bitmap.Width * 4;
        //    int RestStride = t_data.Pitch - NewStride;
        //    for (int j = 0; j < bitmap.Height; j++)
        //    {
        //        t_data.Data.Write(bytes, j * (NewStride), NewStride);
        //        t_data.Data.Position = t_data.Data.Position + RestStride;
        //    }

        //    //DataRectangle t_data = surface.LockRectangle(LockFlags.None);

        //    //int pitch = ((int)t_data.Pitch / sizeof(ushort));
        //    //int bitmapPitch = ((int)bitmapData.Stride / sizeof(ushort));

        //    //DataStream d_stream = t_data.Data;
        //    //ushort* to = (ushort*)d_stream.DataPointer;
        //    //ushort* from = (ushort*)bitmapData.Scan0.ToPointer();

        //    //for (int j = 0; j < bitmap.Height; j++)
        //    //    for (int i = 0; i < bitmapPitch; i++)
        //    //        to[i + j * pitch] = from[i + j * bitmapPitch];

        //    bitmap.UnlockBits(bitmapData);

        //    surface.UnlockRectangle();
        //}

        //private void ___CopyToSurface(Surface surface, GDI.Bitmap bitmap, GDI.Rectangle SurfaceArea)
        //{
        //    GDI.Imaging.BitmapData bitmapData = bitmap.LockBits(SurfaceArea,
        //        GDI.Imaging.ImageLockMode.ReadOnly, GDI.Imaging.PixelFormat.Format32bppArgb);

        //    //SurfaceRectangle.Data.WriteRange(BitmapRectangle.Scan0, BitmapRectangle.Stride * BitmapRectangle.Height);

        //    int bufferSize = bitmapData.Height * bitmapData.Stride;

        //    //create data buffer 
        //    byte[] bytes = new byte[bufferSize];

        //    // copy bitmap data into buffer
        //    System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);

        //    DataRectangle t_data = surface.LockRectangle(LockFlags.None);
        //    //var t_data = _resulttexture.LockRectangle(0, LockFlags.None);

        //    //int NewStride = bitmap.Width * 4;
        //    //int RestStride = t_data.Pitch - NewStride;
        //    //for (int j = 0; j < bitmap.Height; j++)
        //    //{
        //    //    t_data.Data.Write(bytes, j * NewStride, NewStride);
        //    //    t_data.Data.Position = t_data.Data.Position + RestStride;
        //    //}

        //    int pitch = ((int)t_data.Pitch / sizeof(ushort));
        //    int bitmapPitch = ((int)bitmapData.Stride / sizeof(ushort));

        //    DataStream d_stream = t_data.Data;
        //    unsafe
        //    {
        //        ushort* to = (ushort*)d_stream.DataPointer;
        //        ushort* from = (ushort*)bitmapData.Scan0.ToPointer();

        //        for (int j = 0; j < bitmap.Height; j++)
        //            for (int i = 0; i < bitmapPitch; i++)
        //                to[i + j * pitch] = from[i + j * bitmapPitch];
        //    }

        //    bitmap.UnlockBits(bitmapData);
        //    surface.UnlockRectangle();
        //} 

        #endregion

        public override void Dispose()
        {
            CleanUp();

            if (PerfCounter != null)
                PerfCounter.Dispose();

            base.Dispose();
        }

        bool TogglingFullScreen = false;

        public void ToggleFullScreen()
        {
            Debug.WriteLine("SlimDXRenderer.ToggleFullScreen()");
            TogglingFullScreen = true;

            Form form = control as Form;
            if (form == null) return;

            OnLostDevice();

            if (PresentParams.Windowed)
            {
                RendererFormState.BorderStyle = form.FormBorderStyle;
                RendererFormState.WindowState = form.WindowState;

                RendererFormState.Width = form.Width;
                RendererFormState.Height = form.Height;

                // Only normal window can be used in full screen.
                if (form.WindowState != FormWindowState.Normal)
                    form.WindowState = FormWindowState.Normal;

                //form.Width = AdapterInfo.CurrentDisplayMode.Width;
                //form.Height = AdapterInfo.CurrentDisplayMode.Height;
                form.Capture = true;

                form.FormBorderStyle = FormBorderStyle.None;

                PresentParams.BackBufferWidth = AdapterInfo.CurrentDisplayMode.Width;
                PresentParams.BackBufferHeight = AdapterInfo.CurrentDisplayMode.Height;

                //IntPtr DesktopHandle = GetDC(IntPtr.Zero);
                //PresentParams.DeviceWindowHandle = DesktopHandle;

                PresentParams.Windowed = false;
            }
            else
            {
                //PresentParams.BackBufferWidth = Width;
                //PresentParams.BackBufferHeight = Height;

                form.Capture = false;

                PresentParams.BackBufferWidth = AdapterInfo.CurrentDisplayMode.Width;
                PresentParams.BackBufferHeight = AdapterInfo.CurrentDisplayMode.Height;

                form.FormBorderStyle = RendererFormState.BorderStyle;
                form.WindowState = RendererFormState.WindowState;

                if (form.WindowState == FormWindowState.Normal)
                {
                    form.Width = RendererFormState.Width;
                    form.Height = RendererFormState.Height;
                }

                PresentParams.DeviceWindowHandle = form.Handle;

                PresentParams.Windowed = true;

            }

            GraphicDevice.Reset(PresentParams);
            OnDeviceReset();

            TogglingFullScreen = false;
        }

        private void ResetDevice()
        {
            lock (locker)
            {
                ThisContext.Send((_) =>
                {
                    OnLostDevice();
                    GraphicDevice.Reset(PresentParams);
                    OnDeviceReset();
                }, null);
                Debug.WriteLine("SlimDXRenderer.ResetDevice()");
            }

        }

        private void OnLostDevice()
        {
            Debug.WriteLine("SlimDXRenderer.OnLostDevice()");
            //FrameSource.FrameReceived -= FrameSource_FrameReceived;
            if (BitmapTexture != null && BitmapTexture.Disposed == false)
            {
                BitmapTexture.Dispose();
                BitmapTexture = null;
            }

            if (TextureSurface != null && TextureSurface.Disposed == false)
            {
                TextureSurface.Dispose();
                TextureSurface = null;
            }

            //if (SpriteBatch != null && SpriteBatch.Disposed == false)
            //{
            //    SpriteBatch.Dispose();
            //    SpriteBatch = null;
            //}

            //if (ScreenFont != null && ScreenFont.Disposed == false)
            //{
            //    ScreenFont.Dispose();
            //    ScreenFont = null;
            //}

            if (SpriteBatch != null)
                SpriteBatch.OnLostDevice();

            if (ScreenFont != null)
                ScreenFont.OnLostDevice();
        }


        private void OnDeviceReset()
        {
            Debug.WriteLine("SlimDXRenderer.OnDeviceReset()");

            SetRendererState();

            if (SpriteBatch != null) SpriteBatch.OnResetDevice();
            if (ScreenFont != null) ScreenFont.OnResetDevice();

            InitializeTexture();
            InitializeVertex();

            ViewRectangle = new GDI.RectangleF(0, 0, Width, Height);
            BackBufferArea = new GDI.Rectangle(0, 0, PresentParams.BackBufferWidth, PresentParams.BackBufferHeight);
        }

        private void CleanUp()
        {
            Debug.WriteLine("SlimDXRenderer.CleanUp()");

            if (FrameSource != null)
                FrameSource.FrameReceived -= FrameSource_FrameReceived;

            if (Direct3D9 != null && Direct3D9.Disposed == false)
            {
                Direct3D9.Dispose();
                Direct3D9 = null;
            }

            if (GraphicDevice != null && GraphicDevice.Disposed == false)
            {
                GraphicDevice.Dispose();
                GraphicDevice = null;
            }

            if (BitmapTexture != null && BitmapTexture.Disposed == false)
            {
                BitmapTexture.Dispose();
                BitmapTexture = null;
            }

            if (TextureSurface != null && TextureSurface.Disposed == false)
            {
                TextureSurface.Dispose();
                TextureSurface = null;
            }


            if (SpriteBatch != null && SpriteBatch.Disposed == false)
            {
                SpriteBatch.Dispose();
                SpriteBatch = null;
            }

            if (ScreenFont != null && ScreenFont.Disposed == false)
            {
                ScreenFont.Dispose();
                ScreenFont = null;
            }
        }


        internal int CopyToSurfaceTest()
        {
            CopyToSurface(buffer, TextureSurface);
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

        class FormState
        {
            public int Width { get; set; }
            public int Height { get; set; }

            public FormWindowState WindowState { get; set; }
            public FormBorderStyle BorderStyle { get; set; }   
        }

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);
    }


}
