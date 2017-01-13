using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WaveBox {
    public class ImageMagickInterop {
        public enum Filter {
            Undefined,
            Point,
            Box,
            Triangle,
            Hermite,
            Hanning,
            Hamming,
            Blackman,
            Gaussian,
            Quadratic,
            Cubic,
            Catrom,
            Mitchell,
            Lanczos,
            Bessel,
            Sinc,
            Kaiser,
            Welsh,
            Parzen,
            Lagrange,
            Bohman,
            Bartlett,
            SincFast
        };

        public enum InterpolatePixel {
            Undefined,
            Average,
            Bicubic,
            Bilinear,
            Filter,
            Integer,
            Mesh,
            NearestNeighbor,
            Spline
        };

        [DllImport("libMagickWand", EntryPoint = "MagickResizeImage")]
        public static extern bool ResizeImage(IntPtr mgck_wand, IntPtr columns, IntPtr rows, Filter filter_type, double blur);

        [DllImport("libMagickWand", EntryPoint = "MagickBlurImage")]
        public static extern bool BlurImage(IntPtr mgck_wand, double radius, double sigma);

        [DllImport("libMagickWand", EntryPoint = "MagickWandGenesis")]
        public static extern void WandGenesis();

        [DllImport("libMagickWand", EntryPoint = "MagickWandTerminus")]
        public static extern void WandTerminus();

        [DllImport("libMagickWand", EntryPoint = "NewMagickWand")]
        public static extern IntPtr NewWand();

        [DllImport("libMagickWand", EntryPoint = "DestroyMagickWand")]
        public static extern IntPtr DestroyWand(IntPtr wand);

        [DllImport("libMagickWand", EntryPoint = "MagickGetImageBlob")]
        public static extern IntPtr GetImageBlob(IntPtr wand, [Out] out IntPtr length);

        [DllImport("libMagickWand", EntryPoint = "MagickReadImageBlob")]
        public static extern bool ReadImageBlob(IntPtr wand, IntPtr blob, IntPtr length);

        [DllImport("libMagickWand", EntryPoint = "MagickRelinquishMemory")]
        public static extern IntPtr RelinquishMemory(IntPtr resource);

        [DllImport("libMagickWand", EntryPoint = "MagickGetImageWidth")]
        public static extern IntPtr GetWidth(IntPtr wand);

        [DllImport("libMagickWand", EntryPoint = "MagickGetImageHeight")]
        public static extern IntPtr GetHeight(IntPtr wand);

        [DllImport("libMagickWand", EntryPoint = "MagickGetException")]
        public static extern IntPtr GetException(IntPtr wand, IntPtr severity);

        [DllImport("libMagickWand", EntryPoint = "MagickGetExceptionType")]
        public static extern int GetExceptionType(IntPtr wand);

        // Interop
        public static bool ReadImageBlob(IntPtr wand, byte[] blob) {
            GCHandle pinnedArray = GCHandle.Alloc(blob, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();

            bool bRetv = ReadImageBlob(wand, pointer, (IntPtr)blob.Length);

            pinnedArray.Free();

            return bRetv;
        }

        // Interop
        public static byte[] GetImageBlob(IntPtr wand) {
            // Get the blob
            IntPtr len;
            IntPtr buf=GetImageBlob(wand, out len);

            // Copy it
            var dest = new byte[len.ToInt32()];
            Marshal.Copy(buf, dest, 0, len.ToInt32());

            // Relinquish
            RelinquishMemory(buf);

            return dest;
        }
    }
}
