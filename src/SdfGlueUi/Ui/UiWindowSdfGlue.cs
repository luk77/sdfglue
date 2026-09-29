//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.Entities;
using SdfGlueUi.Ui.Properties;
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppFramework.Ui.Properties;
using SingleDocAppCore.Utils;
using SingleDocAppFramework.Ui;
using System.Numerics;

namespace SdfGlueUi.Ui
{
    public abstract class UiWindowSdfGlue : UiWindowBase
    {
        protected IUiExecutorSdfGlue ExecutorSdfGlue { get { return (IUiExecutorSdfGlue)Executor; } }
        // SdfGlue windows are registered only by UiManagerSdfGlue
        protected UiManagerSdfGlue   UiMgrSdfGlue    { get { return (UiManagerSdfGlue)uiMgr_; } }

        public DataModel GetModel()
        {
            return ExecutorSdfGlue.GetModel();
        }

        public static void BuildPropertiesForCompilationParameters(ref int index, FunctionEntity functionEntity, OnValueChanged? onValueChanged = null)
        {
            if (functionEntity.Definition == null)
                return;

            FunctionDefParametersCollection defParamsCollection     = functionEntity.Definition.CompilationParameters;
            ParametersValuesCollection      paramsValuesCollection  = functionEntity.CompilationParametersValues;

            foreach(FunctionDefParameter param in defParamsCollection)
            {
                if (param.ParameterName == null)
                    continue;

                // compilation params ignorują typ - to zawsze jest checkbox
                ExInt? exObj = paramsValuesCollection[param.ParameterKey] as ExInt;
                if (exObj == null)
                    continue;
                UiBool.Build(index++, param.DisplayName ?? param.ParameterName, exObj, onValueChanged );
            }
        }

        public void BuildPropertiesForParameters(ref int index, FunctionEntity functionEntity, OnValueChanged? onValueChanged = null)
        {
            if (functionEntity.Definition == null)
                return;

            FunctionDefParametersCollection defParamsCollection     = functionEntity.Definition.Parameters;
            ParametersValuesCollection      paramsValuesCollection  = functionEntity.ParametersValues;

            BuildPropertiesForFunctionDefinition(ref index, defParamsCollection, paramsValuesCollection, onValueChanged);
        }

        public void BuildPropertiesForMaterialParameters(ref int index, FunctionEntity functionEntity, OnValueChanged? onValueChanged = null)
        {
            if (functionEntity.Definition == null)
                return;

            FunctionDefParametersCollection defParamsCollection     = functionEntity.Definition.MaterialParameters;
            ParametersValuesCollection      paramsValuesCollection  = functionEntity.ParametersValues;
        
            BuildPropertiesForFunctionDefinition(ref index, defParamsCollection, paramsValuesCollection, onValueChanged);
        }


