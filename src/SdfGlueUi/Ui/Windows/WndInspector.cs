//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueUi.Ui.Components;
using SdfGlueUi.Ui.Properties;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.UndoSystem.Actions;
using System.Numerics;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.Entities;
using SdfGlueCore.Model.DataNodes.Signals;

namespace SdfGlueUi.Ui.Windows
{
    public class WndInspector : UiWindowBase
    {
        public override string Title => "Details";

        public override void Build()
        {
            TreeNode selectedNode = GetModel().SelectedNode;

            int index = 1;

            BuildWindow(uiMgr_.LeftColPosX, uiMgr_.InspectorPosY, uiMgr_.LeftColWidth, uiMgr_.InspectorHeight, delegate()
            {
                if (selectedNode is DataModel)
                {
                    DataModel model = selectedNode as DataModel;
                    if (ImGui.CollapsingHeader("Project settings", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        BeginPropertyGrid(GetDefaultFirstColumnWidth());
                        UiBool.Build(ref index, "Fix all objects"         , model.ProjSettings.FixAllObjects   , uiMgr_.ActionsExecutor.OnRebuildShader);
                        UiBool.Build(ref index, "Use 4d (experimental)"   , model.ProjSettings.Use4d           , uiMgr_.ActionsExecutor.OnRebuildShader);
                        EndPropertyGrid();
                    }
                }
                else if (selectedNode is SdfObject)
                {
                    SdfObject selectedSdfNode = selectedNode as SdfObject;

                    ImGui.Separator();
                    ImGui.Text(selectedSdfNode.Name.Val);

                    BeginPropertyGrid(GetDefaultFirstColumnWidth());

                    int index = 1;

                    UiString.BuildReadonly  (ref index, "Id"          , selectedSdfNode.Id.ToString());
                    UiString.Build          (ref index, "Name"        , selectedSdfNode.Name);
                    UiBool.Build            (ref index, "Fixed"       , selectedSdfNode.IsFixed, uiMgr_.ActionsExecutor.OnRebuildShader);

                    ImGui.Separator();

                    bool isFixed = GetModel().ProjSettings.FixAllObjects.Val || selectedSdfNode.IsFixed.Val;
                    if (!isFixed)
                    {
                        UiBool.Build(ref index, "Use in distance func."     , selectedSdfNode.UseInDistanceFunction , uiMgr_.ActionsExecutor.OnRebuildShader);
                        UiBool.Build(ref index, "Use in materials func."    , selectedSdfNode.UseInMaterialsFunction, uiMgr_.ActionsExecutor.OnRebuildShader);
                        ImGui.Separator();

                        UiBool.Build(ref index, "Use Shape"     , selectedSdfNode.UseShape, uiMgr_.ActionsExecutor.OnRebuildShader);
                        if (selectedSdfNode.UseShape.Val)
                        {
                            UiFunctionDefinition.Build(uiMgr_.ActionsExecutor, ref index, "Shape definition", selectedSdfNode.FunctionSdf, GetModel().SdfDefinitions);
                            BuildPropertiesForParameters(ref index, selectedSdfNode.FunctionSdf);
                        }
                        ImGui.Separator();

                        UiFunctionDefinition.Build(uiMgr_.ActionsExecutor, ref index, "Mix operator", selectedSdfNode.FunctionMixOp, GetModel().MixOpDefinitions);
                        BuildPropertiesForParameters(ref index, selectedSdfNode.FunctionMixOp);

                        UiFloat.Build(ref index, "Blending"      , selectedSdfNode.BlendFactor, 0.01f, SdfParamLimitsType.None, 0.0f, 0.0f, false,  GetModel().Signals);
                    }
                    EndPropertyGrid();

                    if (!isFixed)
                    {
                        // Bugfix: to nie może być pod 'UseShape', bo 'material blending' z parenta 
                        // (nawet wyłączonego) ma wpływ na 'material blending' potomków.
                        //if (selectedSdfNode.UseShape)
                        //{
                            // Material
                            //index = GroupIdMaterial;
                            ImGui.PushID(index++);
                            if (ImGui.CollapsingHeader("Material", ImGuiTreeNodeFlags.DefaultOpen))
                            //if (ImGui.CollapsingHeader("Material", ref openedGroupMaterial_))
                            {
                                BeginPropertyGrid(GetDefaultFirstColumnWidth());

                                // Material Id
                                BuildPropertyMaterial(index++, "Material", selectedSdfNode);

                                UiFloat.Build(ref index, "Material Blending"      , selectedSdfNode.MaterialBlendFactor, 0.01f, SdfParamLimitsType.None, 0.0f, 0.0f, false,  GetModel().Signals);

                                EndPropertyGrid();
                            }
                            ImGui.PopID();
                        //}

                        //index = GroupIdPosOp;
                        BuildEditorForOperatorsCollection(ref index, "Position operators", selectedSdfNode.PositionOperators, GetModel().PositionOpDefinitions, selectedSdfNode.InsertNewPositionOp);
                        //index = GroupIdDistOp;
                        BuildEditorForOperatorsCollection(ref index, "Distance operators", selectedSdfNode.DistanceOperators, GetModel().DistanceOpDefinitions, selectedSdfNode.InsertNewDistanceOp);
                    }

                    //to jest bug!! ImGui.End();
                }
                else if (selectedNode is CameraData)
                {
                    EditCameraData.Build(ref GetModel().CameraDat, GetDefaultFirstColumnWidth());
                }
                else if (selectedNode is RenderingData)
                {
                    // Old "Scene Inspector" window
                    float firstColumnWidth = GetDefaultFirstColumnWidth();

                    if (ImGui.CollapsingHeader("Ray marching settings", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        BeginPropertyGrid(firstColumnWidth);

                        UiInt   .Build(ref index, "Max number of steps"   , ref GetModel().MarchingMaxSteps  , 0.1f);
                        UiFloat .Build(ref index, "Minimum distance"      , ref GetModel().MarchingMinDist   , 0.1f);
                        UiFloat .Build(ref index, "Maximum distance"      , ref GetModel().MarchingMaxDist    , 0.1f);

                        EndPropertyGrid();
                    }
                }
                else if (selectedNode is RenderPassData)
                {
                    int index = 0;

                    RenderPassData passData = selectedNode as RenderPassData;

                    ImGui.PushID(index++);
                    if (ImGui.CollapsingHeader("Common render pass parameters", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        BeginPropertyGrid(GetDefaultFirstColumnWidth());

                        // renderer
                        UiFunctionDefinition.Build(uiMgr_.ActionsExecutor, ref index, "Pass type", passData.RendererFunc, GetModel().Renderers);

                        UiString.Build(ref index, "Name"                 , passData.Name);

                        ImGui.Separator();

                        UiBool.Build(ref index, "Enabled"                 , passData.Enabled);
                        UiBool.Build(ref index, "Fixed"                   , passData.IsFixed, uiMgr_.ActionsExecutor.OnRebuildShader);

                        UiBool.Build(ref index, "Clear on every frame"    , passData.ClearOnEveryFrame);
                        UiColor3.Build(ref index, "Clear color"             , passData.ClearColor);
                        UiFloat.Build(ref index, "Clear opacity"           , passData.ClearOpacity, 0.01f);

                        UiBool.Build(ref index, "Use texture filtering"       , passData.UseTextureFiltering, uiMgr_.ActionsExecutor.OnRebuildPreviewTexture);

                        ImGui.Separator();

                        UiBool.Build(ref index, "Auto reset frame counter"    , passData.AutoResetFrameCounter);

                        EndPropertyGrid();
                    }
                    ImGui.PopID();

                    bool isFixed = GetModel().ProjSettings.FixAllObjects.Val || passData.IsFixed.Val;

                    // backdrop
                    if (!isFixed)
                    {
                        ImGui.PushID(index++);
                        if (ImGui.CollapsingHeader(string.Format("Backdrop"), ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            BeginPropertyGrid(GetDefaultFirstColumnWidth());

                            UiFunctionDefinition.Build(uiMgr_.ActionsExecutor, ref index, "Backdrop type", passData.BackdropFunc, GetModel().BackdropsDefinitions);

                            if (passData.BackdropFunc.HasAnyParameters())
                            {
                                ImGui.Separator();
                                BuildPropertiesForParameters(ref index, passData.BackdropFunc);
                            }

                            EndPropertyGrid();
                        }
                        ImGui.PopID();
                    }


                    // camera controller
                    if (DataModel.UseCameraControllers)
                    {
                        if (passData.IsPrimaryPass)
                        {
                            BuildEditorForOperatorsCollection(ref index, "Camera operators", passData.CameraOperators, GetModel().CameraControllers, passData.InsertNewCameraOp);
                        }
                    }

                    if (passData.RendererFunc.HasAnyCompilationParameters())
                    {
                        ImGui.PushID(index++);
                        if (ImGui.CollapsingHeader(string.Format("'{0}' compilation parameters", passData.RendererFunc.Definition.DisplayName), ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            BeginPropertyGrid(GetDefaultFirstColumnWidth());

                            BuildPropertiesForCompilationParameters(ref index, passData.RendererFunc, uiMgr_.ActionsExecutor.OnRebuildShader);
                            //ImGui.Separator();

                            EndPropertyGrid();
                        }
                        ImGui.PopID();
                    }

                    if (!isFixed)
                    {
                        if (passData.RendererFunc.HasAnyParameters())
                        {
                            ImGui.PushID(index++);
                            if (ImGui.CollapsingHeader(string.Format("'{0}' realtime parameters", passData.RendererFunc.Definition.DisplayName), ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                BeginPropertyGrid(GetDefaultFirstColumnWidth());

                                BuildPropertiesForParameters(ref index, passData.RendererFunc);
                                //ImGui.Separator();

                                EndPropertyGrid();
                            }
                            ImGui.PopID();
                        }
                    }
                }
                else if (selectedNode is MaterialInstance)
                {
                    MaterialInstance mat = selectedNode as MaterialInstance;

                    float firstColumnWidth = GetDefaultFirstColumnWidth();

                    BeginPropertyGrid(firstColumnWidth);

                    int index = 0;

                    UiString.BuildReadonly(ref index, "Id"          , mat.Id.ToString());
                    UiString.Build(ref index, "Name"        , mat.Name);
                    UiBool.Build(ref index, "Fixed"       , mat.IsFixed, uiMgr_.ActionsExecutor.OnRebuildShader);

                    bool isFixed = GetModel().ProjSettings.FixAllObjects.Val || mat.IsFixed.Val;
                    if (!isFixed)
                    {
                        BuildPropertiesForMaterialParameters(ref index, mat.MaterialProps);
                    }

                    EndPropertyGrid();

                }
                else if (selectedNode is SignalOscillator)
                {
                    float firstColumnWidth = GetDefaultFirstColumnWidth();
                    BeginPropertyGrid(firstColumnWidth);

                    SignalOscillator? obj = selectedNode as SignalOscillator;
                    int index = 0;
                    UiFloat.Build(ref index, "Amplitude", obj.Amplitude   , 0.001f);
                    UiFloat.Build(ref index, "Frequency", obj.Frequency   , 0.001f);
                    UiFloat.Build(ref index, "Offset X" , obj.OffsetX     , 0.001f);
                    UiFloat.Build(ref index, "Offset Y" , obj.OffsetY     , 0.001f);

                    EndPropertyGrid();
                }
                else
                {
                    ImGui.Text("No data to edit.");
                }
            });
        }

        private void BuildEditorForOperatorsCollection(ref int index, string title, OperatorsCollection opCollection, FunctionDefinitionsSet opDefinitions, SdfObject.DelegateInsertNewOperator delegateInsertNewOp)
        {
            ImGui.PushID(index++);
            if (!ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopID(); // ensure balanced Pop on early return
                return;
            }

            int indexForInsert      = -1;
            int indexForDelete      = -1;
            int indexForMoveUp      = -1;
            int indexForMoveDown    = -1;

            BeginPropertyGrid(GetDefaultFirstColumnWidth());

            List<OperatorEntity> opList = opCollection.Operators;
            for(int i=0; i<opList.Count; i++)
            {
                OperatorEntity opent = opList[i];

                BuildPropertyChooseDefinitionOpEntity   (ref index, title, opent, opDefinitions, i,
                    ref indexForInsert, ref indexForDelete, ref indexForMoveUp, ref indexForMoveDown);

                if (opent.Enabled.Val)
                {
                    BuildPropertiesForParameters(ref index, opent);
                }

                ImGui.Separator();
            }

            // przycisk do dodawania elementów na końcu listy
            ImGui.PushID(index++);
            ImGui.NextColumn();
            if (ImGui.Button("Insert"))
            {
                indexForInsert = opList.Count;
            }
            ImGui.NextColumn();
            ImGui.PopID(); // balance the PushID before the Insert button

            // To musimy wywołać poza pętlą, ponieważ modyfikuje kolekcję
            if (indexForInsert != -1)
            {
                OperatorEntity newOpEnt = delegateInsertNewOp(indexForInsert);
                newOpEnt.RefreshDefinitionReference(opDefinitions);
                uiMgr_.ActionsExecutor.OnRebuildShader();
                // undo/redo support
                UndoManager.Instance.SaveAction(new ActionDelegates(
                    delegate
                    {
                        // undo
                        opList.RemoveAt(indexForInsert);
                        uiMgr_.ActionsExecutor.OnRebuildShader();
                    },
                    delegate
                    {
                        // redo
                        //tu nie można tworzyć nowej instancji, trzeba użyć istniejącej
                        opList.Insert(indexForInsert, newOpEnt);
                        //newOpEnt.RefreshDefinitionReference(opDefinitions);
                        uiMgr_.ActionsExecutor.OnRebuildShader();
                    }
                    ));

            }
            if (indexForDelete != -1)
            {
                OperatorEntity opDeleted = opList[indexForDelete];
                opList.RemoveAt(indexForDelete);
                uiMgr_.ActionsExecutor.OnRebuildShader();
                // undo/redo support
                UndoManager.Instance.SaveAction(new ActionDelegates(
                    delegate
                    {
                        // undo
                        opList.Insert(indexForDelete, opDeleted);
                        uiMgr_.ActionsExecutor.OnRebuildShader();
                    },
                    delegate
                    {
                        // redo
                        opList.RemoveAt(indexForDelete);
                        uiMgr_.ActionsExecutor.OnRebuildShader();
                    }
                    ));
            }
            if (indexForMoveUp >= 1)
            {
                OperatorEntity tempEnt = opList[indexForMoveUp];
                opList.RemoveAt(indexForMoveUp);
                opList.Insert(indexForMoveUp-1, tempEnt);
                uiMgr_.ActionsExecutor.OnRebuildShader();
                // undo/redo support
                UndoManager.Instance.SaveAction(new ActionDelegates(
                    delegate
                    {
                        // undo
                        opList.RemoveAt(indexForMoveUp-1);
                        opList.Insert(indexForMoveUp, tempEnt);
                        uiMgr_.ActionsExecutor.OnRebuildShader();
                    },
                    delegate
                    {
                        // redo
                        opList.RemoveAt(indexForMoveUp);
                        opList.Insert(indexForMoveUp-1, tempEnt);
                        uiMgr_.ActionsExecutor.OnRebuildShader();
                    }
                    ));
            }
            if (indexForMoveDown != -1 && indexForMoveDown < (opList.Count-1))
            {
                OperatorEntity tempEnt = opList[indexForMoveDown];
                opList.RemoveAt(indexForMoveDown);
                opList.Insert(indexForMoveDown+1, tempEnt);
                uiMgr_.ActionsExecutor.OnRebuildShader();
                // undo/redo support
                UndoManager.Instance.SaveAction(new ActionDelegates(
                    delegate
                    {
                        // undo
                        opList.RemoveAt(indexForMoveDown+1);
                        opList.Insert(indexForMoveDown, tempEnt);
                        uiMgr_.ActionsExecutor.OnRebuildShader();
                    },
                    delegate
                    {
                        // redo
                        opList.RemoveAt(indexForMoveDown);
                        opList.Insert(indexForMoveDown+1, tempEnt);
                        uiMgr_.ActionsExecutor.OnRebuildShader();
                    }
                    ));
            }

            EndPropertyGrid();
            ImGui.PopID(); // Pop the header id pushed at the start
        }

        public void BuildPropertyChooseDefinitionOpEntity(ref int id, string title, OperatorEntity opEntity, FunctionDefinitionsSet definitions, int entIndex, ref int indexForInsert, ref int indexForDelete, ref int indexForMoveUp, ref int indexForMoveDown)
        {
            // string[] defs = definitions.GetDefinitionsNames(); // unused

            // enabled
            UiBool.BuildSimpleCheckBoxWithUndo(ref id, opEntity.Enabled, uiMgr_.ActionsExecutor.OnRebuildShader);

            ImGui.SameLine();
            ImGui.PushID(id++);
            ImGui.SetNextItemWidth(-1);
            if (ImGui.Button(opEntity.Definition.DisplayName, new Vector2(-1, 0)))
            {
                ImGui.OpenPopup("menu_"+title);
            }
            FunctionDefinition fd = MenuFunctionDefinition.BuildPopup("menu_"+title, opEntity, definitions);
            if (fd != null)
            {
                opEntity.Definition       = fd;
                opEntity.DefinitionName.Val   = opEntity.Definition.FunctionName;
                uiMgr_.ActionsExecutor.OnRebuildShader();
                UiFunctionDefinition.AddFunctionEntityUndoAction(opEntity.Definition.DisplayName, opEntity, definitions, uiMgr_.ActionsExecutor);
            }
            ImGui.PopID(); // balanced the PushID used for the definition button

            ImGui.NextColumn();

            // menu
            ImGui.PushID(id++);
            if (ImGui.Button("..."))
            {
                ImGui.OpenPopup("op_entity_menu");
            }

            if (ImGui.BeginPopup("op_entity_menu"))
            {
                //BuildOperatorEntityMenu(opent);

                if (ImGui.MenuItem("Insert before"))        { indexForInsert    = entIndex;    }
                if (ImGui.MenuItem("Insert after"))         { indexForInsert    = entIndex+1;  }
                if (ImGui.MenuItem("Move Up"))              { indexForMoveUp    = entIndex;    }
                if (ImGui.MenuItem("Move Down"))            { indexForMoveDown  = entIndex;    }
                if (ImGui.MenuItem("Delete"))               { indexForDelete    = entIndex;    }
                //if (ImGui.MenuItem("Copy" , "", false, false)) {}
                //if (ImGui.MenuItem("Paste", "", false, false)) {}
                //if (ImGui.BeginMenu("Test"))
                //{
                //    if (ImGui.MenuItem("Test 1"       , "CTRL+1")) {}
                //    if (ImGui.MenuItem("Test 2"       , "CTRL+2")) {}
                //    ImGui.Separator();
                //    if (ImGui.MenuItem("Test 3"       , "CTRL+3")) {}
                //    if (ImGui.MenuItem("Test 4"       , "CTRL+4")) {}
                //}

                ImGui.EndPopup();
            }
            ImGui.PopID();

            ImGui.NextColumn();

        }

        private void BuildPropertyMaterial(int id, string name, SdfObject sdfObj)
        {
            // ComboBox w Imgui jest kiepski bo polega na indeksach i nazwach
            // Lepiej zastąpić go prostym menu.

            ImGui.PushID(id);
            ImGui.Text(name);
            ImGui.NextColumn();

            MaterialInstance oldMat = GetModel().FindMaterialById((int)sdfObj.MaterialId.Val);
            if (oldMat != null)
            {
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Button(oldMat.Name.Val, new Vector2(-1, 0)))
                {
                    ImGui.OpenPopup("menu_" + name);
                }

                MaterialInstance newMat = MenuMaterials.BuildPopup("menu_" + name, GetModel().Materials);
                ImGui.NextColumn();

                if (newMat != null && newMat != oldMat)
                {
                    sdfObj.MaterialId.Val = newMat.Id;
                    //Console.WriteLine("Undo save: {0}: {1}", name, sdfObj.MaterialId.Val);
                    UndoManager.Instance.SaveAction(new ActionFloat(sdfObj.MaterialId, uiMgr_.ActionsExecutor.OnRebuildShader));

                    uiMgr_.ActionsExecutor.OnRebuildShader();
                }
            }
            else
            {
                ImGui.NextColumn();
            }

            ImGui.PopID(); // balance push
        }
    }
}
