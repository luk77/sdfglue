//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.CodeFragments;
using SingleDocAppFramework.Ui.CodeView;

namespace SdfGlueUi.Ui.Components
{
    // Languages of the code view in SdfGlue: framework GLSL/HLSL + names of the function library,
    // Shadertoy/Unity built-ins and generated parameters. Rebuilt only after a reload of the library (FunctionLibrary.Version).
    public static class SdfGlueCodeLanguages
    {
        // functions from the library (Workspace/Functions)
        public static readonly CodeTokenKind    LibraryFunction     = CodeTokenKinds.Register("sdfglue.libraryFunction", "Library function (SdfGlue)",
                                                                                              CodeTokenKinds.Rgb(0xE5C07B), CodeTokenKinds.Rgb(0xB35900));

        // prefix of the generated object parameters (ShaderNames.GetParamName)
        private const string                    ObjectParamPrefix   = "g_obj_";

        private static FunctionLibrary?         library_            = null;
        private static int                      libraryVersion_     = -1;
        private static CodeLanguage?            glsl_               = null;
        private static CodeLanguage?            hlsl_               = null;

        // Registers the token kinds (before the settings dialog lists them) - called by the UI manager at startup
        public static void RegisterTokenKinds()
        {
            _ = LibraryFunction;
        }

        public static CodeLanguage GetGlsl(FunctionLibrary library)
        {
            Update(library);
            return glsl_!;
        }

        // Unity export
        public static CodeLanguage GetHlsl(FunctionLibrary library)
        {
            Update(library);
            return hlsl_!;
        }

        private static void Update(FunctionLibrary library)
        {
            if (library == library_ && library.Version == libraryVersion_ && glsl_ != null && hlsl_ != null)
                return;

            library_        = library;
            libraryVersion_ = library.Version;

            List<string> functionNames = new List<string>();
            foreach(FunctionDefinitionsSet set in library.GetAllSets())
            {
                foreach(FunctionDefinition definition in set.GetDefinitions())
                {
                    if (!String.IsNullOrEmpty(definition.FunctionName))
                        functionNames.Add(definition.FunctionName);
                }
            }

            // uniforms and #define constants are colored by the declaration rules of the languages
            CodeLanguage glsl = CodeLanguages.Glsl();
            glsl.Name = "GLSL (SdfGlue)";
            glsl.AddWords(CodeTokenKinds.Builtin, "iTime iTimeDelta iFrame iFrameRate iResolution iMouse iDate iChannel0 iChannel1 iChannel2 iChannel3 iChannelTime iChannelResolution iSampleRate");
            glsl.AddWords(LibraryFunction, functionNames);
            glsl.AddPrefix(ObjectParamPrefix, CodeTokenKinds.Uniform);

            CodeLanguage hlsl = CodeLanguages.Hlsl();
            hlsl.Name = "HLSL (SdfGlue, Unity)";
            hlsl.AddWords(CodeTokenKinds.Keyword, "Shader Properties SubShader Pass Tags HLSLPROGRAM ENDHLSL CGPROGRAM ENDCG Cull ZWrite ZTest Blend Fallback");
            hlsl.AddWords(CodeTokenKinds.Builtin, "_Time _SinTime _CosTime _ScreenParams _ProjectionParams _WorldSpaceCameraPos unity_ObjectToWorld unity_WorldToObject");
            hlsl.AddWords(LibraryFunction, functionNames);
            hlsl.AddPrefix(ObjectParamPrefix, CodeTokenKinds.Uniform);

            glsl_ = glsl;
            hlsl_ = hlsl;
        }
    }
}
