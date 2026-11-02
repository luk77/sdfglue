//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Runtime.InteropServices;

namespace SingleDocAppVideoExportWin
{
    // The minimal Media Foundation interop used by Mp4Sink (the Sink Writer: mfplat.dll, mfreadwrite.dll).
    // COM interfaces declare all methods in the vtable order of the Windows SDK headers (mfobjects.h, mfreadwrite.h);
    // the inherited IMFAttributes methods of IMFMediaType / IMFSample are placeholders - the attributes are set
    // through a cast to IMFAttributes (QueryInterface). Failed calls throw COMException (HRESULT).
    internal static class MediaFoundation
    {
        public const int    MF_VERSION                          = (0x0002 << 16) | 0x0070;
        public const int    MFSTARTUP_LITE                      = 1;        // no sockets

        public const int    MFVideoInterlace_Progressive        = 2;
        public const int    eAVEncH264VProfile_High             = 100;
        public const int    MFVideoTransferMatrix_BT709         = 1;
        public const int    MFVideoPrimaries_BT709              = 2;
        public const int    MFVideoTransFunc_709                = 5;
        public const int    MFNominalRange_16_235               = 2;

        public static readonly Guid MF_MT_MAJOR_TYPE                        = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
        public static readonly Guid MF_MT_SUBTYPE                           = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        public static readonly Guid MF_MT_AVG_BITRATE                       = new Guid("20332624-fb0d-4d9e-bd0d-cbf6786c102e");
        public static readonly Guid MF_MT_INTERLACE_MODE                    = new Guid("e2724bb8-e676-4806-b4b2-a8d6efb44ccd");
        public static readonly Guid MF_MT_FRAME_SIZE                        = new Guid("1652c33d-d6b2-4012-b834-72030849a37d");
        public static readonly Guid MF_MT_FRAME_RATE                        = new Guid("c459a2e8-3d2c-4e44-b132-fee5156c7bb0");
        public static readonly Guid MF_MT_PIXEL_ASPECT_RATIO                = new Guid("c6376a1e-8d0a-4027-be45-6d9a0ad39bb6");
        public static readonly Guid MF_MT_DEFAULT_STRIDE                    = new Guid("644b4e48-1e02-4516-b0eb-c01ca9d49ac6");
        public static readonly Guid MF_MT_MPEG2_PROFILE                     = new Guid("ad76a80b-2d5c-4e0b-b375-64e520137036");
        public static readonly Guid MF_MT_YUV_MATRIX                        = new Guid("3e23d450-2c75-4d25-a00e-b91670d12327");
        public static readonly Guid MF_MT_VIDEO_PRIMARIES                   = new Guid("dbfbe4d7-0740-4ee0-8192-850ab0e21935");
        public static readonly Guid MF_MT_TRANSFER_FUNCTION                 = new Guid("5fb0fce9-be5c-4935-a811-ec838f8eed93");
        public static readonly Guid MF_MT_VIDEO_NOMINAL_RANGE               = new Guid("c21b8ee5-b956-4071-8daf-325edf5cab11");
        public static readonly Guid MF_MT_ALL_SAMPLES_INDEPENDENT           = new Guid("c9173739-5e56-461c-b713-46fb995cb95f");
        public static readonly Guid MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS = new Guid("a634a91c-822b-41b9-a494-4de4643612b0");
        public static readonly Guid MF_TRANSCODE_CONTAINERTYPE              = new Guid("150ff23f-4abc-478b-ac4f-e1916fba1cca");
        public static readonly Guid MFTranscodeContainerType_MPEG4          = new Guid("dc6cd05d-b9d0-40ef-bd35-fa622c1ab28a");

        public static readonly Guid MFMediaType_Video                       = new Guid("73646976-0000-0010-8000-00aa00389b71");
        public static readonly Guid MFVideoFormat_H264                      = new Guid("34363248-0000-0010-8000-00aa00389b71");
        public static readonly Guid MFVideoFormat_RGB32                     = new Guid("00000016-0000-0010-8000-00aa00389b71");

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFStartup(int version, int flags);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFShutdown();

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateAttributes(out IMFAttributes attributes, int initialSize);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateMediaType(out IMFMediaType mediaType);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateSample(out IMFSample sample);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateMemoryBuffer(int maxLength, out IMFMediaBuffer buffer);

        [DllImport("mfreadwrite.dll", ExactSpelling = true)]
        public static extern int MFCreateSinkWriterFromURL([MarshalAs(UnmanagedType.LPWStr)] string url, IntPtr byteStream,
                                                           IMFAttributes? attributes, out IMFSinkWriter sinkWriter);

