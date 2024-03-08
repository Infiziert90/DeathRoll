using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DeathRoll.Bahamood.TextureHandler;

public static class TextureUtils
{
    public static byte[] ImageToRaw(this Image<Rgba32> image)
    {
        var data = new byte[4 * image.Width * image.Height];
        image.CopyPixelDataTo(data);
        return data;
    }
}