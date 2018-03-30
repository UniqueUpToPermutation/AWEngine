using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Xna = Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AWEngine
{
    class Renderer
    {
        protected GraphicsDevice device;

        public GraphicsDevice Device { get { return device; } }
        public Camera CurrentCamera { get; protected set; }

        public virtual void SetCamera(Camera camera)
        {
            CurrentCamera = camera;
        }

        public Renderer(GraphicsDevice device)
        {
            this.device = device;
            CurrentCamera = new Camera();
        }

        public static byte[] BitmapToByteArray(Bitmap bitmap, PixelFormat pix_format, bool bExtractChannel, int ChannelToExtract)
        {
            BitmapData bmpdata = null;
            byte[] bytedata = null;
            int numbytes = 0;

            try
            {
                bmpdata = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                numbytes = bmpdata.Stride * bitmap.Height;
                bytedata = new byte[numbytes];
                IntPtr ptr = bmpdata.Scan0;

                Marshal.Copy(ptr, bytedata, 0, numbytes);
            }
            finally
            {
                if (bmpdata != null)
                    bitmap.UnlockBits(bmpdata);
            }

            // Do some swizzling BR -> RB
            if (pix_format == PixelFormat.Format32bppArgb)
            {
                for (int i = 0; i < numbytes; i += 4)
                {
                    var tmp = bytedata[i];
                    bytedata[i] = bytedata[i + 2];
                    bytedata[i + 2] = tmp;
                }
            }

            if (bExtractChannel)
            {
                byte[] bytedata_ext = new byte[bytedata.Length / 4];
                for (int i = 0; i < bytedata_ext.Length; ++i)
                    bytedata_ext[i] = bytedata[(i << 2) + ChannelToExtract];
                return bytedata_ext;
            }
            else
            return bytedata;
        }

        public struct BitmapTileResult
        {
            public Bitmap Bitmap;
            public bool bPassThrough;
        }

        public static BitmapTileResult TileBitmap(Bitmap bitmap, int countX, int countY, PixelFormat pix_format)
        {
            if (!(pix_format == bitmap.PixelFormat && countX == 1 && countY == 1))
            {
                var result = new Bitmap(bitmap.Width * countX, bitmap.Height * countY, pix_format);
                using (Graphics gr = Graphics.FromImage(result))
                {
                    for (int y = 0; y < countY; ++y)
                        for (int x = 0; x < countX; ++x)
                            gr.DrawImage(bitmap, new System.Drawing.Rectangle(x * bitmap.Width,
                                y * bitmap.Height, bitmap.Width, bitmap.Height));
                }
                return new BitmapTileResult() { Bitmap = result, bPassThrough = false };
            }
            else
                return new BitmapTileResult() { Bitmap = bitmap, bPassThrough = true };
        }

        public static Texture3D ConvertBitmapArrayToTexture3D(GraphicsDevice device, IEnumerable<Bitmap> bitmaps,
            PixelFormat pix_format, SurfaceFormat surf_format)
        {
            var arrMaps = bitmaps.ToArray();
            var sizeXMax = arrMaps.Select(m => m.Width).Max();
            var sizeYMax = arrMaps.Select(m => m.Height).Max();
            if (arrMaps.Count(m => sizeXMax % m.Width != 0 && sizeYMax % m.Height != 0) > 0)
                throw new Exception("All bitmap dimensions must divide the largest dimension!");

            var tex = new Texture3D(device, sizeXMax, sizeYMax, arrMaps.Length, false, surf_format);

            bool bExtractAlpha = (surf_format == SurfaceFormat.Alpha8);

            var convertedBitmaps = arrMaps.Select(m => TileBitmap(m, sizeXMax / m.Width, sizeYMax / m.Height, pix_format)).ToArray();
            var rawDataFlat = convertedBitmaps.SelectMany(m => BitmapToByteArray(m.Bitmap, pix_format, bExtractAlpha, 3)).ToArray();

            foreach (var bitmap in convertedBitmaps)
                if (!bitmap.bPassThrough)
                    bitmap.Bitmap.Dispose();

            tex.SetData(rawDataFlat);

            return tex;
        }
    }
}
