//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueUi.Ui.Properties;
using System.Numerics;
using SdfGlueCore.Utils;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.Entities;

namespace SdfGlueUi.Ui
{
    public abstract class UiWindowBase
    {
        public      bool                    IsVisible               = true;
        public      bool                    IsFocused               = false;
        public      bool                    IsHovered               = false;
        protected   UiManager               uiMgr_                  = null;
        //protected   DataModel               model_                  = null;   // nie chcemy przechowywać tej instancji, bo ona sięzmienia przy new/open project

        protected delegate void BuildContent();

        //public delegate void OnValueChanged();

        public abstract string Title { get; }

        public void SetUiManager(UiManager uiMgr)
        {
            uiMgr_ = uiMgr;
            //model_ = uiMgr.ActionsExecutor.GetModel();
        }

        public DataModel GetModel()
        {
            return uiMgr_.ActionsExecutor.GetModel();
        }

        protected void BuildWindow(int sizeX, int sizeY, BuildContent buildContent)
        {
            BuildWindow(0, 0, sizeX, sizeY, buildContent);
        }

        protected void BuildWindow(int posX, int posY, int sizeX, int sizeY, BuildContent buildContent)
        {
            BuildWindow(posX, posY, sizeX, sizeY, ImGuiWindowFlags.None, buildContent);
        }

        //private void BuildWindow(ref bool isShown, string title, int posX, int posY, int sizeX, int sizeY, BuildContent buildContent)
        protected void BuildWindow(int posX, int posY, int sizeX, int sizeY, ImGuiWindowFlags flags, BuildContent buildContent)
        {
            if (!IsVisible)
                return;

            IsFocused = false;

            ImGuiCond constraints = uiMgr_.AutoLayoutWindows ? ImGuiCond.Always : ImGuiCond.FirstUseEver;

            ImGui.SetNextWindowPos  (new Vector2(posX, posY), constraints);
            ImGui.SetNextWindowSize (new Vector2(sizeX,sizeY), constraints);
            ImGui.SetNextWindowSizeConstraints(new Vector2(340.0f, 100.0f), new Vector2(float.MaxValue, float.MaxValue));

            if (!ImGui.Begin(Title, ref IsVisible, flags))
            {
                ImGui.End();
                return;
            }

            IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootWindow);
            IsHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootWindow);

            // debug
            //ImGui.Text(string.Format("IsFocused: {0}", IsFocused));
            //ImGui.Text(string.Format("IsHovered: {0}", IsHovered));

            buildContent();

