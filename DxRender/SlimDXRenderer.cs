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

        GDI.RectangleF ViewRectangle;

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

            var Capabilities = Direct3D9.GetDeviceCaps(0, DeviceType.Hardware);


            PresentParams = CreatePresentParameters();

            //CreateFlags Flags =  CreateFlags.SoftwareVertexProcessing;
            //CreateFlags Flags = CreateFlags.HardwareVertexProcessing;
            CreateFlags Flags = CreateFlags.Multithreaded | CreateFlags.FpuPreserve | CreateFlags.HardwareVertexProcessing;
            GraphicDevice = new Device(Direct3D9, AdapterInfo.Adapter, DeviceType.Hardware, OwnerHandle, Flags, PresentParams);

            SpriteBatch = new Sprite(GraphicDevice);


            //BackBufferTexture = Texture.FromFile(GraphicDevice, "d:\\01.bmp", Usage.Dynamic, Pool.Default);

            // TODO: текстура должна настраиватся в зависимости от размеров и формата битмапа
            BackBufferTexture = new Texture(GraphicDevice,
                Width,
                Height,//!!!
                1,
                Usage.Dynamic,
                Format.X8R8G8B8,
                Pool.Default);


            BackBufferTextureSurface = BackBufferTexture.GetSurfaceLevel(0);

            //OffscreenSurface = Surface.CreateOffscreenPlain(GraphicDevice,
            //    Width,//PresentParams.BackBufferWidth,
            //    Height,//PresentParams.BackBufferHeight,
            //    PresentParams.BackBufferFormat,
            //    Pool.Default);

            ScreenFont = new Font(GraphicDevice, PerfCounter.Styler.Font);

            VertexElement[] VertexElems = new[] {
            	new VertexElement(0, 0,  DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.PositionTransformed, 0),
        		new VertexElement(0, 8, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.Color, 0),
				VertexElement.VertexDeclarationEnd };

            GraphicDevice.VertexDeclaration = new VertexDeclaration(GraphicDevice, VertexElems);

            BackBufferArea = new GDI.Rectangle(0, 0, PresentParams.BackBufferWidth, PresentParams.BackBufferHeight);
            DeviceLost = false;

            base.FrameSource.FrameReceived += FrameSource_FrameReceived;



            //BitmapRectangle = new GDI.RectangleF BackBufferArea;
            ViewRectangle = new GDI.RectangleF(0, 0, Width, Height);

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



            //GraphicDevice.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.None);
            //GraphicDevice.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Linear);

            // GraphicDevice.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.None);

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


        bool AspectRatioMode = false;

        public override void Draw(bool UpdateSurface = true)
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

                GDI.Rectangle ControlRectangle = control.ClientRectangle; //control.DisplayRectangle;
                Matrix Transform = Matrix.Identity;

                //if (buffer.UpsideDown)
                //{// поворачивем изображение на 180 град, со смещением
                //    //Transform = Matrix.Translation(-Width, -Height, 0) * Matrix.RotationZ((float)Math.PI) * Transform;
                //    Transform = Matrix.Translation(-ViewRectangle.Width, -ViewRectangle.Height, 0) * Matrix.RotationZ((float)Math.PI) * Transform;
                //}

                if (ControlRectangle.Width != Width || ControlRectangle.Height != Height)
                {
                    float ViewRatio = (float)ViewRectangle.Width / ViewRectangle.Height; //(float)Width / Height;
                    float ControlRatio = (float)ControlRectangle.Width / ControlRectangle.Height;

                    if (AspectRatioMode)
                    {
                        float ControlScaleX = (float)ControlRectangle.Width / ViewRectangle.Width;
                        float ControlScaleY = (float)ControlRectangle.Height / ViewRectangle.Height;

                        float TranslationX = 0;// -ViewRectangle.X * ControlScaleX;
                        float TranslationY = 0;//-ViewRectangle.Y * ControlScaleY;

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
                    }


                    //if (ViewRectangle.IsEmpty == false)
                    //{
                    //    //float ScaleX = (float)ControlRectangle.Width / CurrentViewRectangle.Width;
                    //    //float ScaleY = (float)ControlRectangle.Height / CurrentViewRectangle.Height;

                    //    //Transform = Matrix.Translation(-CurrentViewRectangle.X, -CurrentViewRectangle.Y, 0) * Matrix.Scaling(ScaleX, ScaleY, 1);

                    //    float ScaleX = (float)ControlRectangle.Width / ViewRectangle.Width;
                    //    float ScaleY = (float)ControlRectangle.Height / ViewRectangle.Height;

                    //    float TranslationX = -ViewRectangle.X * ScaleX;
                    //    float TranslationY = -ViewRectangle.Y * ScaleY;

                    //    Transform *= Matrix.Scaling(ScaleX, ScaleY, 1);//* Matrix.Translation(TranslationX, TranslationY, 0);
                    //}
                    //else
                    //{
                    //    float ControlScaleX = (float)ControlRectangle.Width / Width;
                    //    float ControlScaleY = (float)ControlRectangle.Height / Height;

                    //    Transform *= Matrix.Scaling(ControlScaleX, ControlScaleY, 1);

                    //    // Transform =  Matrix.Scaling(ControlScaleX, ControlScaleY, 1);
                    //    //Transform = Matrix.Scaling(0.3f, 0.3f, 1);
                    //}

                }





                //Transform = Matrix.Translation(10, 10, 0);// *Matrix.Scaling(2f, 2f, 1);

                if (buffer.UpsideDown)
                {// поворачивем изображение на 180 град, со смещением
                    //Transform = Matrix.Translation(-Width, -Height, 0) * Matrix.RotationZ((float)Math.PI) * Transform;
                    Transform = Matrix.Translation(-ViewRectangle.Width, -ViewRectangle.Height, 0) * Matrix.RotationZ((float)Math.PI) * Transform;

                }


                SpriteBatch.Begin(SpriteFlags.AlphaBlend);


                //SpriteBatch.Transform = view * Perspective * Transform;

                SpriteBatch.Transform = Transform;

                SpriteBatch.Draw(BackBufferTexture, GDI.Rectangle.Round(ViewRectangle), GDI.Color.White);

                //SpriteBatch.Draw(BackBufferTexture, new GDI.Rectangle(0,0,Width,Height), GDI.Color.White);

                // возвращаем все как было и рисуем дальше
                SpriteBatch.Transform = Matrix.Identity;

                ScreenFont.DrawString(SpriteBatch, PerfCounter.GetReport(), 0, 0, PerfCounter.Styler.Color);

                SpriteBatch.End();

                uint SelectionColor = 0x3FFF0000;
                if (SelectionRectangle.IsEmpty == false) DrawFilledRectangle(SelectionRectangle, SelectionColor);


                DrawRectangle(SelectionRectangle, 0xFFFF0000);


                //DrawLine(DisplayRectangle.Width / 2, 0, DisplayRectangle.Width / 2, DisplayRectangle.Height, 0xFF0000FF);
                //DrawLine(0, DisplayRectangle.Height/2, DisplayRectangle.Width , DisplayRectangle.Height/2, 0xFF0000FF);



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

                GraphicDevice.Present(ControlRectangle, ControlRectangle);

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
                    {
                        GDI.Rectangle Rect = (GDI.Rectangle)Parameters[0];
                        SelectionRectangle = Rect;
                    }
                    break;

                case "SetSelection":
                    {
                        GDI.Rectangle Rect = (GDI.Rectangle)Parameters[0];
                        SelectionRectangle = new GDI.Rectangle();

                        if (Rect.IsEmpty)
                        {
                            //CurrentViewRectangle = new GDI.RectangleF();//BackBufferArea;
                            ViewRectangle = new GDI.RectangleF(0, 0, Width, Height);
                            return;
                        }
                        else
                        {
                            if (ViewRectangle.IsEmpty)
                                ViewRectangle = new GDI.RectangleF(0, 0, Width, Height);
                        }

                        GDI.Rectangle ControlRectangle = control.ClientRectangle;

                        //if (AspectRatioMode)
                        //{
                        //    float ControlRatio = (float)ControlRectangle.Width / ControlRectangle.Height;
                        //    float ViewRatio = ViewRectangle.Width / ViewRectangle.Height;

                        //    if (ControlRectangle.Width > ControlRectangle.Height)
                        //    {
                        //        Rect.Width = (int)(Rect.Width * ViewRatio);

                        //    }
                        //    else
                        //    {
                        //        Rect.Height = (int)(Rect.Height / ViewRatio);
                        //    }

                        //    //GDI.Rectangle CorrectedControlRectangle = new GDI.Rectangle();
                        //    //if (ControlRatio < ViewRatio)
                        //    //{
                        //    //    CorrectedControlRectangle.Width = ControlRectangle.Width;
                        //    //    CorrectedControlRectangle.Height = (int)(ViewRectangle.Width / ViewRatio);
                        //    //    CorrectedControlRectangle.Y = (int)(ControlRectangle.Height - ViewRectangle.Height) / 2;
                        //    //    CorrectedControlRectangle.X = 0;
                        //    //}
                        //    //else
                        //    //{
                        //    //    CorrectedControlRectangle.Height = ControlRectangle.Height;
                        //    //    CorrectedControlRectangle.Width = (int)(ViewRectangle.Height * ViewRatio);

                        //    //    CorrectedControlRectangle.Y = 0;
                        //    //    CorrectedControlRectangle.X =(int) (ControlRectangle.Width - ViewRectangle.Width) / 2;
                        //    //}

                        //    //ControlRectangle = CorrectedControlRectangle;
                        //}


                        //float ScaleX = (float)ViewRectangle.Width / ControlRectangle.Width;
                        //float ScaleY = (float)ViewRectangle.Height / ControlRectangle.Height;

                        float ScaleX = (float)ViewRectangle.Width / ControlRectangle.Width;
                        float ScaleY = (float)ViewRectangle.Height / ControlRectangle.Height;

                        float TranslationX = (Rect.X * ScaleX + ViewRectangle.X);
                        float TranslationY = (Rect.Y * ScaleY + ViewRectangle.Y);

                        ViewRectangle = new GDI.RectangleF(TranslationX, TranslationY,
                            Rect.Width * ScaleX, Rect.Height * ScaleY);


                        Debug.WriteLine(ViewRectangle.ToString());
                    }

                    break;

                case "MousePan":
                    {
                        GDI.Point StartPoint = (GDI.Point)Parameters[0];
                        GDI.Point EndPoint = (GDI.Point)Parameters[1];

                        GDI.Rectangle ControlRectangle = control.ClientRectangle;

                        float ScaleX = (float)ViewRectangle.Width / ControlRectangle.Width;
                        float ScaleY = (float)ViewRectangle.Height / ControlRectangle.Height;

                        float TranslationX = (-(EndPoint.X - StartPoint.X) * ScaleX + ViewRectangle.X);
                        float TranslationY = (-(EndPoint.Y - StartPoint.Y) * ScaleY + ViewRectangle.Y);

                        if (TranslationX < 0) TranslationX = 0;
                        if (TranslationY < 0) TranslationY = 0;

                        if (TranslationX + ViewRectangle.Width > Width) TranslationX = ViewRectangle.X;
                        if (TranslationY + ViewRectangle.Height > Height) TranslationY = ViewRectangle.Y;

                        //Debug.WriteLine("TranslationX = {0} TranslationY = {1}", TranslationX, TranslationY);
                        //Debug.WriteLine("CurrentViewRectangle {0}", CurrentViewRectangle);

                        ViewRectangle = new GDI.RectangleF(TranslationX, TranslationY, ViewRectangle.Width, ViewRectangle.Height);


                        //Debug.WriteLine("Strat = {0} End = {1}", StartPoint, EndPoint);
                    }
                    break;

                case "ChangeAspectRatio":
                    AspectRatioMode = !AspectRatioMode;
                    break;

                default:
                    break;
            }
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

        public override void SetRectangle(GDI.Rectangle Rect)
        {

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
        private void __CopyToSurface____(MemoryBuffer buf, Surface surface)
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
                    int Offset = j * (NewStride);

                    t_data.Data.Write(bytes, Offset, NewStride);
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
