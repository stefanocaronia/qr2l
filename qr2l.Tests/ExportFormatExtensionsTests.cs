using System;
using qr2l.Core;
using Xunit;

namespace qr2l.Tests;

public class ExportFormatExtensionsTests
{
    [Theory]
    [InlineData(ExportFormat.Png, "png")]
    [InlineData(ExportFormat.Svg, "svg")]
    [InlineData(ExportFormat.Pdf, "pdf")]
    [InlineData(ExportFormat.Bmp, "bmp")]
    [InlineData(ExportFormat.Jpeg, "jpg")]
    [InlineData(ExportFormat.WebP, "webp")]
    [InlineData(ExportFormat.PostScript, "ps")]
    public void GetExtension_AllFormats_ShouldReturnCorrectExtension(ExportFormat format, string expectedExtension)
    {
        string result = format.GetExtension();

        Assert.Equal(expectedExtension, result);
    }

    [Fact]
    public void GetExtension_AllEnumValues_ShouldHaveExtension()
    {
        foreach (ExportFormat format in Enum.GetValues<ExportFormat>()) {
            string extension = format.GetExtension();

            Assert.NotNull(extension);
            Assert.NotEmpty(extension);
            Assert.DoesNotContain(".", extension);
        }
    }
}