        public static void Check(int hr, string what)
        {
            if (hr < 0)
                throw new COMException(String.Format("{0} failed (HRESULT 0x{1:X8}).", what, hr), hr);
        }

        public static ulong Pack(int high, int low)
        {
            return ((ulong)(uint)high << 32) | (uint)low;
        }

        public static void Release(object? comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
                Marshal.ReleaseComObject(comObject);
        }
    }

    [ComImport, Guid("2cd2d921-c447-44a7-a13c-4adabfc247e3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFAttributes
    {
        void GetItem            ();
        void GetItemType        ();
        void CompareItem        ();
        void Compare            ();
        void GetUINT32          ();
        void GetUINT64          ();
        void GetDouble          ();
        void GetGUID            ();
        void GetStringLength    ();
        void GetString          ();
        void GetAllocatedString ();
        void GetBlobSize        ();
        void GetBlob            ();
        void GetAllocatedBlob   ();
        void GetUnknown         ();
        void SetItem            ();
        void DeleteItem         ();
        void DeleteAllItems     ();
        void SetUINT32          ([In, MarshalAs(UnmanagedType.LPStruct)] Guid key, int value);
        void SetUINT64          ([In, MarshalAs(UnmanagedType.LPStruct)] Guid key, ulong value);
        void SetDouble          ();
        void SetGUID            ([In, MarshalAs(UnmanagedType.LPStruct)] Guid key, [In, MarshalAs(UnmanagedType.LPStruct)] Guid value);
        void SetString          ();
        void SetBlob            ();
        void SetUnknown         ();
        void LockStore          ();
        void UnlockStore        ();
        void GetCount           ();
        void GetItemByIndex     ();
        void CopyAllItems       ();
    }

    // IMFAttributes (30 placeholders) + IMFMediaType
    [ComImport, Guid("44ae0fa8-ea31-4109-8d2e-4cae4997c555"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaType
    {
        void A01(); void A02(); void A03(); void A04(); void A05(); void A06(); void A07(); void A08(); void A09(); void A10();
        void A11(); void A12(); void A13(); void A14(); void A15(); void A16(); void A17(); void A18(); void A19(); void A20();
        void A21(); void A22(); void A23(); void A24(); void A25(); void A26(); void A27(); void A28(); void A29(); void A30();

        void GetMajorType       ();
        void IsCompressedFormat ();
        void IsEqual            ();
        void GetRepresentation  ();
        void FreeRepresentation ();
    }

    // IMFAttributes (30 placeholders) + IMFSample
    [ComImport, Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFSample
    {
        void A01(); void A02(); void A03(); void A04(); void A05(); void A06(); void A07(); void A08(); void A09(); void A10();
        void A11(); void A12(); void A13(); void A14(); void A15(); void A16(); void A17(); void A18(); void A19(); void A20();
        void A21(); void A22(); void A23(); void A24(); void A25(); void A26(); void A27(); void A28(); void A29(); void A30();

        void GetSampleFlags             ();
        void SetSampleFlags             ();
        void GetSampleTime              ();
        void SetSampleTime              (long sampleTime);         // 100 ns units
        void GetSampleDuration          ();
        void SetSampleDuration          (long sampleDuration);
        void GetBufferCount             ();
        void GetBufferByIndex           ();
        void ConvertToContiguousBuffer  ();
        void AddBuffer                  (IMFMediaBuffer buffer);
        void RemoveBufferByIndex        ();
        void RemoveAllBuffers           ();
        void GetTotalLength             ();
        void CopyToBuffer               ();
    }

    [ComImport, Guid("045fa593-8799-42b8-bc8d-8968c6453507"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaBuffer
    {
        void Lock               (out IntPtr buffer, out int maxLength, out int currentLength);
        void Unlock             ();
        void GetCurrentLength   ();
        void SetCurrentLength   (int currentLength);
        void GetMaxLength       ();
    }

    [ComImport, Guid("3137f1cd-fe5e-4805-a5d8-fb477448cb3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFSinkWriter
    {
        void AddStream              (IMFMediaType targetMediaType, out int streamIndex);
        void SetInputMediaType      (int streamIndex, IMFMediaType inputMediaType, IMFAttributes? encodingParameters);
        void BeginWriting           ();
        void WriteSample            (int streamIndex, IMFSample sample);
        void SendStreamTick         ();
        void PlaceMarker            ();
        void NotifyEndOfSegment     ();
        void Flush                  ();
        void Finalize_              ();     // IMFSinkWriter::Finalize (the name is reserved in C#)
        void GetServiceForStream    ();
        void GetStatistics          ();
    }
}
