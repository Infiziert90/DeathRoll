using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using DeathRoll.Data;

namespace DeathRoll.Windows.CardField;

public class CardFieldWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    public CardFieldWindow(Plugin plugin) : base("Card Field##DeathRoll")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 600),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (Plugin.State is GameState.NotRunning or GameState.Crash)
            return;

        if (Plugin.Participants.PList.Count == 0)
            return;

        ImGui.Text("Dealer: ");
        var orgCursor = ImGui.GetCursorPos();
        foreach (var card in Plugin.Participants.DealerCards.Select(x => x.Card))
        {
            var cursor = ImGui.GetCursorPos();
            GameCardRender(card);
            ImGui.SetCursorPos(new Vector2(cursor.X + 110 * ImGuiHelpers.GlobalScale, cursor.Y));
        }

        var currentX = orgCursor.X;
        foreach (var name in Plugin.Participants.PlayerNameList)
        {
            if (Plugin.Participants.PlayerNameList.First() != name)
                currentX += 30 * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPos(new Vector2(currentX, orgCursor.Y + 250 * ImGuiHelpers.GlobalScale));
            ImGui.Text($"{Plugin.Participants.FindPlayer(name).GetDisplayName()}: ");
            foreach (var card in Plugin.Participants.FindAll(name).Select(x => x.Card))
            {
                ImGui.SetCursorPos(new Vector2(currentX, orgCursor.Y + 280 * ImGuiHelpers.GlobalScale));
                GameCardRender(card);
                currentX += 110 * ImGuiHelpers.GlobalScale;
            }
        }
    }

    private void GameCardRender(Cards.Card card)
    {
        var s = Cards.ShowCard(card);
        ImGui.PushFont(Plugin.FontManager.Jetbrains22);
        ImGui.Text(s[0]);
        ImGui.PopFont();

        ImGui.SameLine();

        var p = ImGui.GetCursorPos();
        ImGui.SetCursorPos(new Vector2(p.X - 70 * ImGuiHelpers.GlobalScale, p.Y + 100 * ImGuiHelpers.GlobalScale));
        ImGui.PushFont(Plugin.FontManager.SourceCode36);
        ImGui.Text(s[1]);
        ImGui.PopFont();
    }
}