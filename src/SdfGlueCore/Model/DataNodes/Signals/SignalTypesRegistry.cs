//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.DataNodes.Signals
{
    public class SignalTypeInfo
    {
        public  string                      TypeName;       // stored in the project file - do not change
        public  string                      DisplayName;
        public  Func<SignalElement>         Create;

        public SignalTypeInfo(string typeName, string displayName, Func<SignalElement> create)
        {
            TypeName    = typeName;
            DisplayName = displayName;
            Create      = create;
        }
    }

    // Signal sources and operators are computed in C#, so they are registered here
    // (not loaded from XML definitions like SDF functions)
    public static class SignalTypesRegistry
    {
        public static readonly List<SignalTypeInfo> Sources = new List<SignalTypeInfo>()
        {
            new SignalTypeInfo("Oscillator"     , "Oscillator"      , () => new SignalSrcOscillator()   ),
            new SignalTypeInfo("SmoothNoise"    , "Smooth noise"    , () => new SignalSrcSmoothNoise()  ),
            new SignalTypeInfo("RandomStep"     , "Random step"     , () => new SignalSrcRandomStep()   ),
            new SignalTypeInfo("Time"           , "Time"            , () => new SignalSrcTime()         ),
            new SignalTypeInfo("Constant"       , "Constant"        , () => new SignalSrcConstant()     ),
            new SignalTypeInfo("Manual"         , "Manual"          , () => new SignalSrcManual()       ),
            new SignalTypeInfo("MidiCC"         , "MIDI CC"         , () => new SignalSrcMidiCC()       ),
            new SignalTypeInfo("SignalRef"      , "Other signal"    , () => new SignalSrcSignalRef()    ),
        };

        public static readonly List<SignalTypeInfo> Operators = new List<SignalTypeInfo>()
        {
            new SignalTypeInfo("ScaleOffset"    , "Scale / offset"  , () => new SignalOpScaleOffset()   ),
            new SignalTypeInfo("Remap"          , "Remap"           , () => new SignalOpRemap()         ),
            new SignalTypeInfo("Clamp"          , "Clamp"           , () => new SignalOpClamp()         ),
            new SignalTypeInfo("Math"           , "Math"            , () => new SignalOpMath()          ),
            new SignalTypeInfo("Curve"          , "Curve (pow)"     , () => new SignalOpCurve()         ),
            new SignalTypeInfo("Quantize"       , "Quantize"        , () => new SignalOpQuantize()      ),
            new SignalTypeInfo("Smooth"         , "Smooth"          , () => new SignalOpSmooth()        ),
            new SignalTypeInfo("Slew"           , "Slew limit"      , () => new SignalOpSlew()          ),
            new SignalTypeInfo("Combine"        , "Combine"         , () => new SignalOpCombine()       ),
        };

        public static SignalTypeInfo? FindSource(string? typeName)
        {
            return Sources.Find(t => t.TypeName == typeName);
        }

        public static SignalTypeInfo? FindOperator(string? typeName)
        {
            return Operators.Find(t => t.TypeName == typeName);
        }

        public static SignalSource? CreateSource(string? typeName)
        {
            SignalTypeInfo? info = FindSource(typeName);
            if (info == null)
                return null;

            SignalSource? src = info.Create() as SignalSource;
            if (src != null)
                src.TypeName = info.TypeName;
            return src;
        }

        public static SignalOperator? CreateOperator(string? typeName)
        {
            SignalTypeInfo? info = FindOperator(typeName);
            if (info == null)
                return null;

            SignalOperator? op = info.Create() as SignalOperator;
            if (op != null)
                op.TypeName = info.TypeName;
            return op;
        }

        public static string GetDisplayName(SignalElement element)
        {
            SignalTypeInfo? info = (element is SignalSource) ? FindSource(element.TypeName) : FindOperator(element.TypeName);
            return info?.DisplayName ?? element.TypeName;
        }
    }
}
