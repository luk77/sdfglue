//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model
{
    // Image sequence export (File -> Export image sequence...)
    public class ExportAnimationSettings
    {
        public  const string    DefaultFilePrefix       = "frame_";

        public  int             Fps                     = 60;
        public  int             NumOfFramesToPrerender  = 10;
        public  int             NumOfFramesToExport     = 120;
        public  string?         OutputFolder            = null;
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

        public void CopyFrom(ExportAnimationSettings src)
        {
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

            if (filePrefix != null)
                FilePrefix = filePrefix;

            return true;
        }
    }
}
