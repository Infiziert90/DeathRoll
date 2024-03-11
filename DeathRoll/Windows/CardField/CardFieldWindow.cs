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

        if (!Plugin.Blackjack.Players.Any())
            return;

        ImGui.Text("Dealer: ");
        var orgCursor = ImGui.GetCursorPos();
        foreach (var card in Plugin.Blackjack.Dealer.Cards)
        {
            var cursor = ImGui.GetCursorPos();
            GameCardRender(card);
            ImGui.SetCursorPos(new Vector2(cursor.X + 110 * ImGuiHelpers.GlobalScale, cursor.Y));
        }

        var currentX = orgCursor.X;
        foreach (var (player, idx) in Plugin.Blackjack.Players.Select((var, i) => (var, i)))
        {
            if (idx != 0)
                currentX += 30 * ImGuiHelpers.GlobalScale;

            ImGui.SetCursorPos(new Vector2(currentX, orgCursor.Y + 250 * ImGuiHelpers.GlobalScale));
            ImGui.Text($"{player.DisplayName}: ");
            foreach (var card in player.Cards)
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
        Plugin.FontManager.Jetbrains22.Push();
        ImGui.Text(s[0]);
        Plugin.FontManager.Jetbrains22.Pop();

        ImGui.SameLine();

        var p = ImGui.GetCursorPos();
        ImGui.SetCursorPos(new Vector2(p.X - 70 * ImGuiHelpers.GlobalScale, p.Y + 100 * ImGuiHelpers.GlobalScale));
        Plugin.FontManager.SourceCode36.Push();
        ImGui.Text(s[1]);
        Plugin.FontManager.SourceCode36.Pop();
    }
}