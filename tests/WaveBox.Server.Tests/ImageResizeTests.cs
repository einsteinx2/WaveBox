using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using WaveBox.ApiHandler;
using Xunit;

namespace WaveBox.Server.Tests {
    public class ImageResizeTests {
        private static MemoryStream Png(int width, int height) {
            MemoryStream stream = new MemoryStream();
            using (Image<Rgba32> image = new Image<Rgba32>(width, height)) {
                image.SaveAsPng(stream);
            }
            stream.Position = 0;
            return stream;
        }

        [Fact]
        public void ResizeAspectFitsLandscapeIntoBox() {
            using (MemoryStream source = Png(100, 50))
            using (Stream resized = ArtStream.ResizeImage(source, 25, 0)) {
                using (Image result = Image.Load(resized)) {
                    // 100x50 into a 25x25 box: scale by 0.25 -> 25x12 (12.5 truncates)
                    Assert.Equal(25, result.Width);
                    Assert.Equal(12, result.Height);
                }
            }
        }

        [Fact]
        public void ResizePortraitConstrainedByHeight() {
            using (MemoryStream source = Png(40, 80))
            using (Stream resized = ArtStream.ResizeImage(source, 20, 0)) {
                using (Image result = Image.Load(resized)) {
                    Assert.Equal(10, result.Width);
                    Assert.Equal(20, result.Height);
                }
            }
        }

        [Fact]
        public void ResizeUpscalesSmallerImages() {
            using (MemoryStream source = Png(100, 50))
            using (Stream resized = ArtStream.ResizeImage(source, 200, 0)) {
                using (Image result = Image.Load(resized)) {
                    Assert.Equal(200, result.Width);
                    Assert.Equal(100, result.Height);
                }
            }
        }

        [Fact]
        public void ResizeAlwaysReencodesAsJpeg() {
            using (MemoryStream source = Png(10, 10))
            using (Stream resized = ArtStream.ResizeImage(source, 5, 0)) {
                IImageFormat format = Image.DetectFormat(resized);
                Assert.Equal("JPEG", format.Name);
            }
        }
    }
}
