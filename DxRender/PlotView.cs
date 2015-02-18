using System.Windows.Forms;

namespace DxRender
{
    class PlotView : UserControl
    {
        public PlotView(SlimDXRenderer Renderer)
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.DoubleBuffered = false;
            this.Renderer = Renderer;
        }

        private SlimDXRenderer Renderer = null;

        protected override void OnPaint(PaintEventArgs e)
        {
            Renderer.Draw(false);
        }
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            //do nothing
        }

         
    }
}