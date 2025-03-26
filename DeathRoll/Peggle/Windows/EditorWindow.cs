using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using DeathRoll.Peggle.Objects;

namespace DeathRoll.Peggle.Windows;

public struct DragDropPeg
{
    public PegType Type;
    public Vector2 Position;

    public Vector2? Velocity;
    public float? Radius;
}

public class EditorWindow : Window, IDisposable
{
    public DragDropPeg? DragDropSelection;

    public PegType CurrentSelection = PegType.Blue;

    public float VelocityX;
    public float VelocityY;

    public float Radius = 8.0f;

    public List<DragDropPeg> Pegs = [];

    public EditorWindow() : base("Editor###Peggle", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove)
    {

    }

    public void Dispose()
    {

    }

    public override unsafe void Draw()
    {
        var windowPos = ImGui.GetWindowPos();
        var drawlist = ImGui.GetWindowDrawList();

        if (DragDropSelection != null && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            using var source = ImRaii.DragDropSource(ImGuiDragDropFlags.None);
            if (source)
            {
                Plugin.Log.Information($"Mouse dragging from main");
                ImGui.SetDragDropPayload("DragDropPegType", nint.Zero, 0);

                var previewDrawlist = ImGui.GetWindowDrawList();
                var previewCursor = ImGui.GetCursorScreenPos();
                ImGui.Dummy(new Vector2(16.0f));

                previewCursor += new Vector2(8.0f);
                previewDrawlist.AddCircleFilled(previewCursor, 8.0f, DragDropSelection.Value.Type.ToColor());
            }
        }

        ImGuiHelpers.ScaledDummy(10.0f);

        ImGuiHelpers.ScaledIndent(10.0f);
        foreach (var type in Enum.GetValues<PegType>())
        {
            var cursor = ImGui.GetCursorScreenPos();

            drawlist.AddCircleFilled(cursor, 8.0f, type.ToColor());
            ImGui.Dummy(new Vector2(20.0f));

            if (Vector2.Distance(ImGui.GetMousePos(), cursor) <= 8.0f)
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    CurrentSelection = type;

                    VelocityX = 0.0f;
                    VelocityY = 0.0f;

                    Radius = 8.0f;
                }
                else if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    Plugin.Log.Information($"Setting Source");
                    DragDropSelection = new DragDropPeg
                    {
                        Type = type,
                        Position = new Vector2(0, 0),
                        Velocity = new Vector2(VelocityX, VelocityY),
                        Radius = Radius,
                    };
                }
            }

            ImGui.SameLine();
        }
        ImGuiHelpers.ScaledIndent(-10.0f);

        ImGuiHelpers.ScaledDummy(10.0f);

        // using (var childBackground = ImRaii.PushColor(ImGuiCol.ChildBg, Helper.MapGrey))
        using (var child = ImRaii.Child("##GameField",new Vector2(Settings.HalfWidth, Settings.HalfHeight), true))
        {
            if (!child.Success)
                return;

            var begin = ImGui.GetCursorPos();
            foreach (var peg in Pegs)
            {
                drawlist.AddCircleFilled(peg.Position + windowPos, peg.Radius!.Value, peg.Type.ToColor());
                if (Vector2.Distance(ImGui.GetMousePos(), peg.Position + windowPos) <= peg.Radius!.Value)
                {
                    if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    {
                        Plugin.Log.Information($"Setting Source from existing");
                        DragDropSelection = peg;
                    }
                }
            }

            ImGui.SetCursorPos(begin);
            ImGui.Dummy(new Vector2(300, 300));
            if (ImGui.IsItemHovered())
            {
                using var target = ImRaii.DragDropTarget();
                if (target.Success)
                {
                    Plugin.Log.Information($"Dropping");
                    var payload = ImGui.AcceptDragDropPayload("DragDropPegType", ImGuiDragDropFlags.SourceExtern);
                    if (payload.NativePtr != null && DragDropSelection != null)
                    {
                        var peg = DragDropSelection.Value;
                        peg.Position = ImGui.GetIO().MousePos - windowPos;

                        if (!peg.Velocity.HasValue)
                        {
                            peg.Velocity = new Vector2(VelocityX, VelocityY);
                            peg.Radius = Radius;
                        }
                        Pegs.Add(peg);
                        DragDropSelection = null;

                        Plugin.Log.Information($"Placing Peg at {peg.Position.X} {peg.Position.Y}");
                    }
                }
            }
        }

        ImGui.SameLine();

        // using (var childBackground = ImRaii.PushColor(ImGuiCol.ChildBg, Helper.MapGrey))
        using (var child = ImRaii.Child("##InputField",new Vector2(200, Settings.HalfHeight), true))
        {
            if (!child.Success)
                return;

            ImGui.TextColored(ImGuiColors.DalamudOrange, $"{Enum.GetName(CurrentSelection)}");
            ImGui.InputFloat("Velocity X", ref VelocityX, 10);
            ImGui.InputFloat("Velocity Y", ref VelocityY, 10);

            ImGui.InputFloat("Radius", ref Radius, 1);
        }
    }
}