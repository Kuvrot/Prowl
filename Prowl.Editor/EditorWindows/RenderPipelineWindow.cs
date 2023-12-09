﻿using HexaEngine.ImGuiNET;
using Prowl.Editor.Drawers.NodeSystem;
using Prowl.Icons;
using Prowl.Runtime;
using Prowl.Runtime.ImGUI.Widgets;

namespace Prowl.Editor.EditorWindows;

public class RenderPipelineWindow : EditorWindow
{
    AssetRef<RenderPipeline> CurrentRenderPipeline;

    public RenderPipelineWindow() : base() => Title = FontAwesome6.CameraRetro + " Render Pipeline Editor";

    protected override void Draw()
    {
        if (!Project.HasProject) return;

        // Drag and drop support for the render pipeline asset
        var cStart = ImGui.GetCursorPos();
        ImGui.Dummy(ImGui.GetContentRegionAvail());
        if (DragnDrop.ReceiveAsset<ScriptableObject>(out var asset) && asset.Res is RenderPipeline rp)
            CurrentRenderPipeline = rp;
        ImGui.SetCursorPos(cStart);

        if (CurrentRenderPipeline.IsAvailable == false) return;

        bool changed = NodeSystemDrawer.Draw(CurrentRenderPipeline.Res);
    }
}