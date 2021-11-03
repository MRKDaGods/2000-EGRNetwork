using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace MRK {
    public class MRKImageEncoder {
        public static MemoryStream EncodeImageWithQuality(byte[] rawBytes, int w, int h) {
            try {
                using Image img = Image.Load(rawBytes);
                ResizeOptions resizeOptions = new() {
                    Sampler = KnownResamplers.Lanczos3,
                    Size = new Size(w, h),
                    Compand = true,
                    Mode = ResizeMode.Stretch
                };

                img.Mutate(op => {
                    op.Resize(resizeOptions);
                });

                //img.SaveAsPng(outPath, new SixLabors.ImageSharp.Formats.Png.PngEncoder {
                //    CompressionLevel = SixLabors.ImageSharp.Formats.Png.PngCompressionLevel.BestCompression
                //});

                MemoryStream stream = new();
                img.SaveAsJpeg(stream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder {
                    Quality = 30
                });

                return stream;
            }
            catch {
                return null;
            }
        }
    }
}
