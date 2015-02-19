
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DirectShowLib;

namespace DxRender
{
    internal class CaptureSource : ISampleGrabberCB, IFrameSource, IDisposable
    {

        #region IFrameSource

        private MemoryBuffer buffer = null;
        MemoryBuffer IFrameSource.VideoBuffer
        {
            get { return buffer; }
        }

        void IFrameSource.Start()
        {
            if (!IsRunning)
            {
                int hr = MediaControll.Run();
                DsError.ThrowExceptionForHR(hr);

                IsRunning = true;
            }
        }

        void IFrameSource.Pause()
        {
            if (IsRunning)
            {
                int hr = MediaControll.Pause();
                DsError.ThrowExceptionForHR(hr);
                IsRunning = false;
            }
            else
            {
                int hr = MediaControll.Run();
                DsError.ThrowExceptionForHR(hr);
                IsRunning = true;
            }

        }
        void IFrameSource.Stop()
        {
            CloseInterfaces();
        }

        private event EventHandler<FrameReceivedEventArgs> FrameReceived;
        event EventHandler<FrameReceivedEventArgs> IFrameSource.FrameReceived
        {
            add { FrameReceived += value; }
            remove { FrameReceived -= value; }
        }

        private void OnFrameReceived(double Timestamp)
        {
            if (FrameReceived != null)
                FrameReceived(this, new FrameReceivedEventArgs { SampleTime = Timestamp });
        }

        #endregion

        private IFilterGraph2 FilterGraph = null;
        private IMediaControl MediaControll = null;
        private bool IsRunning = false;

        public CaptureSource(int DeviceNum, int FrameRate, int Width, int Height)
        {
            Setup(DeviceNum, FrameRate, Width, Height);
        }

        public void Dispose()
        {
            CloseInterfaces();
        }

        ~CaptureSource()
        {
            Dispose();
        }


        private void Setup(int DeviceNum, int FrameRate, int Width, int Height)
        {
            DsDevice[] capDevices;

            capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            if (DeviceNum + 1 > capDevices.Length)
            {
                throw new Exception("No video capture devices found at that index!");
            }

            try
            {
                SetupGraph(capDevices[DeviceNum], FrameRate, Width, Height);

                IsRunning = false;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void SetupGraph(DsDevice Device, int FrameRate, int Width, int Height)
        {
            int hr;

            ISampleGrabber SampleGrabber = null;
            IBaseFilter CaptureFilter = null;
            ICaptureGraphBuilder2 CaptureGraphBuilder = null;

            FilterGraph = (IFilterGraph2)new FilterGraph();
            MediaControll = FilterGraph as IMediaControl;
            try
            {
                CaptureGraphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();

                SampleGrabber = (ISampleGrabber)new SampleGrabber();

                hr = CaptureGraphBuilder.SetFiltergraph(FilterGraph);
                DsError.ThrowExceptionForHR(hr);

                hr = FilterGraph.AddSourceFilterForMoniker(Device.Mon, null, "Video input", out CaptureFilter);
                DsError.ThrowExceptionForHR(hr);

                IBaseFilter baseGrabFlt = (IBaseFilter)SampleGrabber;
                ConfigureSampleGrabber(SampleGrabber);

                hr = FilterGraph.AddFilter(baseGrabFlt, "Ds.NET Grabber");
                DsError.ThrowExceptionForHR(hr);

                if (FrameRate + Height + Width > 0)
                {
                    SetConfigParms(CaptureGraphBuilder, CaptureFilter, FrameRate, Width, Height);
                }

                hr = CaptureGraphBuilder.RenderStream(PinCategory.Capture, MediaType.Video, CaptureFilter, null, baseGrabFlt);
                DsError.ThrowExceptionForHR(hr);

                SaveSizeInfo(SampleGrabber);
            }
            finally
            {
                if (CaptureFilter != null)
                {
                    Marshal.ReleaseComObject(CaptureFilter);
                    CaptureFilter = null;
                }
                if (SampleGrabber != null)
                {
                    Marshal.ReleaseComObject(SampleGrabber);
                    SampleGrabber = null;
                }
                if (CaptureGraphBuilder != null)
                {
                    Marshal.ReleaseComObject(CaptureGraphBuilder);
                    CaptureGraphBuilder = null;
                }
            }
        }

        private void SaveSizeInfo(ISampleGrabber SampleGrabber)
        {
            int hr;

            // Get the media type from the SampleGrabber
            AMMediaType media = new AMMediaType();
            hr = SampleGrabber.GetConnectedMediaType(media);
            DsError.ThrowExceptionForHR(hr);

            if ((media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero))
            {
                throw new NotSupportedException("Unknown Grabber Media Format");
            }

            // Grab the size info
            VideoInfoHeader videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));

            buffer = new MemoryBuffer(videoInfoHeader.BmiHeader.Width, videoInfoHeader.BmiHeader.Height,
                videoInfoHeader.BmiHeader.BitCount, UpsideDown: true);

            DsUtils.FreeAMMediaType(media);
            media = null;
        }
        private void ConfigureSampleGrabber(ISampleGrabber SampleGrabber)
        {
            AMMediaType media;
            int hr;

            media = new AMMediaType();
            media.majorType = MediaType.Video;
            media.subType = MediaSubType.ARGB32;
            media.formatType = FormatType.VideoInfo;
            hr = SampleGrabber.SetMediaType(media);
            DsError.ThrowExceptionForHR(hr);

            DsUtils.FreeAMMediaType(media);
            media = null;

            hr = SampleGrabber.SetCallback(this, 1);
            DsError.ThrowExceptionForHR(hr);
        }

