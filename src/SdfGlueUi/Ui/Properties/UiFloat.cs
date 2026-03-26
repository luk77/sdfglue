//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes.Signals;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.UndoSystem.Actions;
using SdfGlueCore.Utils;

namespace SdfGlueUi.Ui.Properties
{
    public class UiFloat
    {
        public static void AddUndoHandler(string name, ExFloat obj)
        {
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                //Console.WriteLine("Undo save: {0}: {1}", name, obj.Val);
                UndoManager.Instance.SaveAction(new ActionFloat(obj));
            }
        }

        public static void Build(ref int id, string name, ExFloat obj, float speed, bool editInDegrees = false)
        {
            Build(ref id, name, obj, speed, SdfParamLimitsType.None, 0.0f, 0.0f, editInDegrees);
        }

        //public static void Build(ref int id, string name, ExFloat obj, float speed, float minVal)
        //{
        //    Build(ref id, name, obj, speed, SdfParamLimitsType.Min, minVal, 0.0f);
        //}

        //public static void Build(ref int id, string name, ExFloat obj, float speed, float minVal, float maxVal)
        //{
        //    Build(ref id, name, obj, speed, SdfParamLimitsType.MinMax, minVal, maxVal);
        //}

        public static void Build(ref int id, string name, ExFloat obj, float speed, SdfParamLimitsType limitsType, float minVal, float maxVal, bool editInDegrees, SignalsCollection signals = null)
        {
            float valF = obj.Val;
            if (editInDegrees)
                valF *= GMath.RadToDeg;

            //Build(id, name, ref valF, speed, limitsType, minVal, maxVal);

            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();

            SignalInstance signalRef = null;
            if (SdfGlueCore.Model.DataModel.UseSignals)
            {
                ExFloatWithSignal objWithSignal = obj as ExFloatWithSignal;
                if (objWithSignal != null && signals != null)
                {
                    SignalInstance oldInst = objWithSignal.SignalRef;

                    ImGui.PushID(id++);
                    if (ImGui.Button("*"))
                    {
                        ImGui.OpenPopup("menu_" + name);
                    }
                    ImGui.PopID();

                    objWithSignal.SignalRef = Components.MenuSignals.BuildPopup("menu_" + name, signals, objWithSignal.SignalRef);

                    signalRef = objWithSignal.SignalRef;

                    ImGui.SameLine();
                }
            }

            ImGui.SetNextItemWidth(-1);

            if (signalRef != null)
            {
                ImGui.PushID(id++);
                ImGui.Text(signalRef.Name.Val);
                ImGui.PopID();
            }
            else
            {
                ImGui.PushID(id++);
                switch(limitsType)
                {
                    case SdfParamLimitsType.None:       ImGui.DragFloat("##value", ref valF, speed);                     break;
                    case SdfParamLimitsType.Min:        ImGui.DragFloat("##value", ref valF, speed, minVal);             break;
                    case SdfParamLimitsType.MinMax:     ImGui.DragFloat("##value", ref valF, speed, minVal, maxVal);     break;
                }
                ImGui.PopID();
            }
            ImGui.NextColumn();

            if (editInDegrees)
                valF *= GMath.DegToRad;

            obj.Val = valF;

            AddUndoHandler(name, obj);
        }

        public static bool Build(ref int id, string name, ref float val, float speed)
        {
            return Build(ref id, name, ref val, speed, SdfParamLimitsType.None, 0.0f, 0.0f);
        }

        //public static void Build(ref int id, string name, ref float val, float speed, float minVal)
        //{
        //    Build(ref id, name, ref val, speed, SdfParamLimitsType.Min, minVal, 0.0f);
        //}

        // for camera settings
        public static bool Build(ref int id, string name, ref float val, float speed, float minVal, float maxVal)
        {
            return Build(ref id, name, ref val, speed, SdfParamLimitsType.MinMax, minVal, maxVal);
        }

        public static bool Build(ref int id, string name, ref float val, float speed, SdfParamLimitsType limitsType, float minVal, float maxVal)
        {
            bool ret = false;

            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            switch(limitsType)
            {
                case SdfParamLimitsType.None:       ret = ImGui.DragFloat("##value", ref val, speed);                     break;
                case SdfParamLimitsType.Min:        ret = ImGui.DragFloat("##value", ref val, speed, minVal);             break;
                case SdfParamLimitsType.MinMax:     ret = ImGui.DragFloat("##value", ref val, speed, minVal, maxVal);     break;
            }
            ImGui.PopID();
            ImGui.NextColumn();

            return ret;
        }
    }
}
