
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
        private string CaptureInfo = "";
        string IFrameSource.Info { get { return CaptureInfo; } }

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
            int HResult = 0;

            ISampleGrabber SampleGrabber = null;
            IBaseFilter CaptureFilter = null;
            ICaptureGraphBuilder2 CaptureGraphBuilder = null;

            FilterGraph = (IFilterGraph2)new FilterGraph();
            MediaControll = FilterGraph as IMediaControl;
            try
            {
                CaptureGraphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();

                SampleGrabber = (ISampleGrabber)new SampleGrabber();

                HResult = CaptureGraphBuilder.SetFiltergraph(FilterGraph);
                DsError.ThrowExceptionForHR(HResult);

                HResult = FilterGraph.AddSourceFilterForMoniker(Device.Mon, null, "Video input", out CaptureFilter);
                DsError.ThrowExceptionForHR(HResult);

                IBaseFilter baseGrabFlt = (IBaseFilter)SampleGrabber;
                ConfigureSampleGrabber(SampleGrabber);

                HResult = FilterGraph.AddFilter(baseGrabFlt, "Ds.NET Grabber");
                DsError.ThrowExceptionForHR(HResult);

                if (FrameRate + Height + Width > 0)
                {
                    SetConfigParms(CaptureGraphBuilder, CaptureFilter, FrameRate, Width, Height);
                }

                HResult = CaptureGraphBuilder.RenderStream(PinCategory.Capture, MediaType.Video, CaptureFilter, null, baseGrabFlt);
                DsError.ThrowExceptionForHR(HResult);

                CaptureInfo = string.Format("{0} ", Device.Name);
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
            int HResult = 0;
            AMMediaType MType = new AMMediaType();
            HResult = SampleGrabber.GetConnectedMediaType(MType);
            DsError.ThrowExceptionForHR(HResult);

            if ((MType.formatType != FormatType.VideoInfo) || (MType.formatPtr == IntPtr.Zero))
                throw new NotSupportedException("Unknown Grabber Media Format");

            VideoInfoHeader videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(MType.formatPtr, typeof(VideoInfoHeader));

            buffer = new MemoryBuffer(videoInfoHeader.BmiHeader.Width, videoInfoHeader.BmiHeader.Height,
                videoInfoHeader.BmiHeader.BitCount, UpsideDown: true);

            CaptureInfo += string.Format("{0}x{1} {2}fps {3}bit",
                videoInfoHeader.BmiHeader.Width, videoInfoHeader.BmiHeader.Height,
                10000000 / videoInfoHeader.AvgTimePerFrame, videoInfoHeader.BmiHeader.BitCount);

            DsUtils.FreeAMMediaType(MType);
            MType = null;
        }

        private void ConfigureSampleGrabber(ISampleGrabber SampleGrabber)
        {
            int HResult = 0;

            AMMediaType MType = new AMMediaType();
            MType.majorType = MediaType.Video;
            MType.subType = MediaSubType.ARGB32;
            MType.formatType = FormatType.VideoInfo;
            HResult = SampleGrabber.SetMediaType(MType);
            DsError.ThrowExceptionForHR(HResult);

            DsUtils.FreeAMMediaType(MType);
            MType = null;

            HResult = SampleGrabber.SetBufferSamples(false);
            DsError.ThrowExceptionForHR(HResult);

            // SampleCB - 0
            // BufferCB - 1
            HResult = SampleGrabber.SetCallback(this, 1);
            DsError.ThrowExceptionForHR(HResult);
        }

        private void SetConfigParms(ICaptureGraphBuilder2 CaptureGraphBuilder, IBaseFilter CaptureFilter, int FrameRate, int Width, int Height)
        {
            int HResult = 0;
            object ObjectPointer = null;
            HResult = CaptureGraphBuilder.FindInterface(PinCategory.Capture, MediaType.Video, CaptureFilter, typeof(IAMStreamConfig).GUID, out ObjectPointer);

            IAMStreamConfig VideoStreamConfig = ObjectPointer as IAMStreamConfig;
            if (VideoStreamConfig == null)
                throw new Exception("Failed to get IAMStreamConfig");

            int iCount = 0, iSize = 0;
            VideoStreamConfig.GetNumberOfCapabilities(out iCount, out iSize);

            IntPtr TaskMemPointer = Marshal.AllocCoTaskMem(iSize);

            VideoInfoHeader VideoInfoHeader = new VideoInfoHeader();

            AMMediaType MType = null;
            for (int iFormat = 0; iFormat < iCount; iFormat++)
            {
                VideoStreamConfig.GetStreamCaps(iFormat, out MType, TaskMemPointer);

                VideoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(MType.formatPtr, typeof(VideoInfoHeader));

                Debug.WriteLine("{0}x{1} {2}fps {3}bit",
                    VideoInfoHeader.BmiHeader.Width, VideoInfoHeader.BmiHeader.Height,
                    10000000 / VideoInfoHeader.AvgTimePerFrame, VideoInfoHeader.BmiHeader.BitCount);

                if (VideoInfoHeader.BmiHeader.Width == Width && VideoInfoHeader.BmiHeader.Height == Height)
                {
                    Marshal.StructureToPtr(VideoInfoHeader, MType.formatPtr, true);
                    HResult = VideoStreamConfig.SetFormat(MType);
                    break;
                }

            }

            DsUtils.FreeAMMediaType(MType);
            MType = null;
        }

        private void CloseInterfaces()
        {
            int HResult = 0;

            try
            {
                if (MediaControll != null)
                {
                    HResult = MediaControll.Stop();
                    IsRunning = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            if (MediaControll != null)
            {
                Marshal.ReleaseComObject(MediaControll);
                MediaControll = null;
            }

            if (FilterGraph != null)
            {
                Marshal.ReleaseComObject(FilterGraph);
                FilterGraph = null;
            }
        }


        int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample MediaSample)
        {
            Marshal.ReleaseComObject(MediaSample);
            return 0;
        }

       // Stopwatch sw = new Stopwatch();
        int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr Buffer, int BufferLen)
        {

            if (BufferLen <= buffer.Size)
            {
                //sw.Restart();

                NativeMethods.CopyMemory(buffer.Data.Scan0, Buffer, buffer.Size);
                //Debug.WriteLine(sw.ElapsedMilliseconds);
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
