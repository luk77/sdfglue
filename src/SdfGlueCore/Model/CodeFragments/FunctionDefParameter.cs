//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.Entities;
using SingleDocAppCore.Model;

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
        public          LimitsType                      LimitsType;
        public          bool                            EditInDegrees;
        public          ParamEditorType                 EditorType;

        // Key used in the parameter values dictionaries (ParametersValuesCollection), e.g. "vec3:radius".
        // It contains the type, so after changing the function of an entity a parameter with the same name,
        // but a different type (e.g. float/vec3 'offset') gets its own value instead of reusing an incompatible one.
        // ParameterName stays a plain name, because it is used as an identifier in the generated code.
        // Empty if ParameterName is null (such parameters are skipped everywhere).
        public          string                          ParameterKey { get; private set; } = "";

        internal bool Deserialize(XmlNode node)
        {
            Type            = LoadParameterType(node, "type");
            DisplayName     = XmlUtils.LoadAttributeAsString(node, "displayName"    , null);
            ParameterName   = XmlUtils.LoadAttributeAsString(node, "parameterName"  , null);
            ParameterKey    = ParameterName != null ? ParametersValuesCollection.MakeKey(Type, ParameterName) : "";
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
                    DefaultVal = new ExVector2WithSignal(valV2);
                    break;

                case SdfParamType.Vec3:
                    System.Numerics.Vector3 valV3 = XmlUtils.LoadAttributeAsVec3(node, "default", System.Numerics.Vector3.Zero);
                    DefaultVal = new ExVector3WithSignal(valV3);
                    break;

                case SdfParamType.Vec4:
                    System.Numerics.Vector4 valV4 = XmlUtils.LoadAttributeAsVec4(node, "default", System.Numerics.Vector4.Zero);
                    DefaultVal = new ExVector4WithSignal(valV4);
                    break;
            }

            bool hasMin = node.Attributes?.GetNamedItem("min") != null;
            bool hasMax = node.Attributes?.GetNamedItem("max") != null;
            LimitsType = LimitsType.None;
            if (hasMin)
                LimitsType = LimitsType.Min;
            if (hasMax)
                LimitsType = LimitsType.MinMax;

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

        [MemberNotNullWhen(true, nameof(ParameterName))]
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
