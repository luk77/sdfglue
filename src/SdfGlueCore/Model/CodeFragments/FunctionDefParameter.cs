//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model.CodeFragments
{
    public class FunctionDefParameter
    {
        // this is for 'extrusion' operator
        public static readonly string                   SpecialParamCurrObjPos          = "{SDFG_CURR_OBJ_POS}";
        public static readonly string                   SpecialParamCurrObjCellIndex    = "{SDFG_CURR_OBJ_CELL_INDEX}";

        public          string?                         DisplayName;
        public          string?                         ParameterName;
        public          SdfParamType                    Type;
        public          ISimpleType?                    DefaultVal;
        public          float                           MinVal;
        public          float                           MaxVal;
        public          float                           ValSpeed = 0.01f;
        public          SdfParamLimitsType              LimitsType;
        public          bool                            EditInDegrees;
        public          ParamEditorType                 EditorType;

        // Klucz do używania w słownikach wartości parametrów
        // Na razie jest to to samo co ParameterName, ale docelowo powinien zawierać także typ parametru.
        // Dzięki temu uniknie się problemów przy dynamicznych zmianach funkcji,
        // gdy parametr ma tą samą nazwę, ale typ się nie zgadza.
        //public string ParameterNameKey
        //{
        //    get
        //    {
        //        //return ParameterName;
        //        return Type.ToString() + "_" + ParameterName;
        //    }
        //}

        internal bool Deserialize(XmlNode node)
        {
            Type            = LoadParameterType(node, "type");
            DisplayName     = XmlUtils.LoadAttributeAsString(node, "displayName"    , null);
            ParameterName   = XmlUtils.LoadAttributeAsString(node, "parameterName"  , null);
            //DefaultVal      = XmlUtils.LoadAttributeAsFloat (node, "default"        , 0.0f);
            MinVal          = XmlUtils.LoadAttributeAsFloat (node, "min"            , 0.0f);
            MaxVal          = XmlUtils.LoadAttributeAsFloat (node, "max"            , 0.0f);
            ValSpeed        = XmlUtils.LoadAttributeAsFloat (node, "speed"          , 0.01f);
            EditInDegrees   = XmlUtils.LoadAttributeAsBool  (node, "editInDegrees"  , false);

            //string strDefaultVal = XmlUtils.LoadAttributeAsString(node, "default", "");
            switch (Type)
            {
                case SdfParamType.Float:
                    float valF = XmlUtils.LoadAttributeAsFloat(node, "default", 0.0f);
                    ExFloatWithSignal exObj = new ExFloatWithSignal(valF);
                    DefaultVal = exObj;
                    //exObj.Val = exObj.Val * (EditInDegrees ? MathUtils.DegToRad : 1.0f); 
                    exObj.Val = exObj.Val * (EditInDegrees ? GMath.DegToRad : 1.0f); 
                    break;

                case SdfParamType.Int:
                    int valI = (int)XmlUtils.LoadAttributeAsFloat(node, "default", 0.0f);
                    DefaultVal = new ExInt(valI);
                    break;

                case SdfParamType.Vec2:
                    System.Numerics.Vector2 valV2 = XmlUtils.LoadAttributeAsVec2(node, "default", System.Numerics.Vector2.Zero);
                    DefaultVal = new ExVector2(valV2);
                    break;

                case SdfParamType.Vec3:
                    System.Numerics.Vector3 valV3 = XmlUtils.LoadAttributeAsVec3(node, "default", System.Numerics.Vector3.Zero);
                    DefaultVal = new ExVector3(valV3);
                    break;

                case SdfParamType.Vec4:
                    System.Numerics.Vector4 valV4 = XmlUtils.LoadAttributeAsVec4(node, "default", System.Numerics.Vector4.Zero);
                    DefaultVal = new ExVector4(valV4);
                    break;
            }

            bool hasMin = node.Attributes?.GetNamedItem("min") != null;
            bool hasMax = node.Attributes?.GetNamedItem("max") != null;
            LimitsType = SdfParamLimitsType.None;
            if (hasMin)
                LimitsType = SdfParamLimitsType.Min;
            if (hasMax)
                LimitsType = SdfParamLimitsType.MinMax;

            EditorType = ParamEditorType.Default;
            string? strEditor = XmlUtils.LoadAttributeAsString(node, "editor", null);
            if (!String.IsNullOrEmpty(strEditor) && strEditor.ToLower() == "color")
                EditorType = ParamEditorType.Color;

            return true;
        }

        private static SdfParamType LoadParameterType(XmlNode node, string attributeName)
        {
            SdfParamType type = SdfParamType.Float; // Default

            string? typeStr = XmlUtils.LoadAttributeAsString(node, attributeName, null);
            if (String.IsNullOrEmpty(typeStr))
                return type;

                 if (typeStr == "float"  ) { type = SdfParamType.Float; }
            else if (typeStr == "int"    ) { type = SdfParamType.Int;   }
            else if (typeStr == "vec2"   ) { type = SdfParamType.Vec2;  }
            else if (typeStr == "vec3"   ) { type = SdfParamType.Vec3;  }
            else if (typeStr == "vec4"   ) { type = SdfParamType.Vec4;  }
            //else if (typeStr == "bool"   ) { type = SdfParamType.Bool;  }

            return type;
        }

        public static string SdfTypeToString(SdfParamType type)
        {
            switch(type)
            {
                case SdfParamType.Float: return "float";
                case SdfParamType.Int  : return "int";
                case SdfParamType.Vec2 : return "vec2";
                case SdfParamType.Vec3 : return "vec3";
                case SdfParamType.Vec4 : return "vec4";
                //case SdfParamType.Bool : return "bool";
            }

            return "float";
        }

        public bool IsEditable()
        {
            if (ParameterName == null)
                return false;

            if ((ParameterName == SpecialParamCurrObjPos) ||
                (ParameterName == SpecialParamCurrObjCellIndex) )
                return false;

            return true;
        }
    }
}
