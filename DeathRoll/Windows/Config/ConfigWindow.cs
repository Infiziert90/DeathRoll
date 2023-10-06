using Dalamud.Interface.Windowing;

namespace DeathRoll.Windows.Config;

public partial class ConfigWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    // private RollTable RollTable;

    public ConfigWindow(Plugin plugin) : base("Configuration##DeathRoll")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(350, 530),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("##ConfigTabBar"))
        {
            General();

            Timer();

            Highlight();

            Blocklist();

            Debug();

            About();

            ImGui.EndTabBar();
        }
    }
}