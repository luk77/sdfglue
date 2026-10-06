//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Settings;
using SingleDocAppCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model
{
    // SdfGlue user settings, persisted in UserSettings.xml (see UserSettingsBase).
    // The instance is owned by the application and shared by consecutive DataModels.
    public class UserSettingsSdfGlue : UserSettingsBase
    {
        public  bool        EnableRendering                     = true;         // diagnostics only, not saved
        public  bool        UseNamesAsIds                       = false;        // when enabled, object names are used in the shader instead of numeric ids. Names must be unique.
        public  float       MouseCameraPanSpeed                 = 1.0f;
        public  float       MouseCameraRotationSpeedPitch       = 1.0f;
        public  float       MouseCameraRotationSpeedYaw         = 1.0f;
        public  bool        MouseCameraRotationInvPitch         = false;
        public  bool        MouseCameraRotationInvYaw           = false;
        public  bool        MouseWheelInvert                    = false;
        public  float       MouseWheelSpeed                     = 1.0f;
        public  bool        UseShiftKeyToZoom                   = false;
        public  bool        UseAltRmbForCameraRotation          = false;

        public  float       CameraPosDamping                    = 7.0f;
        public  float       CameraRotDamping                    = 10.0f;
        public  float       CameraDistDamping                   = 7.0f;

        public bool         MonitorFileSystemChanges            = true;

        // UI text scale factor, update frequency limit: see UserSettingsBase

        public bool                     ConstAspectRatioPreview = true;
        public List<IntCoords>          Resolutions             = new List<IntCoords>();
        public string[]                 ResolutionsAsStrings    = new string[1];
        public int                      CurrentResolutionIndex  = 0;
        // not that simple - path tracing needs more frames to be accumulated
        // rather than just changing the resolution for a single frame
        //public int                      ExportImageResolutionIndex = 0;

        public bool                     PreviewLastSelectedPass = true;

        public ExportAnimationSettings  ExportAnimSettings  = new ExportAnimationSettings();

        public override void InitDefault()
        {
            base.InitDefault();

            //---------------------------------------------------------------
            // Resolutions
            //---------------------------------------------------------------
            // reference: https://en.wikipedia.org/wiki/List_of_common_resolutions
            Resolutions = new List<IntCoords>();
            Resolutions.Add(new IntCoords(160, 120));
            Resolutions.Add(new IntCoords(320, 192));   // Atari 8-bit family
            Resolutions.Add(new IntCoords(320, 240));
            Resolutions.Add(new IntCoords(640, 256));   //  Amiga OCS PAL Hires
            Resolutions.Add(new IntCoords(640, 480));
            Resolutions.Add(new IntCoords(800, 600));
            Resolutions.Add(new IntCoords(1024, 768));
            // 16:9
            Resolutions.Add(new IntCoords(16, 9));
            Resolutions.Add(new IntCoords(160, 90));
            Resolutions.Add(new IntCoords(1920 / 8, 1080 / 8));
            Resolutions.Add(new IntCoords(320, 180));
            Resolutions.Add(new IntCoords(1920 / 4, 1080 / 4));
            Resolutions.Add(new IntCoords(1920 / 2, 1080 / 2));
            Resolutions.Add(new IntCoords(1920, 1080));         // Full HD
            // square
            Resolutions.Add(new IntCoords(128, 128));
            Resolutions.Add(new IntCoords(512, 512));
            Resolutions.Add(new IntCoords(1024, 1024));
            Resolutions.Add(new IntCoords(2048, 2048));
            //Resolutions.Add(new IntCoords(1920 * 2, 1080 * 2)); (same as below)
            Resolutions.Add(new IntCoords(3840 , 2160));    // 4k (TV)  (4K UHD)
            Resolutions.Add(new IntCoords(4096 , 2160));    // 4k (movie) (DCI 4K)

            Resolutions.Add(new IntCoords(800 , 100));
            Resolutions.Add(new IntCoords(1600, 200));
            Resolutions.Add(new IntCoords(2400, 300));

            ResolutionsAsStrings = new string[Resolutions.Count];
            for (int i = 0; i < Resolutions.Count; i++)
            {
                IntCoords c = Resolutions[i];
                ResolutionsAsStrings[i] = String.Format("{0}x{1}", c.X, c.Y);
            }
            CurrentResolutionIndex = 12;
            //ExportImageResolutionIndex = 7; //1920x1080
        }

        public IntCoords GetPreviewResolution()
        {
            return Resolutions[CurrentResolutionIndex];
        }

        // Not saved: EnableRendering (diagnostics), Resolutions (built in InitDefault())
        protected override void Serialize(XmlDocument xmlDoc, XmlNode node)
        {
            base.Serialize(xmlDoc, node);

            XmlUtils.AddNodeBool    (xmlDoc, node, "UseNamesAsIds"                  , UseNamesAsIds                 );
            XmlUtils.AddNodeFloat   (xmlDoc, node, "MouseCameraPanSpeed"            , MouseCameraPanSpeed           );
            XmlUtils.AddNodeFloat   (xmlDoc, node, "MouseCameraRotationSpeedPitch"  , MouseCameraRotationSpeedPitch );
            XmlUtils.AddNodeFloat   (xmlDoc, node, "MouseCameraRotationSpeedYaw"    , MouseCameraRotationSpeedYaw   );
            XmlUtils.AddNodeBool    (xmlDoc, node, "MouseCameraRotationInvPitch"    , MouseCameraRotationInvPitch   );
            XmlUtils.AddNodeBool    (xmlDoc, node, "MouseCameraRotationInvYaw"      , MouseCameraRotationInvYaw     );
            XmlUtils.AddNodeBool    (xmlDoc, node, "MouseWheelInvert"               , MouseWheelInvert              );
            XmlUtils.AddNodeFloat   (xmlDoc, node, "MouseWheelSpeed"                , MouseWheelSpeed               );
            XmlUtils.AddNodeBool    (xmlDoc, node, "UseShiftKeyToZoom"              , UseShiftKeyToZoom             );
            XmlUtils.AddNodeBool    (xmlDoc, node, "UseAltRmbForCameraRotation"     , UseAltRmbForCameraRotation    );

            XmlUtils.AddNodeFloat   (xmlDoc, node, "CameraPosDamping"               , CameraPosDamping              );
            XmlUtils.AddNodeFloat   (xmlDoc, node, "CameraRotDamping"               , CameraRotDamping              );
            XmlUtils.AddNodeFloat   (xmlDoc, node, "CameraDistDamping"              , CameraDistDamping             );

            XmlUtils.AddNodeBool    (xmlDoc, node, "MonitorFileSystemChanges"       , MonitorFileSystemChanges      );

            XmlUtils.AddNodeBool    (xmlDoc, node, "ConstAspectRatioPreview"        , ConstAspectRatioPreview       );
            XmlUtils.AddNodeInt     (xmlDoc, node, "CurrentResolutionIndex"         , CurrentResolutionIndex        );
            XmlUtils.AddNodeBool    (xmlDoc, node, "PreviewLastSelectedPass"        , PreviewLastSelectedPass       );

            XmlNode nodeExportAnimSettings = XmlUtils.AddNode(xmlDoc, node, "ExportAnimSettings");
            ExportAnimSettings.Serialize(xmlDoc, nodeExportAnimSettings);
        }

        public override bool Deserialize(XmlNode parentNode)
        {
            if (!base.Deserialize(parentNode))
                return false;

            XmlUtils.DeserializeBool    (parentNode, "UseNamesAsIds"                  , ref UseNamesAsIds                 );
            XmlUtils.DeserializeFloat   (parentNode, "MouseCameraPanSpeed"            , ref MouseCameraPanSpeed           );
            XmlUtils.DeserializeFloat   (parentNode, "MouseCameraRotationSpeedPitch"  , ref MouseCameraRotationSpeedPitch );
            XmlUtils.DeserializeFloat   (parentNode, "MouseCameraRotationSpeedYaw"    , ref MouseCameraRotationSpeedYaw   );
            XmlUtils.DeserializeBool    (parentNode, "MouseCameraRotationInvPitch"    , ref MouseCameraRotationInvPitch   );
            XmlUtils.DeserializeBool    (parentNode, "MouseCameraRotationInvYaw"      , ref MouseCameraRotationInvYaw     );
            XmlUtils.DeserializeBool    (parentNode, "MouseWheelInvert"               , ref MouseWheelInvert              );
            XmlUtils.DeserializeFloat   (parentNode, "MouseWheelSpeed"                , ref MouseWheelSpeed               );
            XmlUtils.DeserializeBool    (parentNode, "UseShiftKeyToZoom"              , ref UseShiftKeyToZoom             );
            XmlUtils.DeserializeBool    (parentNode, "UseAltRmbForCameraRotation"     , ref UseAltRmbForCameraRotation    );

            XmlUtils.DeserializeFloat   (parentNode, "CameraPosDamping"               , ref CameraPosDamping              );
            XmlUtils.DeserializeFloat   (parentNode, "CameraRotDamping"               , ref CameraRotDamping              );
            XmlUtils.DeserializeFloat   (parentNode, "CameraDistDamping"              , ref CameraDistDamping             );

            XmlUtils.DeserializeBool    (parentNode, "MonitorFileSystemChanges"       , ref MonitorFileSystemChanges      );

            XmlUtils.DeserializeBool    (parentNode, "ConstAspectRatioPreview"        , ref ConstAspectRatioPreview       );
            XmlUtils.DeserializeInt     (parentNode, "CurrentResolutionIndex"         , ref CurrentResolutionIndex        );
            XmlUtils.DeserializeBool    (parentNode, "PreviewLastSelectedPass"        , ref PreviewLastSelectedPass       );

            // the list of resolutions may have changed since the file was saved
            CurrentResolutionIndex = Math.Clamp(CurrentResolutionIndex, 0, Math.Max(0, Resolutions.Count - 1));

            XmlNode? nodeExportAnimSettings = parentNode.SelectSingleNode("ExportAnimSettings");
            if (nodeExportAnimSettings != null)
                ExportAnimSettings.Deserialize(nodeExportAnimSettings);

            return true;
        }

    }
}
