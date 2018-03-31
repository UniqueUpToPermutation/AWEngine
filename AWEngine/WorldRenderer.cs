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
    struct GridVertexOnePass : IVertexType
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

    [StructLayout(LayoutKind.Sequential)]
    struct GridVertexSimple : IVertexType
    {
        public Vector3 Position;
        public Color ByteData;

        public static readonly VertexDeclaration VertexDeclarationStatic = new VertexDeclaration(new VertexElement[] {
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        });

        public VertexDeclaration VertexDeclaration
        {
            get
            {
                return VertexDeclarationStatic;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct GridVertexSimpleAux : IVertexType
    {
        public Vector3 Position;
        public Color ByteData;
        public Vector2 MaskUV;
        public Color Aux1;

        public static readonly VertexDeclaration VertexDeclarationStatic = new VertexDeclaration(new VertexElement[] {
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 1),
        });

        public VertexDeclaration VertexDeclaration
        {
            get
            {
                return VertexDeclarationStatic;
            }
        }
    }

    public class TileComparer : IComparer<TileRef>
    {
        // Call CaseInsensitiveComparer.Compare with the parameters reversed.
        public int Compare(TileRef A, TileRef B)
        {
            var diff = A.LeftUnscaled.Y - B.LeftUnscaled.Y;
            if (diff == 0)
                return A.LeftUnscaled.X - B.LeftUnscaled.X;
            return diff;
        }
    }

    class Camera
    {
        public Vector2 Position;
        public float Scale = 1f;
    }

    struct TileTransitionDraw
    {
        public const byte DrawFlagUR = 0x1;
        public const byte DrawFlagUR_Special = 0x2;
        public const byte DrawFlagUC = 0x4;
        public const byte DrawFlagUC_Special = 0x8;
        public const byte DrawFlagUL = 0x10;
        public const byte DrawFlagUL_Special = 0x20;

        public TileRef BaseTile;
        public TileRef TileUR;
        public TileRef TileUC;
        public TileRef TileUL;
        public byte DrawFlags;
    }

    class WorldRenderer : Renderer
    {
        public const string GridEffectOnePassSrc = "Effects\\GridOnePass";
        public const string GridEffectSimpleSrc = "Effects\\GridSimple";
        public const double BufferOversizeFactor = 1.5f;
        public const int TileVerts = 6;

        protected Texture3D groundTextureArray;
        protected Texture3D groundTransitionMaskArrayUR;
        protected Texture3D groundTransitionMaskArrayUC;
        protected Texture3D groundTransitionMaskArrayUL;

        protected Effect gridOnePassEffect;
        protected Effect gridSimpleEffect;
        protected TileComparer tileComparer = new TileComparer();

        protected GridVertexOnePass[] gridVertexDataOnePass;
        protected GridVertexSimple[] gridVertexDataSimple;
        protected GridVertexSimpleAux[] gridVertexDataTransition;
        protected Vector3[] tileVertexOffsets = new Vector3[TileVerts + 1];
        protected Vector2[] tileVertexMaskUVs = new Vector2[TileVerts + 1];
        protected TileRef[] tileRefCache;
        protected TileTransitionDraw[] tileTransitionDrawCache;
        protected Vector3[] transitionHexPositionOffsets = new Vector3[4];
        protected byte[] transitionHexDrawFlags = new byte[4];
        protected TileRef[] tileRefsTemp = new TileRef[4];

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
            gridOnePassEffect = content.Load<Effect>(GridEffectOnePassSrc);
            gridSimpleEffect = content.Load<Effect>(GridEffectSimpleSrc);
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

        public void RenderGridOnePass(WorldMap map)
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
                if (gridVertexDataOnePass == null || gridVertexDataOnePass.LongLength < requiredLength)
                    gridVertexDataOnePass = new GridVertexOnePass[(long)(requiredLength * BufferOversizeFactor)];

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

                        gridVertexDataOnePass[vertLocation].Position = v2;
                        gridVertexDataOnePass[vertLocation].ByteData = colorData;
                        gridVertexDataOnePass[vertLocation].MaskUV = v2_uv;
                        gridVertexDataOnePass[vertLocation].Aux1 = auxData1;
                        gridVertexDataOnePass[vertLocation].Aux2 = auxData2;

                        ++vertLocation;

                        gridVertexDataOnePass[vertLocation].Position = v1;
                        gridVertexDataOnePass[vertLocation].ByteData = colorData;
                        gridVertexDataOnePass[vertLocation].MaskUV = v1_uv;
                        gridVertexDataOnePass[vertLocation].Aux1 = auxData1;
                        gridVertexDataOnePass[vertLocation].Aux2 = auxData2;

                        ++vertLocation;

                        gridVertexDataOnePass[vertLocation].Position = center;
                        gridVertexDataOnePass[vertLocation].ByteData = colorData;
                        gridVertexDataOnePass[vertLocation].MaskUV = centerMaskUV;
                        gridVertexDataOnePass[vertLocation].Aux1 = auxData1;
                        gridVertexDataOnePass[vertLocation].Aux2 = auxData2;

                        ++vertLocation;
                    }
                }

                Matrix View = Matrix.CreateTranslation(new Vector3(-CurrentCamera.Position, 0f));
                Matrix Proj = GetProjection(CurrentCamera);
                View = Matrix.Transpose(View);

                gridOnePassEffect.Parameters["View"].SetValue(View);
                gridOnePassEffect.Parameters["Projection"].SetValue(Proj);
                gridOnePassEffect.Parameters["TextureSize"].SetValue(new Vector3(groundTextureArray.Width,
                    groundTextureArray.Height,
                    groundTextureArray.Depth));
                gridOnePassEffect.Parameters["TransMaskURCount"].SetValue((float)TransitionURCount);
                gridOnePassEffect.Parameters["TransMaskUCCount"].SetValue((float)TransitionUCCount);
                gridOnePassEffect.Parameters["TransMaskULCount"].SetValue((float)TransitionULCount);
                gridOnePassEffect.Parameters["GroundSampler"]?.SetValue(groundTextureArray);
                gridOnePassEffect.Parameters["TransMaskUR"]?.SetValue(groundTransitionMaskArrayUR);
                gridOnePassEffect.Parameters["TransMaskUC"]?.SetValue(groundTransitionMaskArrayUC);
                gridOnePassEffect.Parameters["TransMaskUL"]?.SetValue(groundTransitionMaskArrayUL);
                gridOnePassEffect.Parameters["MaskUVOffsetLL"]?.SetValue(MaskUVOffsetLL);
                gridOnePassEffect.Parameters["MaskUVOffsetLC"]?.SetValue(MaskUVOffsetLC);
                gridOnePassEffect.Parameters["MaskUVOffsetLR"]?.SetValue(MaskUVOffsetLR);
                gridOnePassEffect.CurrentTechnique.Passes[0].Apply();

                device.SamplerStates[0] = new SamplerState
                {
                    Filter = TextureFilter.Point,
                    AddressU = TextureAddressMode.Wrap,
                    AddressV = TextureAddressMode.Wrap,
                    AddressW = TextureAddressMode.Mirror
                };

                var primitivesPerHex = PrimitivesPerHex;
                device.DrawUserPrimitives(PrimitiveType.TriangleList, gridVertexDataOnePass, 0, (int)(hexCount * primitivesPerHex));
            }
        }

        public void RenderGridSimple(WorldMap map)
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
                if (gridVertexDataSimple == null || gridVertexDataSimple.LongLength < requiredLength)
                    gridVertexDataSimple = new GridVertexSimple[(long)(requiredLength * BufferOversizeFactor)];
                if (tileRefCache == null || tileRefCache.LongLength < requiredLength)
                    tileRefCache = new TileRef[(long)(hexCount * BufferOversizeFactor)];
                if (tileTransitionDrawCache == null || tileTransitionDrawCache.LongLength < requiredLength)
                    tileTransitionDrawCache = new TileTransitionDraw[(long)(hexCount * BufferOversizeFactor)];

                // Copy tiles to render into buffer
                int i = 0;
                foreach (var tile in tilesToRender)
                {
                    tileRefCache[i] = tile;
                    ++i;
                }

                // Sort tiles by y-position
                Array.Sort(tileRefCache, 0, hexCount, tileComparer);

                // Compute tile transitions
                int tilesWithTransitionsCount = 0;
                int transitionCount = 0;
                for (i = 0; i < hexCount; ++i)
                {
                    var tile = tileRefCache[i];

                    var tileTransition = new TileTransitionDraw()
                    {
                        BaseTile = tile,
                        TileUR = tile.GetTileInDirection(WorldDirection.NorthEast),
                        TileUC = tile.GetTileInDirection(WorldDirection.North),
                        TileUL = tile.GetTileInDirection(WorldDirection.NorthWest),
                        DrawFlags = 0
                    };

                    if (tile.Id != tileTransition.TileUR.Id)
                        tileTransition.DrawFlags |= TileTransitionDraw.DrawFlagUR;
                    if (tile.Id != tileTransition.TileUC.Id)
                        tileTransition.DrawFlags |= TileTransitionDraw.DrawFlagUC;
                    if (tile.Id != tileTransition.TileUL.Id)
                        tileTransition.DrawFlags |= TileTransitionDraw.DrawFlagUL;

                    if (tileTransition.DrawFlags != 0)
                    {
                        tileTransitionDrawCache[tilesWithTransitionsCount] = tileTransition;
                        ++tilesWithTransitionsCount;
                        transitionCount += 3;
                    }
                }

                // Compute offsets of the corner vertices of the tile
                var tileSize = map.TileSize;
                tileVertexOffsets[0] = Vector3.Zero;
                tileVertexOffsets[1] = new Vector3(tileSize, -tileSize, 0f);
                tileVertexOffsets[2] = new Vector3(2 * tileSize, -tileSize, 0f);
                tileVertexOffsets[3] = new Vector3(3 * tileSize, 0f, 0f);
                tileVertexOffsets[4] = new Vector3(2 * tileSize, tileSize, 0f);
                tileVertexOffsets[5] = new Vector3(tileSize, tileSize, 0f);
                tileVertexOffsets[6] = tileVertexOffsets[0];

                for (i = 0; i < tileVertexOffsets.Length; ++i)
                {
                    tileVertexMaskUVs[i].X = ((tileVertexOffsets[i].X - 1.5f * tileSize) / (groundTransitionMaskArrayUR.Width / 2)) / 2f + 0.5f;
                    tileVertexMaskUVs[i].Y = ((tileVertexOffsets[i].Y) / (groundTransitionMaskArrayUR.Height / 2)) / 2f + 0.5f;
                }

                // Write hexes into the temporary vertex data buffer
                long vertLocation = 0;
                for (i = 0; i < hexCount; ++i)
                {
                    var tile = tileRefCache[i];

                    var center = new Vector3(tile.Center, 0f);
                    var left = new Vector3(tile.Left, 0f);

                    var colorData = new Color(tile.Id, (byte)0, (byte)0, (byte)0);

                    for (int iTriangle = 0; iTriangle < TileVerts; ++iTriangle)
                    {
                        var v1 = left + tileVertexOffsets[iTriangle + 1];
                        var v2 = left + tileVertexOffsets[iTriangle];

                        gridVertexDataSimple[vertLocation].Position = v2;
                        gridVertexDataSimple[vertLocation].ByteData = colorData;

                        ++vertLocation;

                        gridVertexDataSimple[vertLocation].Position = v1;
                        gridVertexDataSimple[vertLocation].ByteData = colorData;

                        ++vertLocation;

                        gridVertexDataSimple[vertLocation].Position = center;
                        gridVertexDataSimple[vertLocation].ByteData = colorData;

                        ++vertLocation;
                    }
                }

                Matrix View = Matrix.CreateTranslation(new Vector3(-CurrentCamera.Position, 0f));
                Matrix Proj = GetProjection(CurrentCamera);
                View = Matrix.Transpose(View);

                gridSimpleEffect.Parameters["View"].SetValue(View);
                gridSimpleEffect.Parameters["Projection"].SetValue(Proj);
                gridSimpleEffect.Parameters["TextureSize"].SetValue(new Vector3(groundTextureArray.Width,
                    groundTextureArray.Height,
                    groundTextureArray.Depth));
                gridSimpleEffect.Parameters["GroundSampler"]?.SetValue(groundTextureArray);
                gridSimpleEffect.Parameters["TransMaskURCount"].SetValue((float)TransitionURCount);
                gridSimpleEffect.Parameters["TransMaskUCCount"].SetValue((float)TransitionUCCount);
                gridSimpleEffect.Parameters["TransMaskULCount"].SetValue((float)TransitionULCount);
                gridSimpleEffect.Parameters["TransMaskUR"]?.SetValue(groundTransitionMaskArrayUR);
                gridSimpleEffect.Parameters["TransMaskUC"]?.SetValue(groundTransitionMaskArrayUC);
                gridSimpleEffect.Parameters["TransMaskUL"]?.SetValue(groundTransitionMaskArrayUL);
                gridSimpleEffect.Parameters["MaskUVOffset"]?.SetValue(MaskOffset);

                device.SamplerStates[0] = new SamplerState
                {
                    Filter = TextureFilter.Point,
                    AddressU = TextureAddressMode.Wrap,
                    AddressV = TextureAddressMode.Wrap,
                    AddressW = TextureAddressMode.Clamp
                };

                device.BlendState = BlendState.Opaque;
                gridSimpleEffect.CurrentTechnique.Passes[0].Apply();

                var primitivesPerHex = PrimitivesPerHex;
                device.DrawUserPrimitives(PrimitiveType.TriangleList, gridVertexDataSimple, 0, (int)(hexCount * primitivesPerHex));

                // Draw tile transitions
                long transitionHexesCount = tilesWithTransitionsCount + transitionCount;
                long transitionDataSize = transitionHexesCount * VerticesPerHex;
                if (gridVertexDataTransition == null || gridVertexDataTransition.Length < transitionDataSize)
                    gridVertexDataTransition = new GridVertexSimpleAux[(long)(transitionDataSize * BufferOversizeFactor)];

                transitionHexPositionOffsets[0] = Vector3.Zero;
                transitionHexPositionOffsets[1] = new Vector3(tileSize * 2.0f, -tileSize, 0); // UR
                transitionHexPositionOffsets[2] = new Vector3(0, -tileSize * 2.0f, 0); // UC
                transitionHexPositionOffsets[3] = new Vector3(-tileSize * 2.0f, -tileSize, 0); // UL

                transitionHexDrawFlags[0] = (byte)(TileTransitionDraw.DrawFlagUR | TileTransitionDraw.DrawFlagUC | TileTransitionDraw.DrawFlagUL);
                transitionHexDrawFlags[1] = TileTransitionDraw.DrawFlagUR;
                transitionHexDrawFlags[2] = TileTransitionDraw.DrawFlagUC;
                transitionHexDrawFlags[3] = TileTransitionDraw.DrawFlagUL;

                Vector2 centerMaskUV = new Vector2(.5f, .5f);

                vertLocation = 0;
                for (i = 0; i < tilesWithTransitionsCount; ++i)
                {
                    var tileDraw = tileTransitionDrawCache[i];
                    var tilebase = tileDraw.BaseTile;

                    var colorData = new Color(tilebase.Id, tilebase.Id, tilebase.Id, (byte)0);
                    var maskData = new Color(tilebase.TransitionUR, tilebase.TransitionUC, tilebase.TransitionUL, (byte)0);

                    tileRefsTemp[0] = tileDraw.BaseTile;
                    tileRefsTemp[1] = tileDraw.TileUR;
                    tileRefsTemp[2] = tileDraw.TileUC;
                    tileRefsTemp[3] = tileDraw.TileUL;

                    for (int iHex = 0; iHex < 4; ++iHex)
                    {
                        var tile = tileRefsTemp[iHex];

                        var vertOffset = transitionHexPositionOffsets[iHex];
                        var uvOffset = new Vector2(vertOffset.X / groundTransitionMaskArrayUC.Width,
                            vertOffset.Y / groundTransitionMaskArrayUC.Height);

                        var center = new Vector3(tile.Center, 0f);
                        var left = new Vector3(tile.Left, 0f);

                        for (int iTriangle = 0; iTriangle < TileVerts; ++iTriangle)
                        {
                            var v1 = left + tileVertexOffsets[iTriangle + 1];
                            var v2 = left + tileVertexOffsets[iTriangle];
                            var v1_uv = tileVertexMaskUVs[iTriangle + 1] + uvOffset;
                            var v2_uv = tileVertexMaskUVs[iTriangle] + uvOffset;
                            var center_uv = centerMaskUV + uvOffset;

                            gridVertexDataTransition[vertLocation].Position = v2;
                            gridVertexDataTransition[vertLocation].ByteData = colorData;
                            gridVertexDataTransition[vertLocation].MaskUV = v2_uv;
                            gridVertexDataTransition[vertLocation].Aux1 = maskData;

                            ++vertLocation;

                            gridVertexDataTransition[vertLocation].Position = v1;
                            gridVertexDataTransition[vertLocation].ByteData = colorData;
                            gridVertexDataTransition[vertLocation].MaskUV = v1_uv;
                            gridVertexDataTransition[vertLocation].Aux1 = maskData;

                            ++vertLocation;

                            gridVertexDataTransition[vertLocation].Position = center;
                            gridVertexDataTransition[vertLocation].ByteData = colorData;
                            gridVertexDataTransition[vertLocation].MaskUV = center_uv;
                            gridVertexDataTransition[vertLocation].Aux1 = maskData;

                            ++vertLocation;
                        }
                    }
                }

                gridSimpleEffect.CurrentTechnique.Passes[1].Apply();
                device.BlendState = BlendState.NonPremultiplied;
                device.DrawUserPrimitives(PrimitiveType.TriangleList, gridVertexDataTransition, 0, (int)(transitionHexesCount * primitivesPerHex));
            }
        }

        public void Render(WorldMap map)
        {
            // RenderGridOnePass(map);
            RenderGridSimple(map);
        }
    }
}
