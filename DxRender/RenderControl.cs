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
        }

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
            if(e.Button == System.Windows.Forms.MouseButtons.Left)
                Renderer.SetRectangle(new Rectangle());

            base.OnMouseDoubleClick(e);
        }

        Point StartPoint = new Point();
        Point EndPoint = new Point();


        protected override void OnMouseDown(MouseEventArgs e)
        {
            StartPoint = EndPoint = e.Location;

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (StartPoint != EndPoint)
            {

                //if (StartPoint.X < EndPoint.X)
                //{
                    Rectangle CropRectangle = new Rectangle(StartPoint.X, StartPoint.Y, EndPoint.X - StartPoint.X, EndPoint.Y - StartPoint.Y);

                    Renderer.SetRectangle(CropRectangle);
                    Debug.WriteLine(CropRectangle.ToString());
                //}
                //else
                //{
                //    Renderer.SetRectangle(new Rectangle());
                //}

                //MessageBox.Show(string.Format("X={0} Y={1} Width={2} Height={3}",
                //    CropRectangle.X, CropRectangle.Y, CropRectangle.Width, CropRectangle.Height));
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if(e.Button != System.Windows.Forms.MouseButtons.Left) return;
            EndPoint = e.Location;

            Rectangle CropRectangle = new Rectangle(StartPoint.X, StartPoint.Y, EndPoint.X - StartPoint.X, EndPoint.Y - StartPoint.Y);

            Renderer.Execute("MouseMove", CropRectangle);

            base.OnMouseMove(e);
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
    }
}