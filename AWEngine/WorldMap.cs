using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Microsoft.Xna.Framework;
using Xna = Microsoft.Xna.Framework;

namespace AWEngine
{
    public class WorldRegion
    {
        protected WorldMap map;

        public Tile[,] TileGrid;

        protected string Name { get; set; }
        public Position Position { get; set; }
        public Size Size { get { return GridSize; } }
        public Size GridSize { get { return new Size(TileGrid.GetLength(0), TileGrid.GetLength(1)); } }

        public WorldRegion(WorldMap parent)
        {
            map = parent;
        }

        public WorldBounds Bounds
        {
            get
            {
                return new WorldBounds(Position, Position + Size);
            }
        }

        public bool Contains(Position pos)
        {
            return Bounds.Contains(pos);
        }

        public TileRef this[int x, int y]
        {
            get
            {
                return new TileRef(map, this, new Position(x, y));
            }
        }

        public TileRef this[Position pos]
        {
            get
            {
                return new TileRef(map, this, new Position(pos.X, pos.Y));
            }
        }

        public IEnumerable<TileRef> GetTilesInBounds(WorldBounds bounds)
        {
            int x_start = Math.Max(bounds.Lower.X - Position.X, 0);
            int y_start = Math.Max(bounds.Lower.Y - Position.Y, 0);
            int x_end = Math.Min(bounds.Upper.X - Position.X, TileGrid.GetLength(0));
            int y_end = Math.Min(bounds.Upper.Y - Position.Y, TileGrid.GetLength(1));

            for (var x = x_start; x < x_end; ++x)
            {
                for (var y = y_start; y < y_end; ++y)
                {
                    yield return new TileRef(map, this, new Position(x, y));
                }
            }
        }

        public IEnumerable<TileRef> AllTiles
        {
            get
            {
                int x_start = 0;
                int y_start = 0;
                int x_end = TileGrid.GetLength(0);
                int y_end = TileGrid.GetLength(1);

                for (var x = x_start; x < x_end; ++x)
                {
                    for (var y = y_start; y < y_end; ++y)
                    {
                        yield return new TileRef(map, this, new Position(x, y));
                    }
                }
            }
        }
    }

    public struct WorldBounds
    {
        public Position Lower; // Inclusive
        public Position Upper; // Exclusive

        public WorldBounds(Position lower, Position upper)
        {
            Lower = lower;
            Upper = upper;
        }

        public bool Contains(Position pos)
        {
            return pos.X >= Lower.X && pos.Y >= Lower.Y && pos.X < Upper.X && pos.Y < Upper.Y;
        }
    }

    public struct Position
    {
        public int X;
        public int Y;

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Position operator +(Position p1, Position p2)
        {
            return new Position { X = p1.X + p2.X, Y = p1.Y + p2.Y };
        }

        public static Position operator +(Position p1, Size p2)
        {
            return new Position { X = p1.X + p2.Width, Y = p1.Y + p2.Height };
        }
    }

    public enum WorldDirection
    {
        North = 0,
        South = 1,
        NorthWest = 2,
        NorthEast = 3,
        SouthWest = 4,
        SouthEast = 5
    }

    public struct Tile
    {
        public byte Id;
        public byte B1; // Used to store which transitions to use (UR and UC), 4 bits each
        public byte B2; // Used to store whcih transitions to use (UL), 4 bits each

        public byte TransitionUR
        {
            get { return (byte)(B1 & 0x0F); }
            set => B1 = (byte)((B1 & 0xF0) | (value & 0x0F));
        }

        public byte TransitionUC
        {
            get { return (byte)((B1 & 0xF0) >> 4); }
            set => B1 = (byte)((B1 & 0x0F) | (value << 4));
        }

        public byte TransitionUL
        {
            get { return (byte)(B2 & 0x0F); }
            set => B2 = (byte)((B2 & 0xF0) | (value & 0x0F));
        }
    }

    public struct TileRef
    {
        private WorldMap map;
        private WorldRegion region;
        public Position Position { get; set; }

        public Tile Tile
        {
            get
            {
                if (region != null)
                    return region.TileGrid[Position.X - region.Position.X, Position.Y - region.Position.Y];
                else
                    return WorldMap.EmptyTile;
            }
        }

        public byte Id
        {
            get
            {
                return Tile.Id;
            }
            set
            {
                if (region != null)
                    region.TileGrid[Position.X - region.Position.X, Position.Y - region.Position.Y].Id = value;
            }
        }

        public byte TransitionUR
        {
            get { return Tile.TransitionUR; }
            set
            {
                if (region != null)
                    region.TileGrid[Position.X - region.Position.X, Position.Y - region.Position.Y].TransitionUR = value;
            }
        }

