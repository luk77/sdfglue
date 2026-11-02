//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

namespace SingleDocAppFramework.Ui.CodeView
{
    // General language definitions for the code view. Every call returns a new instance - applications extend it
    // with their own words (AddWords, AddPrefix ...) without affecting other users.
    // Only standard language elements belong here; names of a specific application or engine are added by the application.
    public static class CodeLanguages
    {
        //-------------------------------------------------------------------
        // C-like (generic)
        //-------------------------------------------------------------------

        public static CodeLanguage CLike()
        {
            CodeLanguage lang = new CodeLanguage("C-like");
            lang.StringQuotes   = "\"'";
            lang.NumberSuffixes = "uUlLfF";

            lang.AddWords(CodeTokenKinds.Keyword,
                "if else for while do switch case default break continue goto return sizeof typedef struct union enum " +
                "const static extern volatile register inline true false NULL nullptr");
            lang.AddWords(CodeTokenKinds.Type,
                "void bool char short int long float double signed unsigned size_t " +
                "int8_t int16_t int32_t int64_t uint8_t uint16_t uint32_t uint64_t");
            lang.AddWords(CodeTokenKinds.Macro, "__LINE__ __FILE__ __DATE__ __TIME__");

            lang.AddDeclarationRule("#define", CodeTokenKinds.Macro, CodeDeclarationName.NextIdentifier);
            return lang;
        }

        //-------------------------------------------------------------------
        // GLSL (desktop 3.30+ and ES 3.00)
        //-------------------------------------------------------------------

        public static CodeLanguage Glsl()
        {
            CodeLanguage lang = new CodeLanguage("GLSL");
            lang.NumberSuffixes = "uUfFlL";

            lang.AddWords(CodeTokenKinds.Keyword,
                "if else for while do switch case default break continue return discard struct " +
                "const uniform in out inout attribute varying buffer shared layout centroid flat smooth noperspective " +
                "patch sample invariant precise precision highp mediump lowp coherent volatile restrict readonly writeonly " +
                "subroutine true false");

            lang.AddWords(CodeTokenKinds.Type, "void bool int uint float double");
            lang.AddWords(CodeTokenKinds.Type, Expand("vec dvec bvec ivec uvec", "2 3 4"));
            lang.AddWords(CodeTokenKinds.Type, Expand("mat dmat", "2 3 4 2x2 2x3 2x4 3x2 3x3 3x4 4x2 4x3 4x4"));
            lang.AddWords(CodeTokenKinds.Type, Expand("sampler isampler usampler", "1D 2D 3D Cube 2DRect 1DArray 2DArray CubeArray Buffer 2DMS 2DMSArray"));
            lang.AddWords(CodeTokenKinds.Type, "sampler1DShadow sampler2DShadow samplerCubeShadow sampler2DRectShadow sampler1DArrayShadow sampler2DArrayShadow samplerCubeArrayShadow");
            lang.AddWords(CodeTokenKinds.Type, Expand("image iimage uimage", "1D 2D 3D Cube 2DRect 1DArray 2DArray CubeArray Buffer 2DMS 2DMSArray"));
            lang.AddWords(CodeTokenKinds.Type, "atomic_uint");

            lang.AddWords(CodeTokenKinds.Builtin,
                // angles, trigonometry, exponential
                "radians degrees sin cos tan asin acos atan sinh cosh tanh asinh acosh atanh " +
                "pow exp log exp2 log2 sqrt inversesqrt " +
                // common
                "abs sign floor trunc round roundEven ceil fract mod modf min max clamp mix step smoothstep isnan isinf " +
                "floatBitsToInt floatBitsToUint intBitsToFloat uintBitsToFloat fma frexp ldexp " +
                // packing
                "packUnorm2x16 packSnorm2x16 packUnorm4x8 packSnorm4x8 packHalf2x16 packDouble2x32 " +
                "unpackUnorm2x16 unpackSnorm2x16 unpackUnorm4x8 unpackSnorm4x8 unpackHalf2x16 unpackDouble2x32 " +
                // geometric, matrix, vector relational
                "length distance dot cross normalize faceforward reflect refract " +
                "matrixCompMult outerProduct transpose determinant inverse " +
                "lessThan lessThanEqual greaterThan greaterThanEqual equal notEqual any all not " +
                // integer
                "uaddCarry usubBorrow umulExtended imulExtended bitfieldExtract bitfieldInsert bitfieldReverse bitCount findLSB findMSB " +
                // textures
                "textureSize textureQueryLod textureQueryLevels textureSamples texture textureProj textureLod textureOffset " +
                "texelFetch texelFetchOffset textureProjOffset textureLodOffset textureProjLod textureProjLodOffset " +
                "textureGrad textureGradOffset textureProjGrad textureProjGradOffset textureGather textureGatherOffset textureGatherOffsets " +
                "texture1D texture2D texture3D textureCube texture2DLod shadow2D " +
                // derivatives, interpolation, misc
                "dFdx dFdy dFdxFine dFdyFine dFdxCoarse dFdyCoarse fwidth fwidthFine fwidthCoarse " +
                "interpolateAtCentroid interpolateAtSample interpolateAtOffset " +
                "imageLoad imageStore imageSize atomicAdd atomicMin atomicMax atomicAnd atomicOr atomicXor atomicExchange atomicCompSwap " +
                "barrier memoryBarrier groupMemoryBarrier EmitVertex EndPrimitive");

            lang.AddWords(CodeTokenKinds.Macro, "__LINE__ __FILE__ __VERSION__ GL_ES GL_FRAGMENT_PRECISION_HIGH");

            // gl_FragCoord, gl_Position ...
            lang.AddPrefix("gl_", CodeTokenKinds.Builtin);

            lang.AddDeclarationRule("uniform", CodeTokenKinds.Uniform, CodeDeclarationName.LastIdentifierBeforePunctuation);
            lang.AddDeclarationRule("#define", CodeTokenKinds.Macro, CodeDeclarationName.NextIdentifier);
            return lang;
        }

