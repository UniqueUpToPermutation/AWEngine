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

using System.Drawing.Imaging;

using AoWGraphics;

namespace AWEngine
{
    [StructLayout(LayoutKind.Sequential)]
    struct GridVertex : IVertexType
    {
        public Vector3 Position;
        public Color ByteData;
        public Vector2 MaskUV;
        public Color Aux1;
        public Color Aux2;

        public static readonly VertexDeclaration VertexDeclarationStatic = new VertexDeclaration(new VertexElement[] {
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 1),
            new VertexElement(28, VertexElementFormat.Color, VertexElementUsage.Color, 2)
        });

        public VertexDeclaration VertexDeclaration
        {
            get
            {
                return VertexDeclarationStatic;
            }
        }
    }

    class Camera
    {
        public Vector2 Position;
        public float Scale = 1f;
    }

    class WorldRenderer : Renderer
    {
        public const string GridEffectSrc = "Effects\\Grid";
        public const double BufferOversizeFactor = 1.5f;
        public const int TileVerts = 6;

        protected Texture3D groundTextureArray;
        protected Texture3D groundTransitionMaskArrayUR;
        protected Texture3D groundTransitionMaskArrayUC;
        protected Texture3D groundTransitionMaskArrayUL;

        protected Effect gridEffect;
        protected GridVertex[] gridVertexData;
        protected Vector3[] tileVertexOffsets = new Vector3[TileVerts + 1];
        protected Vector2[] tileVertexMaskUVs = new Vector2[TileVerts + 1];

        public Vector2 MaskOffset { get; set; }

        public int TransitionURCount
        {
            get { return groundTransitionMaskArrayUR.Depth; }
        }

        public int TransitionUCCount
        {
            get { return groundTransitionMaskArrayUC.Depth; }
        }

        public int TransitionULCount
        {
            get { return groundTransitionMaskArrayUL.Depth; }
        }

        public WorldRenderer(GraphicsDevice device) : base(device)
        {
        }

        public void LoadContent(ContentManager content)
        {
            gridEffect = content.Load<Effect>(GridEffectSrc);
        }

        public void CreateGroundTransitionArrays(IEnumerable<System.Drawing.Bitmap> bitmapsUR,
            IEnumerable<System.Drawing.Bitmap> bitmapsUC,
            IEnumerable<System.Drawing.Bitmap> bitmapsUL)
        {
            groundTransitionMaskArrayUR = ConvertBitmapArrayToTexture3D(device, bitmapsUR, PixelFormat.Format32bppArgb, SurfaceFormat.Alpha8);
            groundTransitionMaskArrayUC = ConvertBitmapArrayToTexture3D(device, bitmapsUC, PixelFormat.Format32bppArgb, SurfaceFormat.Alpha8);
            groundTransitionMaskArrayUL = ConvertBitmapArrayToTexture3D(device, bitmapsUL, PixelFormat.Format32bppArgb, SurfaceFormat.Alpha8);
        }

        public void CreateGroundTextureArray(IEnumerable<System.Drawing.Bitmap> bitmaps)
        {
            CreateGroundTextureArray(bitmaps, PixelFormat.Format32bppArgb, SurfaceFormat.Color);
        }

        public void CreateGroundTextureArray(IEnumerable<System.Drawing.Bitmap> bitmaps, PixelFormat pix_format, SurfaceFormat surf_format)
        {
            groundTextureArray = ConvertBitmapArrayToTexture3D(device, bitmaps, pix_format, surf_format);
        }

        public Rectangle GetVisibilityRect(Camera camera)
        {
            var ext = new Vector2(device.PresentationParameters.BackBufferWidth / 2,
                device.PresentationParameters.BackBufferHeight / 2);
            var lower = camera.Position - camera.Scale * ext;
            var upper = camera.Position + camera.Scale * ext;

            return new Rectangle((int)Math.Floor(lower.X),
                (int)Math.Floor(lower.Y),
                (int)Math.Ceiling(upper.X - lower.X),
                (int)Math.Ceiling(upper.Y - lower.Y));
        }

        public Matrix GetProjection(Camera camera)
        {
            var ext = new Vector2(device.PresentationParameters.BackBufferWidth / 2,
               device.PresentationParameters.BackBufferHeight / 2);
            var lower = -ext;
            var upper = ext;
            return Matrix.CreateOrthographicOffCenter(lower.X, upper.X, upper.Y, lower.Y, -1.0f, 1.0f);
        }

        public long BytesPerVertex
        {
            get
            {
                return 3 * sizeof(float) + 4 * sizeof(byte);
            }
        }

        public long BytesPerHex
        {
            get
            {
                return BytesPerVertex * VerticesPerHex;
            }
        }

        public long PrimitivesPerHex
        {
            get
            {
                return TileVerts;
            }
        }

        public long VerticesPerHex
        {
            get
            {
                return 3 * PrimitivesPerHex;
            }
        }

        public byte Pack(byte high, byte low)
        {
            return (byte)((high << 4) | (low & 0x0F));
        }

        public void RenderGrid(WorldMap map)
        {
            // Collect all the tiles we need to render
            var visRect = GetVisibilityRect(CurrentCamera);

            var tilesToRender = map.GetTilesInRectangle(visRect);
            var verticesPerHex = VerticesPerHex;
            var hexCount = tilesToRender.Count();
            var requiredLength = hexCount * verticesPerHex;

            if (hexCount > 0)
            {
                // Create new vertex data buffer if existing one isn't large enough
                if (gridVertexData == null || gridVertexData.LongLength < requiredLength)
                    gridVertexData = new GridVertex[(long)(requiredLength * BufferOversizeFactor)];

                // Compute offsets of the corner vertices of the tile
                var tileSize = map.TileSize;
                tileVertexOffsets[0] = Vector3.Zero;
                tileVertexOffsets[1] = new Vector3(tileSize, -tileSize, 0f);
                tileVertexOffsets[2] = new Vector3(2 * tileSize, -tileSize, 0f);
                tileVertexOffsets[3] = new Vector3(3 * tileSize, 0f, 0f);
                tileVertexOffsets[4] = new Vector3(2 * tileSize, tileSize, 0f);
                tileVertexOffsets[5] = new Vector3(tileSize, tileSize, 0f);
                tileVertexOffsets[6] = tileVertexOffsets[0];

                for (int i = 0; i < tileVertexOffsets.Length; ++i)
                {
                    tileVertexMaskUVs[i].X = ((tileVertexOffsets[i].X - 1.5f * tileSize) / (groundTransitionMaskArrayUR.Width / 2)) / 2f + 0.5f;
                    tileVertexMaskUVs[i].Y = ((tileVertexOffsets[i].Y) / (groundTransitionMaskArrayUR.Height / 2)) / 2f + 0.5f;
                }
                Vector2 centerMaskUV = new Vector2(.5f, .5f);
                Vector2 MaskUVOffsetLL = -new Vector2(-2f * tileSize / groundTransitionMaskArrayUR.Width, (float)tileSize / groundTransitionMaskArrayUR.Height);
                Vector2 MaskUVOffsetLC = -new Vector2(0, 2f * tileSize / groundTransitionMaskArrayUR.Height);
                Vector2 MaskUVOffsetLR = -new Vector2(2f * tileSize / groundTransitionMaskArrayUR.Width, (float)tileSize / groundTransitionMaskArrayUR.Height);

                // Magic Offsets
                MaskUVOffsetLL += MaskOffset;
                MaskUVOffsetLC += MaskOffset;
                MaskUVOffsetLR += MaskOffset;

                // Write hexes into the temporary vertex data buffer
                long vertLocation = 0;
                foreach (var tile in tilesToRender)
                {
                    var center = new Vector3(tile.Center, 0f);
                    var left = new Vector3(tile.Left, 0f);

                    var tileLL = tile.GetTileInDirection(WorldDirection.SouthWest);
                    var tileLC = tile.GetTileInDirection(WorldDirection.South);
                    var tileLR = tile.GetTileInDirection(WorldDirection.SouthEast);

                    var colorData = new Color(tile.Id, tileLL.Id, tileLC.Id, tileLR.Id);
                    var auxData1 = new Color(tileLC.TransitionUL, tileLC.TransitionUC, tileLC.TransitionUR,
                        (byte)0);
                    var auxData2 = new Color(tileLL.TransitionUC, tileLL.TransitionUR, 
                        tileLR.TransitionUC, tileLR.TransitionUL);

                    for (int iTriangle = 0; iTriangle < TileVerts; ++iTriangle)
                    {
                        var v1 = left + tileVertexOffsets[iTriangle + 1];
                        var v2 = left + tileVertexOffsets[iTriangle];
                        var v1_uv = tileVertexMaskUVs[iTriangle + 1];
                        var v2_uv = tileVertexMaskUVs[iTriangle];

                        gridVertexData[vertLocation].Position = v2;
                        gridVertexData[vertLocation].ByteData = colorData;
                        gridVertexData[vertLocation].MaskUV = v2_uv;
                        gridVertexData[vertLocation].Aux1 = auxData1;
                        gridVertexData[vertLocation].Aux2 = auxData2;

                        ++vertLocation;

                        gridVertexData[vertLocation].Position = v1;
                        gridVertexData[vertLocation].ByteData = colorData;
                        gridVertexData[vertLocation].MaskUV = v1_uv;
                        gridVertexData[vertLocation].Aux1 = auxData1;
                        gridVertexData[vertLocation].Aux2 = auxData2;

                        ++vertLocation;

                        gridVertexData[vertLocation].Position = center;
                        gridVertexData[vertLocation].ByteData = colorData;
                        gridVertexData[vertLocation].MaskUV = centerMaskUV;
                        gridVertexData[vertLocation].Aux1 = auxData1;
                        gridVertexData[vertLocation].Aux2 = auxData2;

                        ++vertLocation;
                    }
                }

                Matrix View = Matrix.CreateTranslation(new Vector3(-CurrentCamera.Position, 0f));
                Matrix Proj = GetProjection(CurrentCamera);
                View = Matrix.Transpose(View);

                gridEffect.Parameters["View"].SetValue(View);
                gridEffect.Parameters["Projection"].SetValue(Proj);
                gridEffect.Parameters["TextureSize"].SetValue(new Vector3(groundTextureArray.Width,
                    groundTextureArray.Height,
                    groundTextureArray.Depth));
                gridEffect.Parameters["TransMaskURCount"].SetValue((float)TransitionURCount);
                gridEffect.Parameters["TransMaskUCCount"].SetValue((float)TransitionUCCount);
                gridEffect.Parameters["TransMaskULCount"].SetValue((float)TransitionULCount);
                gridEffect.Parameters["GroundSampler"]?.SetValue(groundTextureArray);
                gridEffect.Parameters["TransMaskUR"]?.SetValue(groundTransitionMaskArrayUR);
                gridEffect.Parameters["TransMaskUC"]?.SetValue(groundTransitionMaskArrayUC);
                gridEffect.Parameters["TransMaskUL"]?.SetValue(groundTransitionMaskArrayUL);
                gridEffect.Parameters["MaskUVOffsetLL"]?.SetValue(MaskUVOffsetLL);
                gridEffect.Parameters["MaskUVOffsetLC"]?.SetValue(MaskUVOffsetLC);
                gridEffect.Parameters["MaskUVOffsetLR"]?.SetValue(MaskUVOffsetLR);
                gridEffect.CurrentTechnique.Passes[0].Apply();

                device.SamplerStates[0] = new SamplerState
                {
                    Filter = TextureFilter.Point,
                    AddressU = TextureAddressMode.Wrap,
                    AddressV = TextureAddressMode.Wrap,
                    AddressW = TextureAddressMode.Mirror
                };

                var primitivesPerHex = PrimitivesPerHex;
                device.DrawUserPrimitives(PrimitiveType.TriangleList, gridVertexData, 0, (int)(hexCount * primitivesPerHex));
            }
        }

        public void Render(WorldMap map)
        {
            RenderGrid(map);
        }
    }
}
