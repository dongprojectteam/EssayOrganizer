using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EssayOrganizer.App.Utils;

public static class ImageOrientationHelper
{
    /// <summary>
    /// Reads an image from sourcePath, inspects EXIF orientation, applies any needed rotation,
    /// and saves a normalized upright PNG image to destPath.
    /// </summary>
    public static void SaveOrientedImage(string sourcePath, string destPath)
    {
        using var stream = File.OpenRead(sourcePath);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];

        var orientation = GetExifOrientation(frame.Metadata as BitmapMetadata);
        BitmapSource orientedSource = frame;

        var transform = GetTransformForOrientation(orientation);
        if (transform is not null)
        {
            orientedSource = new TransformedBitmap(frame, transform);
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(orientedSource));

        using var outStream = File.Create(destPath);
        encoder.Save(outStream);
    }

    /// <summary>
    /// Loads a BitmapSource from filePath with EXIF rotation automatically applied for display.
    /// </summary>
    public static BitmapSource LoadOrientedImage(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];

        var orientation = GetExifOrientation(frame.Metadata as BitmapMetadata);
        var transform = GetTransformForOrientation(orientation);

        BitmapSource result = frame;
        if (transform is not null)
        {
            result = new TransformedBitmap(frame, transform);
        }

        // Freeze to make it usable across threads and safe from file locks
        if (result.CanFreeze)
        {
            result.Freeze();
        }

        return result;
    }

    private static ushort GetExifOrientation(BitmapMetadata? metadata)
    {
        if (metadata is null) return 1;

        try
        {
            // Standard EXIF orientation query paths
            if (metadata.GetQuery("/app1/ifd/{ushort=274}") is ushort o1)
                return o1;
            if (metadata.GetQuery("/app1/ifd/exif/{ushort=274}") is ushort o2)
                return o2;
        }
        catch
        {
            // Ignored if query fails
        }

        return 1;
    }

    private static Transform? GetTransformForOrientation(ushort orientation)
    {
        return orientation switch
        {
            2 => new ScaleTransform(-1, 1), // Flip Horizontal
            3 => new RotateTransform(180),  // Rotate 180
            4 => new ScaleTransform(1, -1), // Flip Vertical
            5 => new TransformGroup { Children = { new RotateTransform(90), new ScaleTransform(-1, 1) } },
            6 => new RotateTransform(90),   // Rotate 90 CW (common smartphone portrait)
            7 => new TransformGroup { Children = { new RotateTransform(270), new ScaleTransform(-1, 1) } },
            8 => new RotateTransform(270),  // Rotate 270 CW (or 90 CCW)
            _ => null                       // Normal / 1
        };
    }
}