        private void SetConfigParms(ICaptureGraphBuilder2 CaptureGraphBuilder, IBaseFilter CaptureFilter, int FrameRate, int Width, int Height)
        {
            int hr;
            object o;
            AMMediaType media;

            // Find the stream config interface
            hr = CaptureGraphBuilder.FindInterface(PinCategory.Capture, MediaType.Video, CaptureFilter, typeof(IAMStreamConfig).GUID, out o);

            IAMStreamConfig videoStreamConfig = o as IAMStreamConfig;
            if (videoStreamConfig == null)
            {
                throw new Exception("Failed to get IAMStreamConfig");
            }

            // Get the existing format block
            hr = videoStreamConfig.GetFormat(out media);
            DsError.ThrowExceptionForHR(hr);

            // copy out the videoinfoheader
            VideoInfoHeader v = new VideoInfoHeader();
            Marshal.PtrToStructure(media.formatPtr, v);

            // if overriding the framerate, set the frame rate
            if (FrameRate > 0)
            {
                v.AvgTimePerFrame = 10000000 / FrameRate;
            }

            // if overriding the width, set the width
            if (Width > 0)
            {
                v.BmiHeader.Width = Width;
            }

            // if overriding the Height, set the Height
            if (Height > 0)
            {
                v.BmiHeader.Height = Height;
            }

            // Copy the media structure back
            Marshal.StructureToPtr(v, media.formatPtr, false);

            // Set the new format
            hr = videoStreamConfig.SetFormat(media);
            DsError.ThrowExceptionForHR(hr);

            DsUtils.FreeAMMediaType(media);
            media = null;
        }

        private void CloseInterfaces()
        {
            int hr;

            try
            {
                if (MediaControll != null)
                {
                    hr = MediaControll.Stop();
                    IsRunning = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            if (FilterGraph != null)
            {
                Marshal.ReleaseComObject(FilterGraph);
                FilterGraph = null;
            }
        }


        int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample)
        {
            Marshal.ReleaseComObject(pSample);
            return 0;
        }

        int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            if (BufferLen <= buffer.Size)
            {
                NativeMethods.CopyMemory(buffer.Data.Scan0, pBuffer, buffer.Size);
                OnFrameReceived(SampleTime);
            }
            else
            {
                throw new Exception("Buffer is wrong size");
            }

            return 0;
        }
    }
}
