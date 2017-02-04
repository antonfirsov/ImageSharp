namespace ImageSharp
{
    using System;

    public interface IPixel<TColor> : IPackedPixel, IEquatable<TColor>
        where TColor : struct, IPixel<TColor>, IEquatable<TColor>
    {
        
    }
}