using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;

namespace DxRender
{
    class RenderControl : UserControl
    {
        public RenderControl(IFrameSource FrameSource, RenderMode Mode = RenderMode.SlimDX)
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.DoubleBuffered = false;

            this.FrameSource = FrameSource;
            this.Renderer = CreateRender(Mode);

            AspectRatio =(float)FrameSource.VideoBuffer.Width / FrameSource.VideoBuffer.Height;
        }

        float AspectRatio = float.NaN; 

        private RendererBase CreateRender(RenderMode Mode)
        {
            if (Mode == RenderMode.GDIPlus)
                return new GDIPlusRenderer(this.Handle, this.FrameSource);
            else
                return new SlimDXRenderer(this.Handle, this.FrameSource);
        }

        private RendererBase Renderer = null;
        private IFrameSource FrameSource = null;

        Stopwatch stapwatch = new Stopwatch();
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                //...
                this.ParentForm.Close();
            }

            if (e.KeyCode == Keys.Space)
            {
                if (FrameSource != null)
                    FrameSource.Pause();
            }

            if (e.KeyCode == Keys.F1)
            {
                //...
            }
            base.OnKeyDown(e);
        }


        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                Renderer.Execute("SetSelection", new Rectangle());

            base.OnMouseDoubleClick(e);
        }

        Point StartPoint = new Point();
        Point EndPoint = new Point();

        Point MovePoint = new Point();

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if(e.KeyCode == Keys.R)
                Renderer.Execute("ChangeAspectRatio", true);


            base.OnKeyUp(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            StartPoint = EndPoint = e.Location;

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                this.Cursor = Cursors.SizeAll;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (StartPoint != EndPoint)
                {
                    Rectangle SelectionRectangle = GetSelectionRectangle(AspectRatio);

                    Renderer.Execute("SetSelection", SelectionRectangle);

                    //Debug.WriteLine("OnMouseUp" + SelectionRectangle.ToString());
                }

                StartPoint = new Point();
                EndPoint = new Point();
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                this.Cursor = Cursors.Default;
                StartPoint = new Point();
            }

            base.OnMouseUp(e);
        }

        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                EndPoint = e.Location;

                Rectangle SelectionRectangle = GetSelectionRectangle(AspectRatio); 

                Renderer.Execute("MouseMove", SelectionRectangle);
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                Point MouseLocation = e.Location;

                if (MouseLocation.X < 0) MouseLocation.X = 0;
                if (MouseLocation.Y < 0) MouseLocation.Y = 0;

                if (MouseLocation.X > this.Width) MouseLocation.X = this.Width;
                if (MouseLocation.Y > this.Height) MouseLocation.Y = this.Height;

                if (StartPoint.IsEmpty)
                    StartPoint = MouseLocation;

               // EndPoint = e.Location;

                Renderer.Execute("MousePan", StartPoint, MouseLocation);

                StartPoint = MouseLocation;
            }

            base.OnMouseMove(e);
        }


        private Rectangle GetSelectionRectangle(float AspectRatio = float.NaN)
        {
            if (StartPoint.X < 0) StartPoint.X = 0;
            if (StartPoint.Y < 0) StartPoint.Y = 0;

            if (EndPoint.X < 0) EndPoint.X = 0;
            if (EndPoint.Y < 0) EndPoint.Y = 0;


            if (StartPoint.X > this.Width) StartPoint.X = this.Width;
            if (StartPoint.Y > this.Height) StartPoint.Y = this.Height;

            if (EndPoint.X > this.Width) EndPoint.X = this.Width;
            if (EndPoint.Y > this.Height) EndPoint.Y = this.Height;

            int X = 0;
            int Y = 0;
            int Width = 0;
            int Height = 0;


            if (StartPoint.X > EndPoint.X)
            {
                X = EndPoint.X;
                Width = StartPoint.X - EndPoint.X;
            }
            else
            {
                X = StartPoint.X;
                Width = EndPoint.X - StartPoint.X;
            }

            if (StartPoint.Y > EndPoint.Y)
            {
                Y = EndPoint.Y;
                Height = StartPoint.Y - EndPoint.Y;
            }
            else
            {
                Y = StartPoint.Y;
                Height = EndPoint.Y - StartPoint.Y;
            }

            Rectangle SelectionRectangle = new Rectangle(X, Y, Width, Height);
            return SelectionRectangle;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            //MessageBox.Show(string.Format("OnMouseClick Location: {0},{1}", e.Location.X, e.Location.Y));
            base.OnMouseClick(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            MessageBox.Show(string.Format("OnMouseWheel Delta: {0},", e.Delta));
            base.OnMouseWheel(e);
        }
        protected override void OnResize(System.EventArgs e)
        {
            Renderer.ClientRectangle = this.ClientRectangle;

            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Renderer.Draw(false);
        }
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            //do nothing
        }

        protected override void Dispose(bool disposing)
        {
            if (Renderer != null)
            {
                Renderer.Dispose();
                Renderer = null;
            }

            base.Dispose(disposing);
        }


        public void ControlAspectRatio()
        {
            Control ViewPort = this;
            ViewPort.Dock = DockStyle.None;

            Control Container = ViewPort.Parent;
            if (Container == null) return;

            SetSize(ViewPort, Container);
            Container.Resize += (o, e) =>
            {
                this.SetSize();
            };

        }

        public void SetSize()
        {
            Control ViewPort = this;
            Control Container = ViewPort.Parent;
            if (Container == null) return;

            SetSize(ViewPort, Container);
        }


        private void SetSize(Control ViewPort, Control Container)
        {
            Rectangle ContainerRectangle = Container.ClientRectangle;

            float AspectRatio = (float)FrameSource.VideoBuffer.Width / FrameSource.VideoBuffer.Height;
            float ContainerRatio = (float)ContainerRectangle.Width / ContainerRectangle.Height;
            if (ContainerRatio < AspectRatio)
            {
                ViewPort.Width = ContainerRectangle.Width;
                ViewPort.Height = (int)(ViewPort.Width / AspectRatio);
                ViewPort.Top = (ContainerRectangle.Height - ViewPort.Height) / 2;
                ViewPort.Left = 0;
            }
            else
            {
                ViewPort.Height = ContainerRectangle.Height;
                ViewPort.Width = (int)(ViewPort.Height * AspectRatio);
                
                ViewPort.Top = 0;
                ViewPort.Left = (ContainerRectangle.Width - ViewPort.Width) / 2;
          }
        }
    }
}