        //private void BuildPropertiesForSdf(ref int index, FunctionDefinition sdfDef, SdfObject sdfObj)
        public void BuildPropertiesForFunctionDefinition(ref int index, FunctionDefParametersCollection defParamsCollection , ParametersValuesCollection paramsValuesCollection, OnValueChanged? onValueChanged = null)
        {
            //FunctionDefParametersCollection defParamsCollection     = useCompilationParams ? functionEntity.Definition.CompilationParameters    : functionEntity.Definition.Parameters;
            //ParametersValuesCollection      paramsValuesCollection  = useCompilationParams ? functionEntity.CompilationParametersValues         : functionEntity.ParametersValues;

            foreach(FunctionDefParameter param in defParamsCollection)
            {
                if (!param.IsEditable())
                    continue;

                // Fall back to the parameter name when the definition has no display name
                string displayName = param.DisplayName ?? param.ParameterName;

//                if (useCompilationParams)
//                {
//                    // compilation params ignorują typ - to zawsze jest checkbox
//                    ExInt exObj = paramsValuesCollection[param.ParameterName] as ExInt;
//                    UiBool.Build(index++, param.DisplayName, exObj, onValueChanged );
//                }
//                else 
                if (param.Type == SdfParamType.Float)
                {
                    ExFloat? exObj = paramsValuesCollection[param.ParameterKey] as ExFloat;
                    if (exObj == null)
                        continue;

                    //float valF = exObj.Val;
                    //if (param.EditInDegrees)
                    //    valF *= GMath.RadToDeg;
                    //
                    //ImGui.PushID(index++);
                    //ImGui.Text(param.DisplayName);
                    //ImGui.PopID();
                    //ImGui.NextColumn();
                    //ImGui.SetNextItemWidth(-1);
                    //if (param.LimitsType == LimitsType.Min)
                    //{
                    //    ImGui.DragFloat("##value", ref valF, param.ValSpeed, param.MinVal);
                    //}
                    //else if (param.LimitsType == LimitsType.MinMax)
                    //{
                    //    ImGui.DragFloat("##value", ref valF, param.ValSpeed, param.MinVal, param.MaxVal);
                    //}
                    //else // None
                    //{
                    //    ImGui.DragFloat("##value", ref valF, param.ValSpeed);
                    //}
                    //ImGui.NextColumn();
                    //
                    //if (param.EditInDegrees)
                    //    valF *= GMath.DegToRad;
                    //
                    ////paramsValuesCollection[param.ParameterName] = valF;
                    //exObj.Val = valF;
                    //
                    //UiFloat.AddUndoHandler(param.DisplayName, exObj);

                    UiFloatWithSignal.Build(ref index, displayName, exObj, param.ValSpeed, param.LimitsType, param.MinVal, param.MaxVal, param.EditInDegrees, GetModel().Signals);
                }
                else if (param.Type == SdfParamType.Int)
                {
                    ExInt? exObj = paramsValuesCollection[param.ParameterKey] as ExInt;
                    if (exObj == null)
                        continue;
                    //int valI = exObj.Val;
                    //ImGui.PushID(index++);
                    //ImGui.Text(param.DisplayName);
                    //ImGui.PopID();
                    //ImGui.NextColumn();
                    //ImGui.SetNextItemWidth(-1);
                    //if (param.LimitsType == LimitsType.Min)
                    //{
                    //    ImGui.DragInt("##value", ref valI, param.ValSpeed, (int)param.MinVal);
                    //}
                    //else if (param.LimitsType == LimitsType.MinMax)
                    //{
                    //    ImGui.DragInt("##value", ref valI, param.ValSpeed, (int)param.MinVal, (int)param.MaxVal);
                    //}
                    //else // None
                    //{
                    //    ImGui.DragInt("##value", ref valI, param.ValSpeed);
                    //}
                    //ImGui.NextColumn();
                    //
                    ////paramsValuesCollection[param.ParameterName] = valI;
                    //exObj.Val = valI;
                    //
                    //UiInt.AddUndoHandler(param.DisplayName, exObj);

                    UiInt.Build(ref index, displayName, exObj, param.ValSpeed, param.LimitsType, (int)param.MinVal, (int)param.MaxVal);
                }
                else if (param.Type == SdfParamType.Vec2)
                {
                    ExVector2? exObj = paramsValuesCollection[param.ParameterKey] as ExVector2;
                    if (exObj == null)
                        continue;
                    Vector2 valV = exObj.Val;
                    if (param.EditInDegrees)
                        valV *= GMath.RadToDeg;
                    UiVector2.Build(ref index, displayName, ref valV, param.ValSpeed);
                    if (param.EditInDegrees)
                        valV *= GMath.DegToRad;
                    //paramsValuesCollection[param.ParameterName] = valV;
                    exObj.Val =  valV;

                    UiVector2.AddUndoHandler(displayName, exObj);
                }
                else if (param.Type == SdfParamType.Vec3)
                {
                    ExVector3? exObj = paramsValuesCollection[param.ParameterKey] as ExVector3;
                    if (exObj == null)
                        continue;
                    Vector3 valV = exObj.Val;
                    if (param.EditorType == ParamEditorType.Color)
                        UiColor3.Build(ref index, displayName, ref valV);
                    else
                    {
                        if (param.EditInDegrees)
                            valV *= GMath.RadToDeg;
                        UiVector3.Build(ref index, displayName, ref valV, param.ValSpeed);
                        if (param.EditInDegrees)
                            valV *= GMath.DegToRad;
                    }
                    //paramsValuesCollection[param.ParameterName] = valV;
                    exObj.Val = valV;

                    UiVector3.AddUndoHandler(displayName, exObj);
                }
                else if (param.Type == SdfParamType.Vec4)
                {
                    ExVector4? exObj = paramsValuesCollection[param.ParameterKey] as ExVector4;
                    if (exObj == null)
                        continue;
                    Vector4 valV = exObj.Val;
                    if (param.EditInDegrees)
                        valV *= GMath.RadToDeg;
                    UiVector4.Build(ref index, displayName, ref valV, param.ValSpeed);
                    if (param.EditInDegrees)
                        valV *= GMath.DegToRad;
                    //paramsValuesCollection[param.ParameterName] = valV;
                    exObj.Val = valV;

                    UiVector4.AddUndoHandler(displayName, exObj);
                }
                //else if (param.Type == SdfParamType.Bool)
                //{
                //    bool valB = (bool)paramsValuesCollection[param.ParameterName];
                //    UiBool.Build(index++, param.DisplayName, ref valB);
                //    paramsValuesCollection[param.ParameterName] = valB;
                //}
                else
                {
                    ImGui.PushID(index++);
                    ImGui.Text(param.DisplayName);
                    ImGui.NextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.Text("???");
                    ImGui.NextColumn();
                    ImGui.PopID();
                }
            }
        }
    }
}
