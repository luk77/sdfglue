//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    public enum SignalParamType
    {
        Float,
        Int,
        Bool,           // stored as ExInt (0/1)
        Enum,           // stored as ExInt (index in EnumNames)
        SignalRef,      // stored as ExInt (id of another signal, 0 = none)
    }

    // Description of a single parameter of a signal source or operator (used by the UI and serialization)
    public class SignalParamDef
    {
        public  string              Name;
        public  string              DisplayName;
        public  SignalParamType     Type;
        public  float               Speed           = 0.01f;
        public  LimitsType          LimitsType      = LimitsType.None;
        public  float               MinVal          = 0.0f;
        public  float               MaxVal          = 0.0f;
        public  string[]?           EnumNames       = null;

        public SignalParamDef(string name, string displayName, SignalParamType type)
        {
            Name        = name;
            DisplayName = displayName;
            Type        = type;
        }

        public static string TypeToString(SignalParamType type)
        {
            return (type == SignalParamType.Float) ? "float" : "int";
        }
    }
}
