using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO.MemoryMappedFiles;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace DxRender
{
    public class MappedData
    {
        public MappedData(IntPtr scan0, int size)
        {
            Scan0 = scan0;
            Size = size;
        }
        public readonly IntPtr Scan0;
        public readonly int Size;
    }

    public class VideoBufferAllocator
    {
        public static long GetBufferSize(int Width, int Height, int bitsPerPixel)
        {
            long Size = -1;
            int Stride = 4 * ((Width * ((bitsPerPixel + 7) / 8) + 3) / 4);

            Size = Height * Stride;

            return Size;
        }
    }

    public class MemoryBuffer : IDisposable
    {
        public MemoryBuffer(int Width, int Height, int BitsPerPixel, bool UpsideDown = false)
        {
            this.Width = Width;
            this.Height = Height;
            this.BitsPerPixel = BitsPerPixel;

            this.Stride = 4 * ((Width * ((BitsPerPixel + 7) / 8) + 3) / 4);

            this.UpsideDown = UpsideDown;
        }

        private object locker = new object();

        private MemoryMappedFile MappedFile = null;
        private MemoryMappedViewStream MappedStream = null;

        private MappedData data = null;

        public MappedData Data
        {
            get
            {
                lock (locker)
                {
                    if (data == null)
                    {
                        MappedFile = MemoryMappedFile.CreateOrOpen(Guid.NewGuid().ToString(), Size);
                        MappedStream = MappedFile.CreateViewStream();
                        IntPtr Ptr = MappedStream.SafeMemoryMappedViewHandle.DangerousGetHandle();
                        data = new MappedData(Ptr, Size);
                    }

                    return data;
                }
            }
        }

        /// <summary>
        /// Направление заполнения битмапа (сверху-вниз или снизу-вверх)
        /// </summary>
        public bool UpsideDown { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Stride { get; private set; }
        public int BitsPerPixel { get; private set; }

        public int Size { get { return Height * Stride; } }


        public void Dispose()
        {
            if (MappedFile != null)
            {
                MappedFile.Dispose();
                MappedFile = null;
            }
            if (MappedStream != null)
            {
                MappedStream.Dispose();
                MappedStream = null;
            }

        }
    }

    public class MappedDataEx
    {
        public MappedDataEx(IntPtr handle)
        {
            signalPtr = handle;
            scan0Ptr = handle + VideoBufferEx.EXTRA_DATA_SIZE;
        }
        public readonly IntPtr signalPtr;
        public readonly IntPtr scan0Ptr;
        public byte signal
        {
            get
            {
                return Marshal.ReadByte(signalPtr);
            }
            set
            {
                Marshal.WriteByte(signalPtr, value);
            }
        }
    }
    [Serializable]
    public class VideoBufferEx : IDisposable, IDeserializationCallback
    {
        public const int BUFFER_PADDING_SIZE = 16;
        public const int EXTRA_DATA_SIZE = 16;
        public VideoBufferEx(int width, int height)
        {
            this.memoryMappedFileName = Guid.NewGuid().ToString();
            this.Width = width;
            this.Height = height;
            this.pixelFormat = PixFrmt.rgb24;
            this.Stride = ((width * pixelFormat.bitsPerPixel + 7) / 8 + 15) & ~15;
        }
        public VideoBufferEx(int width, int height, PixFrmt pixFrmt)
        {
            this.memoryMappedFileName = Guid.NewGuid().ToString();
            this.Width = width;
            this.Height = height;
            this.pixelFormat = pixFrmt;
            this.Stride = ((width * pixelFormat.bitsPerPixel + 7) / 8 + 15) & ~15;
        }
        public VideoBufferEx(int width, int height, PixFrmt pixFrmt, int stride)
        {
            this.memoryMappedFileName = Guid.NewGuid().ToString();
            this.Width = width;
            this.Height = height;
            this.pixelFormat = pixFrmt;
            this.Stride = stride;
        }
        private string memoryMappedFileName;
        [NonSerialized]
        private object sync = new object();
        [NonSerialized]
        private IDisposable<MappedDataEx> mappedData = null;

        [NonSerialized]
        private MappedDataEx data = null;

        [NonSerialized]
        private int refCnt = 0;

        public readonly int Height;
        public readonly PixFrmt pixelFormat;
        public int size { get { return Height * Stride + BUFFER_PADDING_SIZE; } }
        public readonly int Stride;
        public readonly int Width;

        public MappedDataEx LockMappedData()
        {
            lock (sync)
            {
                if (data == null)
                {
                    MemoryMappedFile file = null;
                    try
                    {
                        file = MemoryMappedFile.CreateOrOpen(memoryMappedFileName, size + EXTRA_DATA_SIZE);
                        MemoryMappedViewStream stream = null;
                        try
                        {
                            stream = file.CreateViewStream();
                            var handle = stream.SafeMemoryMappedViewHandle;
                            var ptr = handle.DangerousGetHandle();
                            data = new MappedDataEx(ptr);
                        }
                        catch (Exception ex)
                        {
                            Debug.Fail(ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Fail(ex.Message);
                    }

                }

                return data;
            }

        }

        public IDisposable<MappedDataEx> Lock()
        {
            lock (sync)
            {
                if (mappedData == null)
                {
                    var file = MemoryMappedFile.CreateOrOpen(memoryMappedFileName, size + EXTRA_DATA_SIZE);
                    try
                    {
                        var stream = file.CreateViewStream();
                        try
                        {
                            var handle = stream.SafeMemoryMappedViewHandle;
                            mappedData = DisposableExt.Create(
                                new MappedDataEx(handle.DangerousGetHandle()),
                                () =>
                                {
                                    stream.Dispose();
                                    file.Dispose();
                                }
                            );
                        }
                        catch (Exception err)
                        {
                            //dbg.Error(err);
                            stream.Dispose();
                        }
                    }
                    catch (Exception err)
                    {
                        //dbg.Error(err);
                        file.Dispose();
                    }
                }
                ++refCnt;
                return DisposableExt.Create<MappedDataEx>(
                    mappedData.value,
                    () =>
                    {
                        lock (sync)
                        {
                            --refCnt;
                            if (refCnt == 0)
                            {
                                mappedData.Dispose();
                                mappedData = null;
                            }
                        }
                    }
                );
            }
        }

        public void Dispose()
        {
        }
        void IDeserializationCallback.OnDeserialization(object sender)
        {
            sync = new object();
            refCnt = 0;
            mappedData = null;
        }
    }
    public interface IDisposable<T> : IDisposable
    {
        T value { get; }
    }

    public class DisposableExt
    {
        private class AnonymousDisposableWithFinalizer<T> : IDisposable<T>
        {
            private Action<bool> disposeAction = null;
            public T value { get; private set; }
            public AnonymousDisposableWithFinalizer(T value, Action<bool> disposeAction)
            {
                this.value = value;
                this.disposeAction = disposeAction;
            }
            public virtual void Dispose(bool disposing)
            {
                var act = Interlocked.Exchange(ref disposeAction, null);
                if (act != null)
                {
                    value = default(T);
                    act(disposing);
                }
            }
            public void Dispose()
            {
                Dispose(true);
            }
            ~AnonymousDisposableWithFinalizer()
            {
                Dispose(false);
            }
        }

        private class AnonymousDisposable<T> : IDisposable<T>
        {
            private Action disposeAction = null;
            public T value { get; private set; }
            public AnonymousDisposable(T value, Action disposeAction)
            {
                this.value = value;
                this.disposeAction = disposeAction;
            }
            public void Dispose()
            {
                var act = Interlocked.Exchange(ref disposeAction, null);
                if (act != null)
                {
                    value = default(T);
                    act();
                }
            }
        }

        public static IDisposable<T> CreateWithFinalizer<T>(T value, Action<bool> disposeAction)
        {
            return new AnonymousDisposableWithFinalizer<T>(value, disposeAction);
        }
        public static IDisposable<T> Create<T>(T value, Action disposeAction)
        {
            return new AnonymousDisposable<T>(value, disposeAction);
        }
    }

    [Serializable]
    public class PixFrmt : ISerializable
    {
        private class PixFrmtImpl
        {
            private static List<PixFrmtImpl> impls = new List<PixFrmtImpl>(16);
            private string formatStr;
            private int m_bitsPerPixel;
            private int m_id;
            private PixFrmtImpl(string formatStr, int bitsPerPixel)
            {
                this.m_bitsPerPixel = bitsPerPixel;
                this.formatStr = formatStr;
            }
            public static PixFrmtImpl Create(string formatStr, int bitsPerPixel)
            {
                //log.WriteInfo("PixFrmtImpl::Create(...)");
                var impl = new PixFrmtImpl(formatStr, bitsPerPixel);
                lock (impls)
                {
                    impl.m_id = impls.Count;
                    impls.Add(impl);
                }
                return impl;
            }
            public static PixFrmtImpl GetById(int id)
            {
                //log.WriteInfo(String.Format("PixFrmtImpl::GetById({0})", id));
                //log.WriteInfo(String.Format("impls length is {0}", impls.Count));
                lock (impls)
                {
                    return impls[id];
                }
            }
            public int bitsPerPixel
            {
                get { return m_bitsPerPixel; }
            }
            public int id
            {
                get { return m_id; }
            }
            public override string ToString()
            {
                return formatStr;
            }
        }
        private PixFrmtImpl m_pixFmtImpl;

        private PixFrmt(PixFrmtImpl pixFmtImpl)
        {
            this.m_pixFmtImpl = pixFmtImpl;
        }
        public int bitsPerPixel
        {
            get { return m_pixFmtImpl.bitsPerPixel; }
        }
        public override string ToString()
        {
            return m_pixFmtImpl.ToString();
        }
        // Explicit static constructor to tell C# compiler 
        // not to mark type as beforefieldinit 
        static PixFrmt()
        {
        }
        public static readonly PixFrmt rgb24 = new PixFrmt(PixFrmtImpl.Create("rgb24", 24));
        public static readonly PixFrmt bgr24 = new PixFrmt(PixFrmtImpl.Create("bgr24", 24));
        public static readonly PixFrmt argb32 = new PixFrmt(PixFrmtImpl.Create("argb32", 32));
        public static readonly PixFrmt bgra32 = new PixFrmt(PixFrmtImpl.Create("bgra32", 32));
        public static bool operator ==(PixFrmt left, PixFrmt right)
        {
            return left.m_pixFmtImpl == right.m_pixFmtImpl;
        }
        public static bool operator !=(PixFrmt left, PixFrmt right)
        {
            return left.m_pixFmtImpl != right.m_pixFmtImpl;
        }
        public override bool Equals(object obj)
        {
            return obj is PixFrmt && m_pixFmtImpl == ((PixFrmt)obj).m_pixFmtImpl;
        }
        public override int GetHashCode()
        {
            return m_pixFmtImpl.id;
        }

        private static string impl_key { get { return "id"; } }
        //deserialization constructor
        private PixFrmt(SerializationInfo info, StreamingContext context)
        {
            //log.WriteInfo("PixFrmt::PixFrmt(SerializationInfo info, StreamingContext context)");
            if (info == null)
            {
                //log.WriteInfo("info is null");
                throw new System.ArgumentNullException("info");
            }
            var id = info.GetInt32(impl_key);
            this.m_pixFmtImpl = PixFrmtImpl.GetById(id);
            //log.WriteInfo("-------------------------------------------");
        }

        //ISerializable implementation
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(impl_key, m_pixFmtImpl.id);
        }
    }
}
