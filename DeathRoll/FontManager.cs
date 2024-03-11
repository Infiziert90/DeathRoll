using System.IO;
using Dalamud.Interface.ManagedFontAtlas;

namespace DeathRoll;

public class FontManager
{
    public readonly IFontHandle Jetbrains22;
    public readonly IFontHandle SourceCode20;
    public readonly IFontHandle SourceCode36;

    public readonly IFontHandle AmazDooMLeft;
    public readonly IFontHandle AmazDooMRight;

    public FontManager()
    {
        var jetbrains = Path.Combine(Plugin.PluginDir, @"Resources\Fonts\JetBrainsMono-Medium.ttf");
        var sourcecode = Path.Combine(Plugin.PluginDir, @"Resources\Fonts\SourceCodePro-Medium.ttf");
        var amazDoomL = Path.Combine(Plugin.PluginDir, @"Resources\Fonts\AmazDooMLeft.ttf");
        var amazDoomR = Path.Combine(Plugin.PluginDir, @"Resources\Fonts\AmazDooMRight.ttf");

        var range = "♠♥♦♣─＼～┐│┌┘└ςΔΗΓ╲╱∨∧←→↑↓《》■※☀★★☆♥♡ヅツッシ☀☁☂℃℉°♀♂♠♣♦♣♧®©™€$£♯♭♪✓√◎◆◇♦■□〇●△▽▼▲‹›≤≥<«“”─＼～".ToGlyphRange();
        Jetbrains22 = Plugin.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
            e => e.OnPreBuild(
                tk => tk.AddFontFromFile(jetbrains, new SafeFontConfig { SizePx = 22, GlyphRanges = range })
            ));

        SourceCode20 = Plugin.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
            e => e.OnPreBuild(
                tk => tk.AddFontFromFile(sourcecode, new SafeFontConfig { SizePx = 20, GlyphRanges = range })
            ));

        SourceCode36 = Plugin.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
            e => e.OnPreBuild(
                tk => tk.AddFontFromFile(sourcecode, new SafeFontConfig { SizePx = 36, GlyphRanges = range })
            ));

        AmazDooMLeft = Plugin.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
            e => e.OnPreBuild(
                tk => tk.AddFontFromFile(amazDoomL, new SafeFontConfig { SizePx = 142, GlyphRanges = range })
            ));

        AmazDooMRight = Plugin.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
            e => e.OnPreBuild(
                tk => tk.AddFontFromFile(amazDoomR, new SafeFontConfig { SizePx = 142, GlyphRanges = range })
            ));
    }

    public void Dispose()
    {
        Jetbrains22.Dispose();
        SourceCode20.Dispose();
        SourceCode36.Dispose();

        AmazDooMLeft.Dispose();
        AmazDooMRight.Dispose();
    }
}