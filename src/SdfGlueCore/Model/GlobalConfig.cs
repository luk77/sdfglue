//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;

namespace SdfGlueCore.Model
{
    public class GlobalConfig
    {
        public static readonly float    MinDistanceToTarget     = 0.001f;
        public static readonly float    ConstTimeStep           = 1.0f / 10.0f;

        public  bool        EnableRendering                     = true;
        public  bool        UseNamesAsIds                       = false;        // to ustawienie powoduje, że w shaderze zostaną użyte nazwy obiektów zamiast identyfikatorów liczbowych. Nazwy muszą być unikalne.
        public  float       MouseCameraPanSpeed                 = 1.0f;
        public  float       MouseCameraRotationSpeedPitch       = 1.0f;
        public  float       MouseCameraRotationSpeedYaw         = 1.0f;
        public  bool        MouseCameraRotationInvPitch         = false;
        public  bool        MouseCameraRotationInvYaw           = false;
        public  bool        MouseWheelInvert                    = false;
        public  float       MouseWheelSpeed                     = 1.0f;
        public  bool        UseShiftKeyToZoom                   = false;
        public  float       UiTextScaleFactor                   = 1.0f;

        public bool         MonitorFileSystemChanges            = true;

        //public  bool        UseRenderFrequencyLimit             = true;
        public  bool        UseUpdateFrequencyLimit             = true;
        //public  int         RenderFrequencyLimit                = 60;
        public  int         UpdateFrequencyLimit                = 60;

        public bool                     ConstAspectRatioPreview = true;
        public List<IntCoords>          Resolutions             = new List<IntCoords>();
        public string[]                 ResolutionsAsStrings    = new string[1];
        public int                      CurrentResolutionIndex  = 0;
        // to nie takie proste - przy pathtrace'ingu trzeba zakumulować więcej klatek
        // a nie tylko zmienić rozdzielczość na jedną klatkę
        //public int                      ExportImageResolutionIndex = 0;

        public bool                     PreviewLastSelectedPass = true;

        public ExportAnimationSettings  ExportAnimSettings  = new ExportAnimationSettings();

        public void InitDefault()
        {
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
            //Resolutions.Add(new IntCoords(1920 * 2, 1080 * 2)); (to samo co niżej)
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


    }
}