        public byte TransitionUC
        {
            get { return Tile.TransitionUC; }
            set
            {
                if (region != null)
                    region.TileGrid[Position.X - region.Position.X, Position.Y - region.Position.Y].TransitionUC = value;
            }
        }

        public byte TransitionUL
        {
            get { return Tile.TransitionUL; }
            set
            {
                if (region != null)
                    region.TileGrid[Position.X - region.Position.X, Position.Y - region.Position.Y].TransitionUL = value;
            }
        }

        public Vector2 Left { get { return map.GetTileLeft(Position); } }
        public Vector2 Center { get { return map.GetTileCenter(Position); } }

        public TileRef(WorldMap map, WorldRegion region, Position position)
        {
            this.map = map;
            this.region = region;
            Position = position;
        }

        public TileRef GetTileInDirection(WorldDirection dir)
        {
            var newPos = Position + Deltas[Position.X & 0x1][(int)dir];
            if (region != null || !region.Contains(newPos))
                return map[newPos];
            else
                return new TileRef(map, region, newPos);
        }

        public TileRef[] GetAdjacentTiles()
        {
            return new[] {
                GetTileInDirection(WorldDirection.North),
                GetTileInDirection(WorldDirection.South),
                GetTileInDirection(WorldDirection.NorthWest),
                GetTileInDirection(WorldDirection.NorthEast),
                GetTileInDirection(WorldDirection.SouthWest),
                GetTileInDirection(WorldDirection.SouthEast)
            };
        }

        public static Position[][] Deltas =
        {
            // Even Deltas
            new[] {
                new Position(0, -1),
                new Position(0, 1),
                new Position(-1, -1),
                new Position(1, -1),
                new Position(-1, 0),
                new Position(1, 0)
            },

            // Odd Deltas
            new[] {
                new Position(0, -1),
                new Position(0, 1),
                new Position(-1, 0),
                new Position(1, 0),
                new Position(-1, 1),
                new Position(1, 1)
            }
        };
    }

    public class WorldMap
    {
        public static Tile EmptyTile = new Tile() { Id = 0 };
        public const int TileSizeDefault = 24;

        public int TileSize { get; set; } = TileSizeDefault;

        protected List<WorldRegion> regions = new List<WorldRegion>();

        public List<WorldRegion> Regions { get { return regions; } }

        public TileRef this[int x, int y]
        {
            get
            {
                var pos = new Position(x, y);
                return this[pos];
            }
        }

        public TileRef this[Position pos]
        {
            get
            {
                foreach (var region in regions)
                {
                    if (region.Contains(pos))
                        return region[pos];
                }

                return new TileRef(this, null, pos);
            }
        }

        // Get left-most position of tile
        public Vector2 GetTileLeft(Position pos)
        {
            return new Vector2(TileSize * pos.X * 2, TileSize * pos.Y * 2 + (pos.X & 1) * TileSize);
        }

        public Vector2 GetTileCenter(Position pos)
        {
            return GetTileLeft(pos) + new Vector2(TileSize * 1.5f, 0f);
        }

        public WorldBounds GetRectangleBounds(Xna.Rectangle rect)
        {
            var x = Math.Floor(rect.X / (TileSize * 2.0f));
            var y = Math.Floor(rect.Y / (TileSize * 2.0f));
            var width = Math.Ceiling(rect.Width / (TileSize * 2.0f));
            var height = Math.Ceiling(rect.Height / (TileSize * 2.0f));
            var upperx = x + width;
            var uppery = y + height;
            return new WorldBounds()
            {
                Lower = new Position((int)x - 1, (int)y - 1),
                Upper = new Position((int)Math.Ceiling(upperx) + 2, (int)Math.Ceiling(uppery) + 2)
            };
        }

        public WorldRegion CreateEmptyRegion(Position pos, Size size)
        {
            var region = new WorldRegion(this)
            {
                Position = pos,
                TileGrid = new Tile[size.Width, size.Height]
            };

            Regions.Add(region);

            return region;
        }

        public IEnumerable<TileRef> GetTilesInRectangle(Xna.Rectangle rect)
        {
            return GetTilesInBounds(GetRectangleBounds(rect));
        }

        public IEnumerable<TileRef> GetTilesInBounds(WorldBounds bounds)
        {
            var enumer = Enumerable.Empty<TileRef>();

            foreach (var region in regions)
                enumer = enumer.Concat(region.GetTilesInBounds(bounds));

            return enumer;
        }

        public IEnumerable<TileRef> AllTiles
        {
            get
            {
                var enumer = Enumerable.Empty<TileRef>();

                foreach (var region in regions)
                    enumer = enumer.Concat(region.AllTiles);

                return enumer;
            }
        }
    }
}
