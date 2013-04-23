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
using System.Diagnostics;


namespace KinectVRSandbox
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class KinectComponent : Microsoft.Xna.Framework.GameComponent
    {
        static KinectSensor sensor; // static because we only want one hardware instance to be shared between instances of this class.
        Skeleton[] skeletons;
        Skeleton closestSkeleton;

        Boolean evaChangeRequested = false;

        Texture2D depthTex, colorTex, playermaskTex;

        TimeSpan timeBetweenEvaChange = new TimeSpan(0, 0, 30), lastEvaChange = TimeSpan.FromSeconds(-28);
        TimeSpan prevSkelFrameTime;

        Vector3 playerHeadStartingPos;

        bool headPosSet;

        readonly Color[] PlayerColors = new Color[] {Color.White, Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Gray, Color.MediumPurple};

        int tracked = 0;
        int targetEva;


        public KinectComponent(Game game)
            : base(game)
        {
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            initializeKinect();


            base.Initialize();
        }

        private void initializeKinect()
        {

            if (sensor == null || !sensor.IsRunning)
            {
                sensor = null; // set so we can detect if no suitable sensor can be found.
                foreach (var s in KinectSensor.KinectSensors)
                {
                    if (s.Status == KinectStatus.Connected)
                    {
                        sensor = s;

                        Debug.WriteLine("Opening sensor: " + sensor.UniqueKinectId);
                        sensor.Start();
                        sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                        sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);

                        sensor.SkeletonStream.Enable();
                        sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                        sensor.SkeletonStream.AppChoosesSkeletons = true;

                        this.targetEva = sensor.ElevationAngle;
                        break;
                    }
                }
            }

            if (sensor == null)
            {
                Debug.WriteLine("No suitable sensor found.");
            }
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            KeyboardState kstate = Keyboard.GetState();

            if (sensor != null || !sensor.IsRunning)
            {
                if (sensor.ColorStream.IsEnabled)
                {
                    using (var frame = sensor.ColorStream.OpenNextFrame(0))
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
                }
                else
                {
                    this.colorTex = null;
                }

                if (sensor.SkeletonStream.IsEnabled)
                {
                    using (var frame = sensor.SkeletonStream.OpenNextFrame(0))
                    {
                        if (frame != null)
                        {
                            if (this.skeletons == null || this.skeletons.Length != frame.SkeletonArrayLength)
                            {
                                this.skeletons = new Skeleton[frame.SkeletonArrayLength];
                            }
                            

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
                            if (closest != null)
                            {
                                if (this.closestSkeleton == null || this.closestSkeleton.TrackingId != closest.TrackingId)
                                {
                                    sensor.SkeletonStream.ChooseSkeletons(closest.TrackingId);
                                    headPosSet = false;
                                    Debug.WriteLine("New closest!");
                                }

                                this.closestSkeleton = closest;
                            }

                            if (!headPosSet && this.closestSkeleton != null && this.closestSkeleton.Joints[JointType.Head].TrackingState == JointTrackingState.Tracked)
                            {
                                this.headPosSet = true;
                                this.playerHeadStartingPos = new Vector3(this.closestSkeleton.Joints[JointType.Head].Position.X, this.closestSkeleton.Joints[JointType.Head].Position.Y, this.closestSkeleton.Joints[JointType.Head].Position.Z);
                                this.HeadOffset = Vector3.Zero;
                                Debug.WriteLine("Set head pos!");
                            }
                            else if (this.headPosSet && this.closestSkeleton.Joints[JointType.Head].TrackingState == JointTrackingState.Tracked)
                            {
                                this.HeadOffset = (new Vector3(this.closestSkeleton.Joints[JointType.Head].Position.X, this.closestSkeleton.Joints[JointType.Head].Position.Y, this.closestSkeleton.Joints[JointType.Head].Position.Z)) - this.playerHeadStartingPos;
                                this.HeadPosition = new Vector3(this.closestSkeleton.Joints[JointType.Head].Position.X, this.closestSkeleton.Joints[JointType.Head].Position.Y, this.closestSkeleton.Joints[JointType.Head].Position.Z);
                            }


                            this.prevSkelFrameTime = gameTime.TotalGameTime;
                        }
                    }
                }
                else
                {
                    this.skeletons = null;
                    this.closestSkeleton = null;
                }

                if (sensor.DepthStream.IsEnabled)
                {
                    using (var frame = sensor.DepthStream.OpenNextFrame(0))
                    {
                        if (frame != null)
                        {
                            // we have a fraem we can use so create a texture asset, for the depth data.
                            this.depthTex = new Texture2D(this.Game.GraphicsDevice, frame.Width, frame.Height, false, SurfaceFormat.Bgra4444);
                            // ... and another for the player mask image.
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

                            if (this.closestSkeleton != null)
                            {
                                // draw dots on the player mask for each location of the joints.
                                foreach (var item in this.closestSkeleton.Joints.ToArray())
                                {
                                    if (item.TrackingState == JointTrackingState.Tracked)
                                    {
                                        var pos = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(item.Position, frame.Format);
                                        int aOffset = pos.X + (pos.Y * frame.Width);
                                        if (aOffset < pMask.Length)
                                        {
                                            pMask[aOffset] = Color.Black;
                                        }

                                    }
                                }
                            }

                            this.depthTex.SetData<short>(pdata);
                            this.playermaskTex.SetData<Color>(pMask);
                        }
                    }

                }
                else
                {
                    this.depthTex = null;
                    this.playermaskTex = null;
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

                if (evaChangeRequested && this.targetEva != sensor.ElevationAngle && this.lastEvaChange.Add(this.timeBetweenEvaChange) < gameTime.TotalGameTime)
                {
                    this.lastEvaChange = gameTime.TotalGameTime;
                    evaChangeRequested = false;

                    if (sensor.MaxElevationAngle < this.targetEva)
                    {
                        this.targetEva = sensor.MaxElevationAngle;
                    }

                    try
                    {
                        sensor.ElevationAngle = this.targetEva;
                    }
                    catch
                    {

                    }
                }
            }
            else
            {
                // find a new kinect!
                this.initializeKinect();
            }


            base.Update(gameTime);
        }

        public void CloseDevice()
        {
            if (sensor.IsRunning)
            {
                sensor.Stop();
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

        public bool SensorRunning { get { return sensor != null && sensor.IsRunning; } }

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

        public Skeleton[] Skeletons
        {
            get { return this.skeletons.ToArray(); }
        }

        public Vector3 HeadOffset { get; set; }

        public Vector3 HeadPosition { get; set; }

        public bool HasSensor
        {
            get
            {
                return sensor != null;
            }
        }
    }
}
