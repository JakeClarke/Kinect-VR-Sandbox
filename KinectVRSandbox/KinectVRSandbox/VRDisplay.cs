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
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class VRDisplay : Microsoft.Xna.Framework.DrawableGameComponent
    {
        KinectComponent kinect;
        Matrix view, projection, worldRotation = Matrix.CreateRotationX(MathHelper.ToRadians(-15f)) * Matrix.CreateRotationY(MathHelper.ToRadians(15f));
        Vector3 cameraPos, cameraTarget = new Vector3(0, 0, 500), rsHeadoffset;
        Model model;
        const float HEAD_SCALER = 1f;
        const float FIELD_OF_VIEW = 80f;
        float radiansPerPixel = 0f;

        const float NEAR_PLANE = 1f, FAR_PLANE = 100000f;
        

        public VRDisplay(Game game, KinectComponent kinect)
            : base(game)
        {
            this.kinect = kinect;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            this.radiansPerPixel = (float)((Math.PI / 180d) * (double)FIELD_OF_VIEW) / this.Game.GraphicsDevice.Viewport.Width;
            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            if (this.kinect.ClosestSkeleton != null)
            {

                //this.rsHeadoffset = new Vector3(-kinect.HeadOffset.X, kinect.HeadOffset.Y, -(kinect.HeadOffset.Z *0.5f)) * HEAD_SCALER;
                this.rsHeadoffset = kinect.HeadPosition != Vector3.Zero ?new Vector3(-kinect.HeadPosition.X, kinect.HeadPosition.Y, -(kinect.HeadOffset.Z - 0.5f)) * HEAD_SCALER : Vector3.Zero;
                //this.rsHeadoffset = new Vector3((float)Math.Sin(kinect.ClosestSkeleton.Joints[Microsoft.Kinect.JointType.Head].Position.X  * radiansPerPixel) * kinect.HeadOffset.Z, kinect.HeadOffset.Y, -kinect.HeadOffset.Z);
            }
            //this.projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FIELD_OF_VIEW), this.Game.GraphicsDevice.Viewport.AspectRatio, NEAR_PLANE, FAR_PLANE);
            
            /*
            this.projection = Matrix.CreatePerspectiveOffCenter(NEAR_PLANE * (-0.5f * this.Game.GraphicsDevice.Viewport.AspectRatio + kinect.HeadPosition.X) / kinect.HeadPosition.Z,
                NEAR_PLANE * (0.5f * this.Game.GraphicsDevice.Viewport.AspectRatio + kinect.HeadPosition.X) / kinect.HeadPosition.Z,
                NEAR_PLANE * (-.5f - kinect.HeadPosition.Y) / kinect.HeadPosition.Z,
                NEAR_PLANE * (.5f - kinect.HeadPosition.Y) / kinect.HeadPosition.Z, NEAR_PLANE, FAR_PLANE);
             * 
             */

            this.projection = Matrix.CreatePerspectiveOffCenter(NEAR_PLANE * (-0.5f * this.Game.GraphicsDevice.Viewport.AspectRatio + this.rsHeadoffset.X) / this.rsHeadoffset.Z,
                NEAR_PLANE * (0.5f * this.Game.GraphicsDevice.Viewport.AspectRatio + this.rsHeadoffset.X) / this.rsHeadoffset.Z,
                NEAR_PLANE * (-.5f - this.rsHeadoffset.Y) / this.rsHeadoffset.Z,
                NEAR_PLANE * (.5f - this.rsHeadoffset.Y) / this.rsHeadoffset.Z, NEAR_PLANE, FAR_PLANE);

            this.view = Matrix.CreateLookAt(cameraPos + this.rsHeadoffset, cameraTarget  + new Vector3(this.rsHeadoffset.X, this.rsHeadoffset.Y, 0), Vector3.Up);

            base.Update(gameTime);
        }

        protected override void LoadContent()
        {
            
            this.model = this.Game.Content.Load<Model>("ship1");
            base.LoadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            this.drawModel(this.model, this.cameraTarget);
            this.drawModel(this.model, new Vector3(200, 0, 1000));
            this.drawModel(this.model, new Vector3(-200, 0, 1000));
            this.drawModel(this.model, new Vector3(200, 0, 100));
            this.drawModel(this.model, new Vector3(-200, 0, 100));
            base.Draw(gameTime);
        }

        void drawModel(Model model, Vector3 position)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyBoneTransformsTo(transforms);
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.View = this.view;
                    effect.Projection = this.projection;
                    effect.World = worldRotation * transforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(position);
                }
                mesh.Draw();
            }
        }
    }
}
