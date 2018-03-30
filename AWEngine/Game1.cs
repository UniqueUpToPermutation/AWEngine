using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Xna = Microsoft.Xna.Framework;

using AoWGraphics;

using ColorXNA = Microsoft.Xna.Framework.Color;

namespace AWEngine
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        WorldMap map = new WorldMap();
        WorldRenderer renderer;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            WorldRendererFactory factory = new WorldRendererFactory();
            renderer = factory.CreateRenderer(GraphicsDevice, Content);

            map.CreateEmptyRegion(new Position(0, 0), new Size(50, 50));

            var rand = new Random();
            foreach (var tile in map.AllTiles)
            {
                var tileCpy = tile;
                tileCpy.TransitionUR = (byte)rand.Next(renderer.TransitionURCount);
                tileCpy.TransitionUC = (byte)rand.Next(renderer.TransitionUCCount);
                tileCpy.TransitionUL = (byte)rand.Next(renderer.TransitionULCount);
            }

            for (int x = 3; x < 10; ++x)
            {
                for (int y = 3; y < 10; ++y)
                {
                    var tileRef = map[x, y];
                    tileRef.Id = 1;
                }
            }

            for (int x = 14; x < 25; ++x)
            {
                for (int y = 2; y < 8; ++y)
                {
                    var tileRef = map[x, y];
                    tileRef.Id = 2;
                }
            }

            for (int x = 6; x < 15; ++x)
            {
                for (int y = 8; y < 14; ++y)
                {
                    var tileRef = map[x, y];
                    tileRef.Id = 3;
                }
            }

            for (int x = 2; x < 7; ++x)
            {
                for (int y = 10; y < 18; ++y)
                {
                    var tileRef = map[x, y];
                    tileRef.Id = 5;
                }
            }

            for (int x = 13; x < 27; ++x)
            {
                for (int y = 12; y < 17; ++y)
                {
                    var tileRef = map[x, y];
                    tileRef.Id = 7;
                }
            }
        }

        protected override void UnloadContent()
        {

        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            float CameraVelocity = 5.0f;
            var keys = Keyboard.GetState();

            if (keys.IsKeyDown(Keys.Right))
                renderer.CurrentCamera.Position.X += CameraVelocity;
            if (keys.IsKeyDown(Keys.Left))
                renderer.CurrentCamera.Position.X -= CameraVelocity;
            if (keys.IsKeyDown(Keys.Up))
                renderer.CurrentCamera.Position.Y -= CameraVelocity;
            if (keys.IsKeyDown(Keys.Down))
                renderer.CurrentCamera.Position.Y += CameraVelocity;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(ColorXNA.Black);

            renderer.Render(map);

            base.Draw(gameTime);
        }
    }
}
