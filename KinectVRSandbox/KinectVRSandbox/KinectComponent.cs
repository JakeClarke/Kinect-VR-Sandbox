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
using Microsoft.Kinect;


namespace KinectVRSandbox
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class KinectComponent : Microsoft.Xna.Framework.GameComponent
    {
        KinectSensor sensor;
        Skeleton[] skeletons = new Skeleton[0];
        Skeleton closestSkeleton;

        Boolean evaChangeRequested = false;

        Texture2D depthTex, colorTex, playermaskTex;

        TimeSpan timeBetweenEvaChange = new TimeSpan(0, 0, 30), lastEvaChange = TimeSpan.FromSeconds(-28);
        TimeSpan prevSkelFrameTime;

        Vector3 playerHeadRS;

        readonly Color[] PlayerColors = new Color[] {Color.White, Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Gray };

        int tracked = 0;
        int targetEva;

        public KinectComponent(Game game)
            : base(game)
        {
            // TODO: Construct any child components here
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            foreach (var sensor in KinectSensor.KinectSensors)
            {
                if (sensor.Status == KinectStatus.Connected)
                {
                    this.sensor = sensor;
                    break;
                }
            }

            if (this.sensor != null)
            {
                this.sensor.Start();
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);

                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                this.sensor.SkeletonStream.AppChoosesSkeletons = true;

                this.targetEva = this.sensor.ElevationAngle;
            }


            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            KeyboardState kstate = Keyboard.GetState();

            if (this.sensor != null)
            {



                using (var frame = this.sensor.ColorStream.OpenNextFrame(0))
                {
                    if (frame != null)
                    {
                        this.colorTex = new Texture2D(this.Game.GraphicsDevice, frame.Width, frame.Height, false, SurfaceFormat.Color);

                        byte[] data = new byte[frame.PixelDataLength];

                        frame.CopyPixelDataTo(data);

                        // convert bgr to rgb
                        for (int i = 0; i < data.Length; i += 4)
                        {
                            byte temp = data[i];
                            data[i] = data[i + 2];
                            data[i + 2] = temp;
                        }

                        this.colorTex.SetData<byte>(data);
                    }
                }

                using (var frame = this.sensor.DepthStream.OpenNextFrame(0))
                {
                    if (frame != null)
                    {
                        this.depthTex = new Texture2D(this.Game.GraphicsDevice, frame.Width, frame.Height, false, SurfaceFormat.Bgra4444);
                        this.playermaskTex = new Texture2D(this.Game.GraphicsDevice, frame.Width, frame.Height);

                        short[] pdata = new short[frame.PixelDataLength];
                        Color[] pMask = new Color[frame.PixelDataLength];

                        DepthImagePixel[] data = new DepthImagePixel[frame.PixelDataLength];
                        frame.CopyDepthImagePixelDataTo(data);

                        for (int i = 0; i < data.Length; i++)
                        {
                            pdata[i] = data[i].Depth;
                            pMask[i] = PlayerColors[data[i].PlayerIndex];
                        }
                        this.depthTex.SetData<short>(pdata);
                        this.playermaskTex.SetData<Color>(pMask);
                    }
                }

                using (var frame = this.sensor.SkeletonStream.OpenNextFrame(0))
                {
                    if (frame != null)
                    {
                        this.skeletons = new Skeleton[frame.SkeletonArrayLength];

                        frame.CopySkeletonDataTo(skeletons);
                        Skeleton closest = null;

                        this.tracked = 0;

                        for (int i = 0; i < this.skeletons.Length; i++)
                        {
                            if (this.skeletons[i].TrackingState == SkeletonTrackingState.PositionOnly || this.skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                            {
                                if (closest == null || closest.Position.Z > this.skeletons[i].Position.Z)
                                {
                                    closest = this.skeletons[i];

                                }

                                this.tracked++;
                            }
                        }

                        if (closest != null && closest.TrackingState != SkeletonTrackingState.Tracked)
                        {
                            this.sensor.SkeletonStream.ChooseSkeletons(closest.TrackingId);
                        }

                        this.closestSkeleton = closest;


                        this.prevSkelFrameTime = gameTime.TotalGameTime;
                    }
                }

                if (kstate.IsKeyDown(Keys.D1))
                {
                    this.targetEva++;
                    this.lastEvaChange = (gameTime.TotalGameTime > this.lastEvaChange.Add(this.timeBetweenEvaChange)) ? gameTime.TotalGameTime.Subtract(TimeSpan.FromSeconds(28)) : this.lastEvaChange;
                    evaChangeRequested = true;
                }
                else if (kstate.IsKeyDown(Keys.D2))
                {
                    this.targetEva--;
                    this.lastEvaChange = (gameTime.TotalGameTime > this.lastEvaChange.Add(this.timeBetweenEvaChange)) ? gameTime.TotalGameTime.Subtract(TimeSpan.FromSeconds(28)) : this.lastEvaChange;
                    evaChangeRequested = true;
                }

                if (evaChangeRequested && this.targetEva != this.sensor.ElevationAngle && this.lastEvaChange.Add(this.timeBetweenEvaChange) < gameTime.TotalGameTime)
                {
                    this.lastEvaChange = gameTime.TotalGameTime;
                    evaChangeRequested = false;

                    if (this.sensor.MaxElevationAngle < this.targetEva)
                    {
                        this.targetEva = this.sensor.MaxElevationAngle;
                    }

                    try
                    {
                        this.sensor.ElevationAngle = this.targetEva;
                    }
                    catch
                    {

                    }
                }
            }


            base.Update(gameTime);
        }

        public void CloseDevice()
        {
            if (this.SensorRunning)
            {
                this.sensor.Stop();
            }
        }

        public Texture2D ColorTex
        {
            get { return colorTex; }
        }

        public Texture2D DepthTex
        {
            get { return depthTex; }
        }

        public Texture2D PlayerMaskTex
        {
            get { return this.playermaskTex; }
        }

        public Boolean SensorRunning { get { return this.sensor != null && this.sensor.IsRunning; } }

        public int TrackedPlayers
        {
            get { return tracked; }
        }

        public int TargetEva
        {
            get { return targetEva; }
        }

        public Skeleton ClosestSkeleton
        {
            get { return this.closestSkeleton; }
        }

        public Skeleton[] Skeleton
        {
            get { return this.skeletons; }
        }
    }
}
