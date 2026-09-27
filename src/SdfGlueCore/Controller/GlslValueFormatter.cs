//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.BaseTypes;
using System.Globalization;

namespace SdfGlueCore.Controller
{
    // Formats simple type values as GLSL literals (e.g. "vec3(1.00000, 0.00000, 0.50000)").
    // Kept in SdfGlueCore, because GLSL is SdfGlue-specific (SingleDocAppCore types know nothing about shaders).
    public static class GlslValueFormatter
    {
        public static readonly string      FloatFormat             = "0.00000";

        public static string FormatAsStringForUniform(this ISimpleType value)
        {
            switch (value)
            {
                case ExFloat    v:  return FormatFloat(v.Val);
                case ExInt      v:  return v.Val.ToString(CultureInfo.InvariantCulture);
                case ExVector2  v:  return String.Format("vec2({0}, {1})"          , FormatFloat(v.Val.X), FormatFloat(v.Val.Y));
                case ExVector3  v:  return String.Format("vec3({0}, {1}, {2})"     , FormatFloat(v.Val.X), FormatFloat(v.Val.Y), FormatFloat(v.Val.Z));
                case ExVector4  v:  return String.Format("vec4({0}, {1}, {2}, {3})", FormatFloat(v.Val.X), FormatFloat(v.Val.Y), FormatFloat(v.Val.Z), FormatFloat(v.Val.W));
                case ExBool     v:  return "Unsupported type: bool: " + v.Val;
                case ExString   v:  return "Unsupported type: string: " + v.Val;
                default:            return "Unsupported type: " + value.GetType().Name;
            }
        }

        private static string FormatFloat(float val)
        {
            return val.ToString(FloatFormat, CultureInfo.InvariantCulture);
        }
    }
}
