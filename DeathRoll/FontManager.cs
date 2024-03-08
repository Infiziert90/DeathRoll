using System.IO;

namespace DeathRoll;

public class FontManager
{
    public ImFontPtr Jetbrains22;
    public ImFontPtr SourceCode20;
    public ImFontPtr SourceCode36;

    public ImFontPtr AmazDooMLeft;
    public ImFontPtr AmazDooMRight;

    private ImVector Ranges;
    private ImFontConfigPtr FontConfig;

    private unsafe void SetUpRanges()
    {
        ImVector BuildRange(IReadOnlyList<ushort>? chars, params IntPtr[] ranges)
        {
            var builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
            foreach (var range in ranges)
            {
                builder.AddRanges(range);
            }
            if (chars != null)
            {
                for (var i = 0; i < chars.Count; i += 2)
                {
                    if (chars[i] == 0)
                    {
                        break;
                    }

                    for (var j = (uint)chars[i]; j <= chars[i + 1]; j++)
                    {
                        builder.AddChar((ushort)j);
                    }
                }
            }

            // various symbols
            builder.AddText("♠♥♦♣─＼～┐│┌┘└ςΔΗΓ╲╱∨∧");
            builder.AddText("←→↑↓《》■※☀★★☆♥♡ヅツッシ☀☁☂℃℉°♀♂♠♣♦♣♧®©™€$£♯♭♪✓√◎◆◇♦■□〇●△▽▼▲‹›≤≥<«“”─＼～");

            var result = new ImVector();
            builder.BuildRanges(out result);
            builder.Destroy();

            return result;
        }

        var ranges = new List<IntPtr> {
            ImGui.GetIO().Fonts.GetGlyphRangesDefault(),
        };

        Ranges = BuildRange(null, ranges.ToArray());
        FontConfig = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig())
        {
            FontDataOwnedByAtlas = false,
        };
    }

    public void BuildFonts()
    {
        SetUpRanges();

        var jetbrains = Path.Combine(Plugin.PluginDir, @"Resources\Fonts\JetBrainsMono-Medium.ttf");
        var sourcecode = Path.Combine(Plugin.PluginDir, @"Resources\Fonts\SourceCodePro-Medium.ttf");
        var amazDoomL = Path.Combine(Plugin.PluginDir, @"Resources\Fonts\AmazDooMLeft.ttf");
        var amazDoomR = Path.Combine(Plugin.PluginDir, @"Resources\Fonts\AmazDooMRight.ttf");
        try
        {
            Jetbrains22 = ImGui.GetIO().Fonts.AddFontFromFileTTF(jetbrains, 22, FontConfig, Ranges.Data);
            SourceCode20 = ImGui.GetIO().Fonts.AddFontFromFileTTF(sourcecode, 20, FontConfig, Ranges.Data);
            SourceCode36 = ImGui.GetIO().Fonts.AddFontFromFileTTF(sourcecode, 36, FontConfig, Ranges.Data);

            AmazDooMLeft = ImGui.GetIO().Fonts.AddFontFromFileTTF(amazDoomL, 142, FontConfig, Ranges.Data);
            AmazDooMRight = ImGui.GetIO().Fonts.AddFontFromFileTTF(amazDoomR, 142, FontConfig, Ranges.Data);
        } catch (Exception e) {
            Plugin.Log.Error(e, $"Font failed to load.");
        }
    }
}