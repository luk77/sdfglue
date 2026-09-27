//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model
{
    public class ExportAnimationSettings
    {
        public  int         Fps                     = 60;
        public  int         NumOfFramesToPrerender  = 10;
        public  int         NumOfFramesToExport     = 120;

        public void Serialize(XmlDocument xmlDoc, XmlNode parentNode)
        {
            XmlUtils.AddNodeInt     (xmlDoc, parentNode, "Fps"                      , Fps                       );
            XmlUtils.AddNodeInt     (xmlDoc, parentNode, "NumOfFramesToPrerender"   , NumOfFramesToPrerender    );
            XmlUtils.AddNodeInt     (xmlDoc, parentNode, "NumOfFramesToExport"      , NumOfFramesToExport       );
        }

        public bool Deserialize(XmlNode parentNode)
        {
            if (parentNode == null)
                return false;

            XmlUtils.DeserializeInt (parentNode, "Fps"                      , ref Fps                       );
            XmlUtils.DeserializeInt (parentNode, "NumOfFramesToPrerender"   , ref NumOfFramesToPrerender    );
            XmlUtils.DeserializeInt (parentNode, "NumOfFramesToExport"      , ref NumOfFramesToExport       );

            return true;
        }
    }
}
