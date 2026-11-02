//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model
{
    public enum ExportAnimationFormat
    {
        PngSequence,
        Mp4,
        Gif,
    }

    // MP4 bitrate: presets (bits per pixel of a frame) or Mp4BitrateMbps
    public enum ExportMp4Quality
    {
        Low,
        Medium,
        High,
        Custom,
    }

    public enum ExportGifDither
    {
        None,
        Ordered,
        FloydSteinberg,
    }

    // Animation export (File -> Export animation...): an image sequence (PNG), an MP4 video or an animated GIF
    public class ExportAnimationSettings
    {
        public  const string    DefaultFilePrefix       = "frame_";
        // browsers play GIF frames shorter than 2/100 s slower
        public  const int       MaxGifFps               = 50;
        public  static readonly int[]   GifScales       = { 1, 2, 4 };

        public  ExportAnimationFormat   Format          = ExportAnimationFormat.PngSequence;
        public  string?         OutputFile              = null;     // MP4 / GIF
        public  ExportMp4Quality Mp4Quality             = ExportMp4Quality.High;
        public  double          Mp4BitrateMbps          = 20.0;     // Mp4Quality.Custom
        public  ExportGifDither GifDither               = ExportGifDither.Ordered;
        public  bool            GifLoop                 = true;
        public  bool            GifOptimizeDelta        = true;     // unchanged pixels transparent, frames cropped
        public  int             GifScale                = 1;        // 1, 2, 4: the GIF is smaller than the rendered frames

        public  int             Fps                     = 60;
        public  int             NumOfFramesToPrerender  = 10;
        public  int             NumOfFramesToExport     = 120;
        public  string?         OutputFolder            = null;     // PNG sequence
        public  string          FilePrefix              = DefaultFilePrefix;
        public  int             ResolutionIndex         = -1;       // index in UserSettingsSdfGlue.Resolutions, -1: preview resolution
        public  bool            UseStartTime            = false;    // false: export from the current time of the session
        public  double          StartTime               = 0.0;      // time of the first saved frame (UseStartTime)

        // Duration of the saved frames in seconds
        public double GetDuration()
        {
            return (Fps > 0) ? (double)NumOfFramesToExport / Fps : 0.0;
        }

        public string GetFileName(int frameIndex)
        {
            return String.Format("{0}{1}.png", FilePrefix, frameIndex.ToString("D8"));
        }

        public bool IsVideoFile()
        {
            return Format != ExportAnimationFormat.PngSequence;
        }

        public static string GetFileExtension(ExportAnimationFormat format)
        {
            switch (format)
            {
                case ExportAnimationFormat.Mp4: return ".mp4";
                case ExportAnimationFormat.Gif: return ".gif";
                default:                        return ".png";
            }
        }

        public void CopyFrom(ExportAnimationSettings src)
        {
            Format                  = src.Format;
            OutputFile              = src.OutputFile;
            Mp4Quality              = src.Mp4Quality;
            Mp4BitrateMbps          = src.Mp4BitrateMbps;
            GifDither               = src.GifDither;
            GifLoop                 = src.GifLoop;
            GifOptimizeDelta        = src.GifOptimizeDelta;
            GifScale                = src.GifScale;
            Fps                     = src.Fps;
            NumOfFramesToPrerender  = src.NumOfFramesToPrerender;
            NumOfFramesToExport     = src.NumOfFramesToExport;
            OutputFolder            = src.OutputFolder;
            FilePrefix              = src.FilePrefix;
            ResolutionIndex         = src.ResolutionIndex;
            UseStartTime            = src.UseStartTime;
            StartTime               = src.StartTime;
        }

        public void Serialize(XmlDocument xmlDoc, XmlNode parentNode)
        {
            XmlUtils.AddNodeInt     (xmlDoc, parentNode, "Fps"                      , Fps                       );
            XmlUtils.AddNodeInt     (xmlDoc, parentNode, "NumOfFramesToPrerender"   , NumOfFramesToPrerender    );
            XmlUtils.AddNodeInt     (xmlDoc, parentNode, "NumOfFramesToExport"      , NumOfFramesToExport       );
            XmlUtils.AddNodeString  (xmlDoc, parentNode, "OutputFolder"             , OutputFolder              );
            XmlUtils.AddNodeString  (xmlDoc, parentNode, "FilePrefix"               , FilePrefix                );
            XmlUtils.AddNodeInt     (xmlDoc, parentNode, "ResolutionIndex"          , ResolutionIndex           );
            XmlUtils.AddNodeBool    (xmlDoc, parentNode, "UseStartTime"             , UseStartTime              );
            XmlUtils.AddNodeDouble  (xmlDoc, parentNode, "StartTime"                , StartTime                 );
            XmlUtils.AddNodeEnum    (xmlDoc, parentNode, "Format"                   , Format                    );
            XmlUtils.AddNodeString  (xmlDoc, parentNode, "OutputFile"               , OutputFile                );
            XmlUtils.AddNodeEnum    (xmlDoc, parentNode, "Mp4Quality"               , Mp4Quality                );
            XmlUtils.AddNodeDouble  (xmlDoc, parentNode, "Mp4BitrateMbps"           , Mp4BitrateMbps            );
            XmlUtils.AddNodeEnum    (xmlDoc, parentNode, "GifDither"                , GifDither                 );
            XmlUtils.AddNodeBool    (xmlDoc, parentNode, "GifLoop"                  , GifLoop                   );
            XmlUtils.AddNodeBool    (xmlDoc, parentNode, "GifOptimizeDelta"         , GifOptimizeDelta          );
            XmlUtils.AddNodeInt     (xmlDoc, parentNode, "GifScale"                 , GifScale                  );
        }

        public bool Deserialize(XmlNode parentNode)
        {
            if (parentNode == null)
                return false;

            string? filePrefix = null;

            XmlUtils.DeserializeInt     (parentNode, "Fps"                      , ref Fps                       );
            XmlUtils.DeserializeInt     (parentNode, "NumOfFramesToPrerender"   , ref NumOfFramesToPrerender    );
            XmlUtils.DeserializeInt     (parentNode, "NumOfFramesToExport"      , ref NumOfFramesToExport       );
            XmlUtils.DeserializeString  (parentNode, "OutputFolder"             , ref OutputFolder              );
            XmlUtils.DeserializeString  (parentNode, "FilePrefix"               , ref filePrefix                );
            XmlUtils.DeserializeInt     (parentNode, "ResolutionIndex"          , ref ResolutionIndex           );
            XmlUtils.DeserializeBool    (parentNode, "UseStartTime"             , ref UseStartTime              );
            XmlUtils.DeserializeDouble  (parentNode, "StartTime"                , ref StartTime                 );
            // missing in older settings: the defaults (PNG sequence)
            XmlUtils.DeserializeEnum    (parentNode, "Format"                   , ref Format                    );
            XmlUtils.DeserializeString  (parentNode, "OutputFile"               , ref OutputFile                );
            XmlUtils.DeserializeEnum    (parentNode, "Mp4Quality"               , ref Mp4Quality                );
            XmlUtils.DeserializeDouble  (parentNode, "Mp4BitrateMbps"           , ref Mp4BitrateMbps            );
            XmlUtils.DeserializeEnum    (parentNode, "GifDither"                , ref GifDither                 );
            XmlUtils.DeserializeBool    (parentNode, "GifLoop"                  , ref GifLoop                   );
            XmlUtils.DeserializeBool    (parentNode, "GifOptimizeDelta"         , ref GifOptimizeDelta          );
            XmlUtils.DeserializeInt     (parentNode, "GifScale"                 , ref GifScale                  );

            if (filePrefix != null)
                FilePrefix = filePrefix;

            return true;
        }
    }
}
