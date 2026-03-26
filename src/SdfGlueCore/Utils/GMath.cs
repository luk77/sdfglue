//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Numerics;

namespace SdfGlueCore.Utils
{
    public class GMath
    {
        public static readonly float                Pi              = (float)Math.PI;
        public static readonly float                PiMul2			= 2.0f * (float)Math.PI;
        public static readonly float                PiMul4			= 4.0f * (float)Math.PI;
        public static readonly float                PiDiv2			= 0.5f * (float)Math.PI;
        public static readonly float                PiDiv4			= 0.25f* (float)Math.PI;
        public static readonly float                PiInv			= 1.0f / (float)Math.PI;
        public static readonly float                DegToRad        = 0.01745329251994329547f;
        public static readonly float                RadToDeg        = 57.29577951308232286465f;

        public static float Lerp(float a, float b, float weight)
        { 
            return a * (1 - weight) + b * weight; 
        }

        public static float Smooth(float current, float target, float lambda, float dt)
        {
            float t = 1.0f - MathF.Exp(-lambda * dt);
            return current + (target - current) * t;
        }

        public static Vector2 Smooth(Vector2 current, Vector2 target, float lambda, float dt)
        {
            float t = 1.0f - MathF.Exp(-lambda * dt);
            return current + (target - current) * t;
        }

        public static Vector3 Smooth(Vector3 current, Vector3 target, float lambda, float dt)
        {
            float t = 1.0f - MathF.Exp(-lambda * dt);
            return current + (target - current) * t;
        }
    }
}
