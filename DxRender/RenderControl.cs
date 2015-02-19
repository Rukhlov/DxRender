using System.Windows.Forms;

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
            Renderer = CreateRender(Mode);
        }

        private RendererBase CreateRender(RenderMode Mode)
        {
            if(Mode==RenderMode.GDIPlus)
                return new GDIPlusRenderer(this.Handle, this.FrameSource);
            else
                return new SlimDXRenderer(this.Handle, this.FrameSource);
        }

        private RendererBase Renderer = null;
        private IFrameSource FrameSource = null;

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

            base.OnKeyDown(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Renderer.Draw(false);
        }
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            //do nothing
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            //MessageBox.Show(string.Format("OnMouseClick Location: {0},{1}", e.Location.X, e.Location.Y));
            base.OnMouseClick(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            //MessageBox.Show(string.Format("OnMouseWheel Delta: {0},", e.Delta));
            base.OnMouseWheel(e);
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