            ImGui.End();
        }

        public static void BeginPropertyGrid(float firstColumnWidth)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2,2));
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, firstColumnWidth);
            ImGui.Separator();
        }

        public static void EndPropertyGrid()
        {
            ImGui.Columns(1);
            ImGui.Separator();
            ImGui.PopStyleVar();
        }

        public abstract void Build();

        public virtual void HandleInput()
        {
        }

        // Helper to display a little (?) mark which shows a tooltip when hovered.
        // In your own code you may want to display an actual icon if you are using a merged icon fonts (see docs/FONTS.txt)
        public static void HelpMarker(string desc)
        {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted(desc);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        public static void BuildPropertiesForCompilationParameters(ref int index, FunctionEntity functionEntity, DataModel.OnValueChanged onValueChanged = null)
        {
            FunctionDefParametersCollection defParamsCollection     = functionEntity.Definition.CompilationParameters;
            ParametersValuesCollection      paramsValuesCollection  = functionEntity.CompilationParametersValues;

            foreach(FunctionDefParameter param in defParamsCollection)
            {
                // compilation params ignorują typ - to zawsze jest checkbox
                ExInt exObj = paramsValuesCollection[param.ParameterName] as ExInt;
                UiBool.Build(index++, param.DisplayName, exObj, onValueChanged );
            }
        }

        public void BuildPropertiesForParameters(ref int index, FunctionEntity functionEntity, DataModel.OnValueChanged onValueChanged = null)
        {
            FunctionDefParametersCollection defParamsCollection     = functionEntity.Definition.Parameters;
            ParametersValuesCollection      paramsValuesCollection  = functionEntity.ParametersValues;

            BuildPropertiesForFunctionDefinition(ref index, defParamsCollection, paramsValuesCollection, onValueChanged);
        }

        public void BuildPropertiesForMaterialParameters(ref int index, FunctionEntity functionEntity, DataModel.OnValueChanged onValueChanged = null)
        {
            FunctionDefParametersCollection defParamsCollection     = functionEntity.Definition.MaterialParameters;
            ParametersValuesCollection      paramsValuesCollection  = functionEntity.ParametersValues;
        
            BuildPropertiesForFunctionDefinition(ref index, defParamsCollection, paramsValuesCollection, onValueChanged);
        }


        //private void BuildPropertiesForSdf(ref int index, FunctionDefinition sdfDef, SdfObject sdfObj)
        public void BuildPropertiesForFunctionDefinition(ref int index, FunctionDefParametersCollection defParamsCollection , ParametersValuesCollection paramsValuesCollection, DataModel.OnValueChanged onValueChanged = null)
        {
            //FunctionDefParametersCollection defParamsCollection     = useCompilationParams ? functionEntity.Definition.CompilationParameters    : functionEntity.Definition.Parameters;
            //ParametersValuesCollection      paramsValuesCollection  = useCompilationParams ? functionEntity.CompilationParametersValues         : functionEntity.ParametersValues;

            foreach(FunctionDefParameter param in defParamsCollection)
            {
                if (!param.IsEditable())
                    continue;

//                if (useCompilationParams)
//                {
//                    // compilation params ignorują typ - to zawsze jest checkbox
//                    ExInt exObj = paramsValuesCollection[param.ParameterName] as ExInt;
//                    UiBool.Build(index++, param.DisplayName, exObj, onValueChanged );
//                }
//                else 
                if (param.Type == SdfParamType.Float)
                {
                    ExFloat exObj = paramsValuesCollection[param.ParameterName] as ExFloat;

                    //float valF = exObj.Val;
                    //if (param.EditInDegrees)
                    //    valF *= GMath.RadToDeg;
                    //
                    //ImGui.PushID(index++);
                    //ImGui.Text(param.DisplayName);
                    //ImGui.PopID();
                    //ImGui.NextColumn();
                    //ImGui.SetNextItemWidth(-1);
                    //if (param.LimitsType == SdfParamLimitsType.Min)
                    //{
                    //    ImGui.DragFloat("##value", ref valF, param.ValSpeed, param.MinVal);
                    //}
                    //else if (param.LimitsType == SdfParamLimitsType.MinMax)
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

                    UiFloat.Build(ref index, param.DisplayName, exObj, param.ValSpeed, param.LimitsType, param.MinVal, param.MaxVal, param.EditInDegrees, GetModel().Signals);
                }
                else if (param.Type == SdfParamType.Int)
                {
                    ExInt exObj = paramsValuesCollection[param.ParameterName] as ExInt;
                    //int valI = exObj.Val;
                    //ImGui.PushID(index++);
                    //ImGui.Text(param.DisplayName);
                    //ImGui.PopID();
                    //ImGui.NextColumn();
                    //ImGui.SetNextItemWidth(-1);
                    //if (param.LimitsType == SdfParamLimitsType.Min)
                    //{
                    //    ImGui.DragInt("##value", ref valI, param.ValSpeed, (int)param.MinVal);
                    //}
                    //else if (param.LimitsType == SdfParamLimitsType.MinMax)
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

                    UiInt.Build(ref index, param.DisplayName, exObj, param);
                }
                else if (param.Type == SdfParamType.Vec2)
                {
                    ExVector2 exObj = paramsValuesCollection[param.ParameterName] as ExVector2;
                    Vector2 valV = exObj.Val;
                    if (param.EditInDegrees)
                        valV *= GMath.RadToDeg;
                    UiVector2.Build(ref index, param.DisplayName, ref valV, param.ValSpeed);
                    if (param.EditInDegrees)
                        valV *= GMath.DegToRad;
                    //paramsValuesCollection[param.ParameterName] = valV;
                    exObj.Val =  valV;

                    UiVector2.AddUndoHandler(param.DisplayName, exObj);
                }
                else if (param.Type == SdfParamType.Vec3)
                {
                    ExVector3 exObj = paramsValuesCollection[param.ParameterName] as ExVector3;
                    Vector3 valV = exObj.Val;
                    if (param.EditorType == ParamEditorType.Color)
                        UiColor3.Build(ref index, param.DisplayName, ref valV);
                    else
                    {
                        if (param.EditInDegrees)
                            valV *= GMath.RadToDeg;
                        UiVector3.Build(ref index, param.DisplayName, ref valV, param.ValSpeed);
                        if (param.EditInDegrees)
                            valV *= GMath.DegToRad;
                    }
                    //paramsValuesCollection[param.ParameterName] = valV;
                    exObj.Val = valV;

                    UiVector3.AddUndoHandler(param.DisplayName, exObj);
                }
                else if (param.Type == SdfParamType.Vec4)
                {
                    ExVector4 exObj = paramsValuesCollection[param.ParameterName] as ExVector4;
                    Vector4 valV = exObj.Val;
                    if (param.EditInDegrees)
                        valV *= GMath.RadToDeg;
                    UiVector4.Build(ref index, param.DisplayName, ref valV, param.ValSpeed);
                    if (param.EditInDegrees)
                        valV *= GMath.DegToRad;
                    //paramsValuesCollection[param.ParameterName] = valV;
                    exObj.Val = valV;

                    UiVector4.AddUndoHandler(param.DisplayName, exObj);
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

        public float GetDefaultFirstColumnWidth()
        {
            //float bonus = UiManager.GetWindowsScaling() - 1.0f;
            float bonus = uiMgr_.ActionsExecutor.GetWindowsScaling() - 1.0f;

            return 220.0f + bonus * 120.0f;
        }

    }
}