        //-------------------------------------------------------------------
        // HLSL
        //-------------------------------------------------------------------

        public static CodeLanguage Hlsl()
        {
            CodeLanguage lang = new CodeLanguage("HLSL");
            lang.NumberSuffixes = "uUfFhHlL";

            lang.AddWords(CodeTokenKinds.Keyword,
                "if else for while do switch case default break continue return discard struct class interface " +
                "cbuffer tbuffer register packoffset static const uniform extern in out inout inline groupshared shared volatile " +
                "nointerpolation linear centroid noperspective sample precise row_major column_major snorm unorm " +
                "typedef namespace technique technique10 technique11 pass compile true false");

            lang.AddWords(CodeTokenKinds.Type, "void bool int uint dword half float double min16float min10float min16int min12int min16uint string vector matrix");
            lang.AddWords(CodeTokenKinds.Type, Expand("bool int uint half float double min16float min10float min16int min12int min16uint", "1 2 3 4 1x1 1x2 1x3 1x4 2x1 2x2 2x3 2x4 3x1 3x2 3x3 3x4 4x1 4x2 4x3 4x4"));
            lang.AddWords(CodeTokenKinds.Type,
                "Texture1D Texture1DArray Texture2D Texture2DArray Texture2DMS Texture2DMSArray Texture3D TextureCube TextureCubeArray " +
                "RWTexture1D RWTexture1DArray RWTexture2D RWTexture2DArray RWTexture3D " +
                "Buffer RWBuffer StructuredBuffer RWStructuredBuffer AppendStructuredBuffer ConsumeStructuredBuffer " +
                "ByteAddressBuffer RWByteAddressBuffer SamplerState SamplerComparisonState " +
                "sampler sampler1D sampler2D sampler3D samplerCUBE sampler_state texture texture1D texture2D texture3D textureCUBE");

            lang.AddWords(CodeTokenKinds.Builtin,
                "abort abs acos all any asdouble asfloat asin asint asuint atan atan2 ceil clamp clip cos cosh countbits cross " +
                "ddx ddx_coarse ddx_fine ddy ddy_coarse ddy_fine degrees determinant distance dot dst errorf exp exp2 " +
                "f16tof32 f32tof16 faceforward firstbithigh firstbitlow floor fma fmod frac frexp fwidth " +
                "isfinite isinf isnan ldexp length lerp lit log log10 log2 mad max min modf msad4 mul noise normalize " +
                "pow printf radians rcp reflect refract reversebits round rsqrt saturate sign sin sincos sinh smoothstep sqrt step " +
                "tan tanh transpose trunc " +
                "tex1D tex1Dbias tex1Dgrad tex1Dlod tex1Dproj tex2D tex2Dbias tex2Dgrad tex2Dlod tex2Dproj " +
                "tex3D tex3Dbias tex3Dgrad tex3Dlod tex3Dproj texCUBE texCUBEbias texCUBEgrad texCUBElod texCUBEproj " +
                "AllMemoryBarrier AllMemoryBarrierWithGroupSync DeviceMemoryBarrier DeviceMemoryBarrierWithGroupSync " +
                "GroupMemoryBarrier GroupMemoryBarrierWithGroupSync " +
                "InterlockedAdd InterlockedAnd InterlockedCompareExchange InterlockedCompareStore InterlockedExchange " +
                "InterlockedMax InterlockedMin InterlockedOr InterlockedXor");

            lang.AddWords(CodeTokenKinds.Macro, "__LINE__ __FILE__");

            // semantics: SV_Target, SV_Position ...
            lang.AddPrefix("SV_", CodeTokenKinds.Builtin);

            lang.AddDeclarationRule("#define", CodeTokenKinds.Macro, CodeDeclarationName.NextIdentifier);
            return lang;
        }

        // Expand("vec ivec", "2 3") -> vec2 vec3 ivec2 ivec3
        public static IEnumerable<string> Expand(string prefixes, string suffixes)
        {
            string[] suffixList = suffixes.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            foreach(string prefix in prefixes.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach(string suffix in suffixList)
                    yield return prefix + suffix;
            }
        }
    }
}
