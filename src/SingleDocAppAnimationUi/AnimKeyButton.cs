//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using System.Numerics;

namespace SingleDocAppAnimation.Ui
{
    // The key button of an animatable property (the timeline, the property panels of a host): a diamond, empty when the
    // property is static, orange when a track animates it, yellow with a key at the playhead
    public static class AnimKeyButton
    {
        public static void Draw(ImDrawListPtr dl, Vector2 c, float r, AnimKeyState state, AnimTimelineStyle? style = null)
        {
            style ??= AnimTimelineStyle.Default;
            Vector2 a = c + new Vector2(0, -r), b = c + new Vector2(r, 0), d = c + new Vector2(0, r), e = c + new Vector2(-r, 0);
            switch (state)
            {
                case AnimKeyState.None:
                    dl.AddQuad(a, b, d, e, style.KeyNone, 1.5f);
                    break;
                case AnimKeyState.Animated:
                    dl.AddQuadFilled(a, b, d, e, style.KeyAnimated);
                    dl.AddQuad(a, b, d, e, 0xFF000000, 1.0f);
                    break;
                default:
                    dl.AddQuadFilled(a, b, d, e, style.KeyOn);
                    dl.AddQuad(a, b, d, e, 0xFFFFFFFF, 1.5f);
                    break;
            }
        }

        // The button in front of the widget of a property in a panel (the cursor moves past it); true: clicked
        public static bool Button(string id, AnimKeyState state, string? tooltip = null, AnimTimelineStyle? style = null)
        {
            float h = ImGui.GetFrameHeight();
            Vector2 pos = ImGui.GetCursorScreenPos();
            bool clicked = ImGui.InvisibleButton(id, new Vector2(h * 0.8f, h));
            Draw(ImGui.GetWindowDrawList(), pos + new Vector2(h * 0.4f, h * 0.5f), h * 0.25f, state, style);
            if (tooltip != null && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
            return clicked;
        }

        // The frames of the widgets of an animated property are colored (green: animated, yellow: a key here) until
        // PopColors with what this returns
        public static int PushColors(AnimKeyState state, AnimTimelineStyle? style = null)
        {
            if (state == AnimKeyState.None)
                return 0;
            style ??= AnimTimelineStyle.Default;
            uint col = state == AnimKeyState.OnKey ? style.FrameOnKey : style.FrameAnimated;
            ImGui.PushStyleColor(ImGuiCol.FrameBg, col);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, col + 0x00101010);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, col + 0x00202020);
            return 3;
        }

        public static void PopColors(int pushed)
        {
            if (pushed > 0)
                ImGui.PopStyleColor(pushed);
        }
    }
}
