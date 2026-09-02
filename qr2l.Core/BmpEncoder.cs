using System.Buffers.Binary;
using SkiaSharp;

namespace qr2l.Core;

/// <summary>
/// Scrittore BMP a 24 bit: Skia non codifica questo formato, ma è abbastanza semplice da produrre a mano.
/// </summary>
internal static class BmpEncoder
{
    private const int FileHeaderSize = 14;
    private const int InfoHeaderSize = 40;
    private const int BytesPerPixel = 3;
    private const int PixelsPerMeter = 2835; // 72 dpi

    public static byte[] Encode(SKBitmap source)
    {
        // Le righe vengono lette come BGRA: si normalizza il formato se il bitmap fosse diverso
        using SKBitmap bitmap = source.ColorType == SKColorType.Bgra8888
            ? source.Copy()
            : source.Copy(SKColorType.Bgra8888);

        int width = bitmap.Width;
        int height = bitmap.Height;
        int rowBytes = width * BytesPerPixel;
        int stride = (rowBytes + 3) & ~3;   // ogni riga è allineata a 4 byte
        int pixelBytes = stride * height;
        int headerSize = FileHeaderSize + InfoHeaderSize;

        var output = new byte[headerSize + pixelBytes];
        Span<byte> span = output;

        // File header
        span[0] = (byte)'B';
        span[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(span[2..], output.Length);
        BinaryPrimitives.WriteInt32LittleEndian(span[10..], headerSize);

        // Info header (BITMAPINFOHEADER)
        BinaryPrimitives.WriteInt32LittleEndian(span[14..], InfoHeaderSize);
        BinaryPrimitives.WriteInt32LittleEndian(span[18..], width);
        BinaryPrimitives.WriteInt32LittleEndian(span[22..], height);   // positivo: righe dal basso verso l'alto
        BinaryPrimitives.WriteInt16LittleEndian(span[26..], 1);        // piani
        BinaryPrimitives.WriteInt16LittleEndian(span[28..], 24);       // bit per pixel
        BinaryPrimitives.WriteInt32LittleEndian(span[34..], pixelBytes);
        BinaryPrimitives.WriteInt32LittleEndian(span[38..], PixelsPerMeter);
        BinaryPrimitives.WriteInt32LittleEndian(span[42..], PixelsPerMeter);

        ReadOnlySpan<byte> pixels = bitmap.GetPixelSpan();
        int sourceStride = bitmap.RowBytes;

        for (var y = 0; y < height; y++) {
            ReadOnlySpan<byte> sourceRow = pixels.Slice((height - 1 - y) * sourceStride, width * 4);
            Span<byte> targetRow = span.Slice(headerSize + (y * stride), rowBytes);

            for (var x = 0; x < width; x++) {
                targetRow[x * BytesPerPixel] = sourceRow[x * 4];         // B
                targetRow[(x * BytesPerPixel) + 1] = sourceRow[(x * 4) + 1]; // G
                targetRow[(x * BytesPerPixel) + 2] = sourceRow[(x * 4) + 2]; // R
            }
        }

        return output;
    }
}
