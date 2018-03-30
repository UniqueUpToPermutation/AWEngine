using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Xna = Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

using AoWGraphics;

namespace AWEngine
{
    class WorldRendererFactory
    {
        private AoW2ImageLibrary lib = new AoW2ImageLibrary();

        public const string groundTexturesSrc = "E:\\SteamLibrary\\SteamApps\\common\\Age of Wonders Shadow Magic\\Images\\WMap\\WM_Text.ILB";
        public const string groundTransitionSrc = "E:\\SteamLibrary\\SteamApps\\common\\Age of Wonders Shadow Magic\\Images\\WMap\\WM_Trans.ilb";

        public void LoadGroundTextures(WorldRenderer renderer)
        {
            var filename = groundTexturesSrc;
            var ims = new List<AoWBitmap>();
            lib.OpenIlb(filename, ims);

            var rng = ims.GetRange(0, ims.FindIndex(m => m == null));
            renderer.CreateGroundTextureArray(from m in rng select m.Original, System.Drawing.Imaging.PixelFormat.Format16bppRgb565, SurfaceFormat.Bgr565);
            foreach (var im in ims)
                im?.Dispose();
        }

        public void LoadGroundTransitionTextures(WorldRenderer renderer)
        {
            var filename = groundTransitionSrc;
            var ims = new List<AoWBitmap>();
            lib.OpenIlb(filename, ims);

            var rng = ims.GetRange(0, ims.FindIndex(m => m == null));
            var subsection = rng.Count / 3;
            var rngUR = from m in rng.GetRange(subsection * 2, subsection) select m.Original;
            var rngUC = from m in rng.GetRange(subsection, subsection) select m.Original;
            var rngUL = from m in rng.GetRange(0, subsection) select m.Original;

            renderer.CreateGroundTransitionArrays(rngUR, rngUC, rngUL);
            foreach (var im in ims)
                im?.Dispose();
        }

        public WorldRenderer CreateRenderer(GraphicsDevice device, ContentManager content)
        {
            WorldRenderer renderer = new WorldRenderer(device);
            renderer.LoadContent(content);

            LoadGroundTextures(renderer);
            LoadGroundTransitionTextures(renderer);

            return renderer;
        }
    }
}
