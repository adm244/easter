using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using K4os.Compression.LZ4;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace EasterLib.Files.PGF
{
    internal static class LZ4Helper
    {
        private static readonly string ImageSizeIsTooBig = "Couldn't get a contiguous buffer. Probably image size is too big.";

        public static Image DecodeImage(byte[] buffer, int width, int height, int length)
        {
            byte[] pixels = new byte[length];

            int written = LZ4Codec.Decode(buffer, pixels);
            Debug.Assert(written > 0);

            return Image.LoadPixelData<Rgba32>(pixels, width, height);
        }

        public static byte[] EncodeImage(Image<Rgba32> image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if (!image.TryGetSinglePixelSpan(out Span<Rgba32> span))
                throw new NotSupportedException(ImageSizeIsTooBig);

            Span<byte> pixels = MemoryMarshal.AsBytes(span);

            int worstCaseSize = pixels.Length + (int)(pixels.Length * 0.04 + 0.5);
            byte[] pixelsCompressed = new byte[worstCaseSize];

            int compressedLength = LZ4Codec.Encode(pixels, pixelsCompressed);
            Debug.Assert(compressedLength > 0);

            Array.Resize(ref pixelsCompressed, compressedLength);
            Debug.Assert(pixelsCompressed.Length == compressedLength);

            return pixelsCompressed;
        }
    }
}
