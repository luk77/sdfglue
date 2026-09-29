//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using SingleDocAppCore.Utils;

namespace SingleDocAppFramework.Ui.Properties
{
    public class UiFloat
    {
        public static void AddUndoHandler(string name, ExFloat obj)
        {
            UndoEditTracker.HandleLastItem(() => !obj.PrevVal.Equals(obj.Val), () => new ActionFloat(obj));
        }

        public static void Build(ref int id, string name, ExFloat obj, float speed, bool editInDegrees = false)
        {
            Build(ref id, name, obj, speed, LimitsType.None, 0.0f, 0.0f, editInDegrees);
        }

        //public static void Build(ref int id, string name, ExFloat obj, float speed, float minVal)
        //{
        //    Build(ref id, name, obj, speed, LimitsType.Min, minVal, 0.0f);
        //}

        //public static void Build(ref int id, string name, ExFloat obj, float speed, float minVal, float maxVal)
        //{
        //    Build(ref id, name, obj, speed, LimitsType.MinMax, minVal, maxVal);
        //}

        public static void Build(ref int id, string name, ExFloat obj, float speed, LimitsType limitsType, float minVal, float maxVal, bool editInDegrees)
        {
            float valF = obj.Val;
            if (editInDegrees)
                valF *= GMath.RadToDeg;

            //Build(id, name, ref valF, speed, limitsType, minVal, maxVal);

            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);

            ImGui.PushID(id++);
            switch(limitsType)
            {
                case LimitsType.None:       ImGui.DragFloat("##value", ref valF, speed);                     break;
                case LimitsType.Min:        ImGui.DragFloat("##value", ref valF, speed, minVal);             break;
                case LimitsType.MinMax:     ImGui.DragFloat("##value", ref valF, speed, minVal, maxVal);     break;
            }
            ImGui.PopID();

            ImGui.NextColumn();

            if (editInDegrees)
                valF *= GMath.DegToRad;

            obj.Val = valF;

            AddUndoHandler(name, obj);
        }

        public static bool Build(ref int id, string name, ref float val, float speed)
        {
            return Build(ref id, name, ref val, speed, LimitsType.None, 0.0f, 0.0f);
        }

        //public static void Build(ref int id, string name, ref float val, float speed, float minVal)
        //{
        //    Build(ref id, name, ref val, speed, LimitsType.Min, minVal, 0.0f);
        //}

        // Float without undo support (e.g. camera settings)
        public static bool Build(ref int id, string name, ref float val, float speed, float minVal, float maxVal)
        {
            return Build(ref id, name, ref val, speed, LimitsType.MinMax, minVal, maxVal);
        }

        public static bool Build(ref int id, string name, ref float val, float speed, LimitsType limitsType, float minVal, float maxVal)
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
                case LimitsType.None:       ret = ImGui.DragFloat("##value", ref val, speed);                     break;
                case LimitsType.Min:        ret = ImGui.DragFloat("##value", ref val, speed, minVal);             break;
                case LimitsType.MinMax:     ret = ImGui.DragFloat("##value", ref val, speed, minVal, maxVal);     break;
            }
            ImGui.PopID();
            ImGui.NextColumn();

            return ret;
        }
    }
}
