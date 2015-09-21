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
            buffer = this.FrameSource.VideoBuffer;

            this.PixFormat = PixelFormat.Format32bppArgb;
            this.CurrentBitmap = new Bitmap(Width, Height, PixFormat);

        }

        private PixelFormat PixFormat = PixelFormat.Format32bppArgb;

        private Bitmap CurrentBitmap = null;
        private MemoryBuffer buffer = null;
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
                    CopyToBitmap();

                    if (buffer.UpsideDown)
                        CurrentBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                    //Rectangle cropRect = new Rectangle(0,0, 320,240 );
                    //var bitmap = CopyToBitmap();
                    //CurrentBitmap = bitmap.Clone(cropRect, bitmap.PixelFormat);

                    Graphics g = Graphics.FromImage(CurrentBitmap);

                    //Rectangle cropRect = new Rectangle(0,0, 320,240 );
                    //Bitmap target = CurrentBitmap.Clone(cropRect, CurrentBitmap.PixelFormat);
                    //graphics.DrawImage(target, ClientRectangle);

                    g.DrawString(PerfCounter.GetReport(), PerfCounter.Styler.Font, PerfCounter.Styler.Brush, 0, 0);
                    g.Dispose();
                }
               
                graphics.DrawImage(CurrentBitmap,new Rectangle(0,0, 320,240 ), ClientRectangle, GraphicsUnit.Pixel);

                graphics.Dispose();
            }
            finally { ReDrawing = false; }
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        private void CopyToBitmap()
        {
            lock (buffer)
            {
                MappedData data = buffer.Data;
                BitmapData bits = CurrentBitmap.LockBits(new Rectangle(0, 0, CurrentBitmap.Width, CurrentBitmap.Height),
                    ImageLockMode.ReadOnly, PixFormat);
                NativeMethods.CopyMemory(bits.Scan0, data.Scan0, data.Size);
                CurrentBitmap.UnlockBits(bits);
            }
        }

        private Bitmap ___CopyToBitmap()
        {
            lock (buffer)
            {
                MappedData data = buffer.Data;
                Bitmap bitmap = new Bitmap(buffer.Width, buffer.Height);

                BitmapData bits = bitmap.LockBits(new Rectangle(0, 0, buffer.Width, buffer.Height),
                    ImageLockMode.ReadOnly, PixFormat);
                NativeMethods.CopyMemory(bits.Scan0, data.Scan0, data.Size);
                bitmap.UnlockBits(bits);

                return bitmap;
            }
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
