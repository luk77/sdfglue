//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.CodeFragments
{
    public enum ParamEditorType
    {
        Default,
        Color,          // vec3 edited as a color
        Toggle,         // int edited as a checkbox (0/1)
        Combo           // int edited as a combo box (index of one of FunctionDefParameter.Options)
    }
}
