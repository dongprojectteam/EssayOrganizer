namespace EssayOrganizer.App.Models;

public class FontSettings
{
    public const double MinSize = 10;
    public const double MaxSize = 28;
    public const double DefaultSize = 14;

    public double BodyFontSize { get; set; } = DefaultSize;

    public void Clamp()
    {
        BodyFontSize = Math.Clamp(BodyFontSize, MinSize, MaxSize);
    }
}

