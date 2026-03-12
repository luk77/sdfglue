//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using OpenTK.Mathematics;

namespace SdfGlueEditor.Utils
{
    public class MathUtils
    {
        public static float RadToDeg                = 180.0f / (float)System.Math.PI;
        public static float DegToRad                = (float)System.Math.PI / 180.0f;

        public static Vector2 ToGlVec2(System.Numerics.Vector2 val)
        {
            return new Vector2(val.X, val.Y);
        }

        public static System.Numerics.Vector2 ToNumericsVec4(Vector2 val)
        {
            return new System.Numerics.Vector2(val.X, val.Y);
        }

        public static Vector3 ToGlVec3(System.Numerics.Vector3 val)
        {
            return new Vector3(val.X, val.Y, val.Z);
        }

        public static System.Numerics.Vector3 ToNumericsVec3(Vector3 val)
        {
            return new System.Numerics.Vector3(val.X, val.Y, val.Z);
        }

        public static Vector4 ToGlVec4(System.Numerics.Vector4 val)
        {
            return new Vector4(val.X, val.Y, val.Z, val.W);
        }

        public static System.Numerics.Vector4 ToNumericsVec4(Vector4 val)
        {
            return new System.Numerics.Vector4(val.X, val.Y, val.Z, val.W);
        }

    }
}
