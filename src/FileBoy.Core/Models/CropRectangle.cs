namespace FileBoy.Core.Models;

/// <summary>
/// Represents a rectangle in 2D space.
/// </summary>
public readonly struct CropRectangle
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }

    public CropRectangle(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public static CropRectangle Empty => new(0, 0, 0, 0);

    public bool IsEmpty => Width == 0 || Height == 0;
}
