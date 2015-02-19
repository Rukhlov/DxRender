using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace DxRender
{
    class GDIPlusRenderer : RendererBase
    {
        public GDIPlusRenderer(IntPtr Handle, IFrameSource FrameSource)
            : base(Handle, FrameSource)
        {
            base.FrameSource.FrameReceived += FrameSource_FrameReceived;
            this.CurrentBitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        }

        private Bitmap CurrentBitmap = null;

        private void FrameSource_FrameReceived(object sender, FrameReceivedEventArgs e)
        {
            Draw();
            PerfCounter.UpdateStatistic(e.SampleTime);
        }

        public override void Draw(bool UpdateSurface = true)
        {
            if (ReDrawing == true) return;
            try
            {
                ReDrawing = true;
                Graphics graphics = Graphics.FromHwnd(base.OwnerHandle);
                if (UpdateSurface)
                {
                    MappedData data = this.FrameSource.VideoBuffer.Data;
                    BitmapData bits = CurrentBitmap.LockBits(new Rectangle(0, 0, CurrentBitmap.Width, CurrentBitmap.Height),
                        ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    NativeMethods.CopyMemory(bits.Scan0, data.Scan0, data.Size);
                    CurrentBitmap.UnlockBits(bits);

                    if (this.FrameSource.VideoBuffer.UpsideDown)
                        CurrentBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                    Graphics g = Graphics.FromImage(CurrentBitmap);
                    g.DrawString(PerfCounter.GetReport(), PerfCounter.Styler.Font, PerfCounter.Styler.Brush, 10f, 10f);
                    g.Dispose();
                }
                graphics.DrawImage(CurrentBitmap, ClientRectangle);

                //graphics.DrawImage(CurrentBitmap, 0, 0, Width, Height);

                graphics.Dispose();
            }
            finally { ReDrawing = false; }
        }

        public override void Dispose()
        {
            if (FrameSource != null)
                FrameSource.FrameReceived -= FrameSource_FrameReceived;

            if (CurrentBitmap != null)
            {
                CurrentBitmap.Dispose();
                CurrentBitmap = null;
            }
            base.Dispose();
        }
    }
}
