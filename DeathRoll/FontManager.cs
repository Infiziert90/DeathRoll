using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dalamud.Logging;
using ImGuiNET;

namespace DeathRoll;

public static class FontManager
{
    public static ImFontPtr Font;
    public static ImFontPtr Font1;
    public static ImFontPtr Font2;
    
    static ImVector Ranges;
    static ImFontConfigPtr FontConfig;
    
    static unsafe void SetUpRanges()
    {
        static ImVector BuildRange(IReadOnlyList<ushort> chars, params IntPtr[] ranges)
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
    
    public static void BuildFonts()
    {
        SetUpRanges();
        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Fonts\\JetBrainsMono-Medium.ttf");
        var path1 = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Fonts\\SourceCodePro-Medium.ttf");
        try
        {
            Font = ImGui.GetIO().Fonts.AddFontFromFileTTF(path, 22, FontConfig, Ranges.Data);
            Font1 = ImGui.GetIO().Fonts.AddFontFromFileTTF(path1, 36, FontConfig, Ranges.Data);
            Font2 = ImGui.GetIO().Fonts.AddFontFromFileTTF(path1, 20, FontConfig, Ranges.Data);
        } catch (Exception ex) {
            PluginLog.Log($"Font failed to load. {path}");
            PluginLog.Log(ex.ToString());
        }
    }
}