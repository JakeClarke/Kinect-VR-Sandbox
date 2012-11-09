using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace KinectVRSandbox
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        KinectComponent kinectComponent;
        SpriteFont font;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            this.graphics.PreferredBackBufferHeight = 768;
            this.graphics.PreferredBackBufferWidth = 1280;
            //this.graphics.IsFullScreen = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            this.kinectComponent = new KinectComponent(this);
            this.Components.Add(this.kinectComponent);
                
            this.Components.Add(new VRDisplay(this, this.kinectComponent));

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = this.Content.Load<SpriteFont>("font");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            var kState = Keyboard.GetState();
            if (kState.IsKeyDown(Keys.F))
            {
                this.graphics.IsFullScreen = !this.graphics.IsFullScreen;
                this.graphics.ApplyChanges();
            }
            else if (kState.IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            if (this.kinectComponent.ColorTex != null)
            {
                spriteBatch.Draw(this.kinectComponent.ColorTex, this.GraphicsDevice.Viewport.Bounds, Color.White);
            }
            if (this.kinectComponent.DepthTex != null)
            {
                spriteBatch.Draw(this.kinectComponent.DepthTex, this.kinectComponent.DepthTex.Bounds, Color.White);
            }
            if (this.kinectComponent.PlayerMaskTex != null)
            {
                spriteBatch.Draw(this.kinectComponent.PlayerMaskTex, new Vector2(this.kinectComponent.DepthTex.Width, 0), Color.White);
            }
            spriteBatch.End();

            spriteBatch.Begin();
            if(this.kinectComponent.SensorRunning)
                spriteBatch.DrawString(this.font, "Head offset:" + this.kinectComponent.HeadOffset, new Vector2(0, this.GraphicsDevice.Viewport.Height - 40f), Color.White);
            else
                spriteBatch.DrawString(this.font, "No kinect enabled!", new Vector2(0, this.GraphicsDevice.Viewport.Height - 40f), Color.Red);
            spriteBatch.End();

            // reset the graphics device.
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            base.Draw(gameTime);
        }
    }
}
