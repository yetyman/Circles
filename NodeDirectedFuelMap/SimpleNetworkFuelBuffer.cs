﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static NodeDirectedFuelMap.ManipulatePoints;

namespace NodeDirectedFuelMap
{
    public class SimpleNetworkFuelBuffer : SimpleWindow
    {
        int PointVertexArrayBuffer;
        int PointArrayObject;
        int QuadElementBuffer;//squares
        int LineIndexesElementBuffer;//line indexes

        int FuelPoolBuffer;
        int FuelRequestBuffer;
        int FuelUsedBuffer;
        int LinesBuffer;
        int FuelPoolTexture;
        int FuelRequestTexture;
        int FuelUsedTexture;
        int LinesTexture;

        BlurryCircleShader Step1CreateFuelRequestShader;
        MultiColorLineShader RenderLinesShader;
        SubtractShader Step2TakeFuelShader;//take pool and request, regen pool?, subtract request from pool. 
        SomeMinAndDivideShader Step3PercentFuelTakenShader;//take request and pool, generate used amount from request and pool.
        SomeZeroingShader Step4FuelPoolZeroingShader;//take pool, set negatives to zero
        MaxMipMapShader Step5CalculateActivationsShader;//find the maximum activation level. well actually max activation optimized with max available fuel

        MultiViewShader texShader;
       
        ManipulatePoints points = new ManipulatePoints();
        ManipulateLines ManipulatedLines = new ManipulateLines();
        uint[] indices = {  // note that we start from 0!
            0, 1, 3,   // first triangle
            0, 2, 3    // second triangle
        };

        public SimpleNetworkFuelBuffer(int width, int height, string title) : base(width, height, title)
        {
            points.Allocate(count*100);
            ManipulatedLines.Allocate(count*9);
        }


        private const float half = .5f;
        private const float circleScale = half;
        public static float[] QuadCorners = new float[]
        {
            -circleScale,  circleScale, 0.0f,  //Top-left vertex
            -circleScale, -circleScale, 0.0f,  //Bottom-left vertex
             circleScale,  circleScale, 0.0f,  //Top-right vertex
             circleScale, -circleScale, 0.0f,  //Bottom-right vertex
        };
        static int count = 10000;
        static float overlap = 9f;
        static float RefuelRate = .01f;
        protected override void OnLoad()
        {
            var rand = new Random();
            //for(int i = 0; i<140000; i++)
            
            for (int i = 0; i < count; i++)
            {
                points.AddPoint(
                    (float)rand.NextDouble() * 3 - 1.5f,
                    (float)rand.NextDouble() * 3 - 1.5f,
                    (float)rand.NextDouble(),
                    (float)rand.NextDouble(),
                    (float)rand.NextDouble(),
                    (float)rand.NextDouble() * overlap / count,
                    (float)rand.NextDouble() * overlap / count,
                    (float)rand.NextDouble() * overlap / count
                );

            }
            for (int i = 0; i < count; i++)
            {
                //just guesstimating an average of 9 children per node. completely random
                for(int x = 0; x < 9; x++)
                    ManipulatedLines.AddLine(
                        rand.Next(0, points.PointCount),
                        rand.Next(0, points.PointCount)
                    );
            }

            GL.ClearColor(0f, 0f, 0f, 0.0f);
            
            FuelUsedBuffer = InitializeFrameBuffer();
            FuelUsedTexture = InitializeFloatFrameBufferTexture();
            CheckFramebufferStatus(FuelUsedBuffer);

            FuelPoolBuffer = InitializeFrameBuffer();
            FuelPoolTexture = InitializeFloatFrameBufferTexture();

            GL.ClearColor(1f, 0f, 0f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckFramebufferStatus(FuelPoolBuffer);
            GL.ClearColor(0f, 0f, 0f, 0.0f);

            FuelRequestBuffer = InitializeFrameBuffer();
            FuelRequestTexture = InitializeFloatFrameBufferTexture();
            CheckFramebufferStatus(FuelRequestBuffer);

            LinesBuffer = InitializeFrameBuffer();
            LinesTexture = InitializeRGBAFrameBufferTexture();
            CheckFramebufferStatus(LinesBuffer);



            PointVertexArrayBuffer = GL.GenBuffer();//make triangle object

            Step1CreateFuelRequestShader = new BlurryCircleShader(ClientSize, "1.DrawCircles\\CircleLayerShader.vert", "1.DrawCircles\\CircleShader.frag");
            Step2TakeFuelShader = new SubtractShader("ScreenTriangle.vert", "2.subtract fuel\\SubtractFrag.frag", FuelPoolTexture, FuelRequestTexture, .01f);
            Step3PercentFuelTakenShader = new SomeMinAndDivideShader("ScreenTriangle.vert", "3.fuel taken percent(create activation pool)\\SomeMinFrag.frag", FuelPoolTexture, FuelRequestTexture);
            Step4FuelPoolZeroingShader = new SomeZeroingShader("ScreenTriangle.vert", "4.remove negative fuel pool values\\SomeZeroingFrag.frag", FuelPoolTexture);
            Step5CalculateActivationsShader = new MaxMipMapShader("6.maximums\\MaxMipMap.compute", FuelPoolTexture, FuelRequestTexture);//TODO: this should actually be the growth potential texture, which is activation level * remainingfuel(fuelpool) where activation level = fuelused/fuelrequested*circleActivationEnergyLevel... but for testing this can be an already rendered image first to make sure the mipmap looks right
            CheckGPUErrors("Error initializing compute shader");//just in case
            RenderLinesShader = new MultiColorLineShader(ClientSize, "5.1.render lines\\LineLayerShader.vert", "5.1.render lines\\LineShader.frag");
            CheckGPUErrors("Error initializing line shader");//just in case
            texShader = new MultiViewShader("ScreenTriangle.vert", "5.2.render fuel textures\\MultiViewTexture.frag");

            CheckGPUErrors("Error initializing shader classes");//just in case
            //TODO: setup line buffer data arrays, vertex shaders, and line shaders

            PointArrayObject = GL.GenVertexArray();

            // ..:: Initialization code (done once (unless your object frequently changes)) :: ..
            // 1. bind Vertex Array Object
            GL.BindVertexArray(PointArrayObject);

            // 2. copy our vertices array in a buffer for OpenGL to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, PointVertexArrayBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, points.MaxPointCount * ManipulatePoints.PointSize * sizeof(float) + QuadCorners.Length * sizeof(float), (IntPtr)null, BufferUsageHint.DynamicDraw);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)0, QuadCorners.Length * sizeof(float), QuadCorners);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(QuadCorners.Length * sizeof(float)), points.Points.Length * sizeof(float), points.Points);

            CommonPatterns.QuadElementBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, CommonPatterns.QuadElementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            
            LineIndexesElementBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexesElementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, ManipulatedLines.LineCount*2 * sizeof(uint), ManipulatedLines.lines, BufferUsageHint.DynamicDraw);
            
            CheckGPUErrors("Error Loading before Float Buffer");//just in case
            
            Rand.Rand(50000 * 50);
            //new Thread(ProcessingLoopFake).Start();
            base.OnLoad();
        }

        
        RandomHelper86 Rand = new RandomHelper86();
        float[] randomValues = new float[50000 * 50];
        int[] randomInts = new int[2+5*50000];
        int RoundRobinI = 0;
        private void UpdateLocationRandomTestData()
        {
            try
            {
                int x = 0;
                int iIndex = 0;
                int fIndex = 0;
                //separating for profiling

                randomValues = Rand.Rand(50000 * 50);
                //Rand.RandDirect(randomValues);//booo
                //hi me. time to look at optimizing the point CRUD functions

                //separating out all the random integer casting for profiling and optimization
                var pointCount = points.PointCount;
                var inactivePointCount = points.firstOpenInactiveSpace/8-1;
                var lineCount = ManipulatedLines.LineCount;
                for (int i = 0; i < 2; i++)
                {
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (pointCount - 1) + 1)) * 8;//deactivate point. lowers pointcount by one
                }
                ;
                for (int i = 0; i < 50000; i++)
                {
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (pointCount - 1) + 1)) * 8;//deactivate. lowers point count by one
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (pointCount - 1))) * 8;//update. no effect on point count
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (pointCount - 1))) * 8;//update. no effect on point count
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (inactivePointCount))) * 8;//activated point. increases point count by one
                    //activate points called here. increases point count by one
                }
                ;
                for (int i = 0; i < 100; i++)
                { 
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (pointCount - 2))) * 8;
                }

                iIndex = 0;

                for (int i = 0; i < 2; i++)
                {


                    //cause inactive neurons to slowly fill up
                    points.DeactivatePoint(randomInts[iIndex++]);

                    points.AddPoint(
                        randomValues[x++] * 3 - 1.5f,//positionX
                        randomValues[x++] * 3 - 1.5f,//positionY
                        randomValues[x++],//size1
                        randomValues[x++],//size2
                        randomValues[x++],//size3
                        randomValues[x++] * overlap / count,//opacity1
                        randomValues[x++] * overlap / count,//opacity2                         
                        randomValues[x++] * overlap / count//opacity3);
                    );

                    //TODO: next step. lets keep lines around. adding them and removing them based on the currently active neurons. should be fast since we already eat the speed of adding and removing lines.
                }

                //if (points.InactiveNeurons.Count > 0)
                //{
                //    var neuronindex = (int)(randomValues[x++] * (points.InactiveNeurons.Count - 1));
                //    points.DeleteNeuron((Neuron)points.InactiveNeurons[neuronindex]);
                //}

                 ;
                for (int i = 0; i < 50000; i++)
                {

                    //next cache all the random values first. then use them. so that we can profile. i bet the slow part is actually the calls to pointcount. its a math operation we're doing constantly.
                    //update all kinds of values

                    points.DeactivatePoint(randomInts[iIndex++]);

                    points.UpdatePoint(randomInts[iIndex++],
                        randomValues[x++],//size1
                        randomValues[x++],//size2
                        randomValues[x++],//size3
                        randomValues[x++] * overlap / count,//opacity1
                        randomValues[x++] * overlap / count,//opacity2                         
                        randomValues[x++] * overlap / count//opacity3
                    );

                    points.MovePoint(randomInts[iIndex++],
                        randomValues[x++] * 3 - 1.5f,//position1
                        randomValues[x++] * 3 - 1.5f//position2
                    );


                    points.ActivatePoint(points.InactiveNeurons[randomInts[iIndex++]]);

                }

                ;
                //add some lines to the neurons
                for (int i = 0; i < 3; i++)
                {
                    var a = points.ActiveNeurons[randomInts[iIndex++]];
                    var b = points.ActiveNeurons[randomInts[iIndex++]];
                    a.To.Add(b);
                }
                //remove some lines from the neurons to keep it even
                int iii = 0;
                if (ManipulatedLines.LineCount > 50000)
                {
                    while (iii < 3)
                    {
                        if (RoundRobinI > points.PointCount)
                            RoundRobinI = 0;
                    
                        var n = points.ActiveNeurons[RoundRobinI++ * ManipulatePoints.PointSize];
                        if (n.To.Count() > 0)
                        {
                            n.To.RemoveAt(0);
                            iii++;
                        }
                    }
                }
                //set all the lines
                var g = ManipulatedLines.LineCount;
                int j = 0;
                var activeNeurons = (points.PointCount+1) * ManipulatePoints.PointSize;
                int plus = 0;
                for (int i = 0; i < activeNeurons; i+=8)
                {
                    for (int h = 0; h < points.ActiveNeurons[i].To.Count(); h++)
                    {
                        if (!points.ActiveNeurons[i].To[h].Active)
                            plus = activeNeurons;
                        else plus = 0;

                        if (j < g)
                            ManipulatedLines.UpdateLine(j, (points.ActiveNeurons[i].pointIndex)/8, (points.ActiveNeurons[i].To[h].pointIndex+plus)/8);//oops what if the point it points to isnt active, that wont make much sense. i think i need to cache ALL the point's locations, active or not on the gfx card, then just update the active list with its extra data often. or could all the points go on the gfx card? if 1 million of them would fit on it no problem in a single managable array then i'm game
                        else
                            ManipulatedLines.AddLine((points.ActiveNeurons[i].pointIndex)/8, (points.ActiveNeurons[i].To[h].pointIndex+plus)/8);
                        j++;
                    }
                }

                //clear remaining old lines
                g = ManipulatedLines.LineCount;
                for (; j < g; g--)
                    ManipulatedLines.RemoveLine(g);


            }
            catch (Exception ex)
            {
                ;
            }
            //update point data set
            GL.BindBuffer(BufferTarget.ArrayBuffer, PointVertexArrayBuffer); 
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(QuadCorners.Length * sizeof(float)), points.PointCount * ManipulatePoints.PointSize * sizeof(float), points.Points);
            CheckGPUErrors("Error Buffering Active points");

            //update point data set
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)((QuadCorners.Length + (points.PointCount+1) * ManipulatePoints.PointSize) * sizeof(float)), points.InactivePointCount*ManipulatePoints.PointSize * sizeof(float), points.InactivePoints);
            CheckGPUErrors("Error Buffering Inactive points");

            //update line data set
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexesElementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, ManipulatedLines.LineCount * 2 * sizeof(int), ManipulatedLines.lines, BufferUsageHint.DynamicDraw);
            CheckGPUErrors("Error Buffering Lines");
        }
        private void CheckFramebufferStatus(int requestBuffer)
        {
            var status = GL.CheckNamedFramebufferStatus(requestBuffer, FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
                Console.WriteLine("FBO not complete:" + status);

            CheckGPUErrors("Error completing frame buffer:");
        }

        private int InitializeFloatFrameBufferTexture()
        {
            var requestTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, requestTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);
            CheckGPUErrors("Error Loading Float Texture:");

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, requestTexture, 0);

            CheckGPUErrors("Error Binding Float Texture to Buffer:");
            return requestTexture;
        }

        private int InitializeRGBAFrameBufferTexture()
        {
            var requestTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, requestTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);
            CheckGPUErrors("Error Loading Float Texture:");

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, requestTexture, 0);

            CheckGPUErrors("Error Binding Float Texture to Buffer:");
            return requestTexture;
        }

        private int InitializeFrameBuffer()
        {
            var requestBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, requestBuffer);

            CheckGPUErrors("Error Loading Float Buffer");
            return requestBuffer;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            Console.WriteLine($"Frame time: {args.Time * 1000:0.000} ms"); 
            UpdateLocationRandomTestData();
            base.OnUpdateFrame(args);
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            
            GL.Clear(ClearBufferMask.ColorBufferBit);
            RequestFuel();
            RenderNodeLines();
            RenderInstrumentation();

            //if(Math.Abs(FuelZeroingShader.Average - .5f) > threshold)
            RefuelRate = (.5f - Step4FuelPoolZeroingShader.Average)/1.1f;

            Step2TakeFuelShader.AddValue = RefuelRate;

            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }
        /// <summary>
        /// one frame essentially of the neural net
        /// what's it doing shader wise? well basically...
        /// -CreateFuelRequestShader - drawing energy requirements of newly activated neurons
        /// -RequestFuelShader - subtracting them from a pool of "fuel" and refilling the pool at an adaptive rate
        /// -FuelUsedShader - set intermediate texture to some 0-1 values of what percent of requested fuel neurons should receive
        /// -FuelZeroingShader - cut off anything in the fuel pool texture that is above or below 0-1, keeping it in bounds
        /// </summary>
        public void RequestFuel()
        {

            //PoolMinMax[1] = FuelZeroingShader.Average;

            //create request pool buffer
            //-already handled by updateLocations loop
            //-draw opacities to request pool buffer
            //generate requested fuel usage buffer. so many circles
            Step1CreateFuelRequestShader.Use();
            CheckGPUErrors("Error using opacity shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelRequestBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, (IntPtr)0, points.PointCount);
            //hi me. pull up renderdoc and diagnose this stupid outofmemory exception!
            CheckGPUErrors("Error rendering to activation request float buffer:");


            //process fuel pool regen and request subtraction
            //subtract from fuel pool
            Step2TakeFuelShader.Use();
            CheckGPUErrors("Error using addsubtract shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelPoolBuffer);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            CheckGPUErrors("Error rendering to subtract from fuel pool float buffer:");

            //zero out negative fuel areas(in the future we may divide remaining fuel by the negative values or apply a division to the strength of any neurons in the negative vicinity if we aren't already, but for now
            Step3PercentFuelTakenShader.Use();
            CheckGPUErrors("Error using min shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelUsedBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            CheckGPUErrors("Error rendering to divide from fuel pool and fuel request float buffer:");

            //draw negative areas of fuel usage(overdrawing from the pool). this buffer is an intermediate for dividing activation energy in the next frame by negative valued area's deficit
            Step4FuelPoolZeroingShader.Use();
            CheckGPUErrors("Error using zeroing shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelPoolBuffer);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            CheckGPUErrors("Error rendering to min fuel pool float buffer:");

            //get average value in the fuel pool, used to set refill rate for next frame
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, CommonPatterns.QuadElementBuffer);
            Step4FuelPoolZeroingShader.Use();
            Step4FuelPoolZeroingShader.CheckAverage(ClientSize.X, ClientSize.Y);

            //gets the level of activation to be added for the next pass's activation propogation
            //we'll multiply the FuelPoolBuffer(an indicator for whether there is enough fuel available in a given area for activation to occur) with the fuel request buffer. 
            //Then we'll find the maximum areas for where a new neuron might be created. 
            //FUTURE: in the future this may separate so that new neuron creation is based on a different set of radiation values than neuron activation and at that point, the new neuron creation probability map would simply be the fuel available buffer multiplied by that radiation buffer
            Step5CalculateActivationsShader.Use();//later this will be used for new neuron creation



            Add4thValueToMaximumsReadShader.Use(range);//this will use the same buffer as step5 and the float array of points. doing it here in a compute shader will keep it in frame. this will make the 4th value of the maximums read be the uniqueid of the neuron closest to this point. 0 if no neuron was within range!
                                                       //range is based on the density of the graph


            //so awesome thought. we can totally make a list from a computer shader... every item in a small buffer is placed according to an index that is incremented by each shader in the tightest way possible if it passes the predicate then they each stick their result in there
            fuckyeahComputeAllRelatedActivePoints.Use();//everything that was affecting that spot. include how much it affected that spot! copy the distance intensity formula.  [UniqueId, strength, uniqueId, strength, ...]


            //read maximums will end up reading a lot more than one pixel...

            var maximumValues = Step5CalculateActivationsShader.ReadMaximum();//later this will be used for new neuron creation //this is always one frame behind.
            CheckGPUErrors("Error rendering to activation buffer:");

            ////get the highest points in the activation pool. should it be the highest or random high ones? i prefer less random, so lets go highest and see if it doesn't backfire completely
            //find max point or create a new point. when do we create a new point? when do we connect new things to an existing point?

            //this is last frame's most activated neuron
            Neuron mostActivated = points.GetNeuron(points.poin[(int)maximumValues[4]];//how do i resolve this a frame late? i feel the only way is to add the neuron's unique id to the float array and carry it all the way through to be the 4th entry in the pixel read
            if (mostActivated == null)
                MakeNewNeuron(fuckyeahComputeAllRelatedActivePoints.useLastFramesAffectingHere, fuckyeahComputeAllRelatedActivePoints.useThisFramesAffectingHere);

            CalculateAllTheNextFramesActivation()//for now just add to all children as needed. one pass, no second. all extra unactivated can just go back into the fuel grid with a radiation of the same equation as subtracted to feed the neuron's activation

            FindActivatedExternalNodes.Use();
            ProcessActions(FindActivatedExternalNodes.nodes);//this may be a copy buffer + asynchronous execution. who knows


            //when the highest activation point is within a given distance(variable, smaller as the grid becomes denser) then that point is the point that will be affected
            // -then reinforce existing lines and add week new ones to whatever's nearby and active... or no.. new lines should be forward only i think. no. it can't be. if it was, then what would the new point even connect to to activate anything. we start with a line, then we make it more advanced.
            // -new lines from anything involved at this location atm. we may add a um.. frequency variable later to determine how much overlap occurs with neuron creation and activation level etc. hm. we could end up with a rainbow radiation graph. nice thought for another time
            //if not, the highest activation point gains a new neuron
            // -i imagine we can just set this new point to have added itself as the recipient of every previous frame neuron on this spot and a sender to every neuron that was about to be activated? sounds good to me.


            //Step6FindMaximumsShader.Use();
            //CheckGPUErrors("Error using maximums compute shader:");
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, ActivationBuffer);//finding maximums of this, the activation buffer... fuel used isnt activation... its fuel used. activation is another blurry circle buffer i haven't made yet...
            //GL.Clear(ClearBufferMask.ColorBufferBit);//clear 
            //CheckGPUErrors("Error binding to opacity fbo:");
            //GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            //CheckGPUErrors("Error finding maximums:");

            //Step7FindMaximumSourcesShader.Use();
            //CheckGPUErrors("Error using maximums compute shader:");
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, ActivationBuffer);
            //GL.Clear(ClearBufferMask.ColorBufferBit);//clear 
            //CheckGPUErrors("Error binding to opacity fbo:");
            //GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            //CheckGPUErrors("Error finding maximums:");

            //two more steps.
            //8. create new neurons from maximums. kind of already handled
            //9. go from current neurons to child neurons. including both activated energy and activation energy buffer (paren'ts energy/default level)*requestfulfilledpercentage*default activation level+activation energy buffer)
            //    -resolve neuron's child connections with some fancy math(fast math is fine for now)
            //    -should activation level radiation be subtracted from action energy overall? make some falloff function for this if it starts overflowing
            //10. rinse and repeat
        }
        public void RenderNodeLines()
        {


            // 3. then set our vertex attributes pointers
            //which attribute are we settings, how many is it, what is each it?, should it be normalized?, what's the total size?,
            //TODO: i may not want to normalize

            GL.VertexAttribPointer(RenderLinesShader.PositionLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 12 * sizeof(float));
            GL.EnableVertexAttribArray(RenderLinesShader.PositionLocation);

            GL.VertexAttribDivisor(RenderLinesShader.PositionLocation, 0);//use from start to end, based on instance id instead of vertex index


            //TODO: hi me! this is the next step. fix up the way lines are displayed. i think all it'll really need is for the gfx card to keep all the point locations cached(active or otherwise) and then you can just reference that array in this shader to get all the locations instead of just the active ones
            //TODO: after that will be finding lines that start or end around the most optimal activation area(the compute shade may still need a little work to get that final locations out)

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexesElementBuffer);
            //process active connection array.
            RenderLinesShader.Use();
            CheckGPUErrors("Error using lines shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, LinesBuffer);
            //GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckGPUErrors("Error binding to lines fbo:");
            GL.DrawElements(BeginMode.Lines, ManipulatedLines.LineCount * 2, DrawElementsType.UnsignedInt, 0);
            CheckGPUErrors("Error rendering to line buffer:");
        }


        public RectangleF FuelRequestBounds = new RectangleF(.01f, .34f, .32f, .32f);
        public RectangleF FuelUsedBounds = new RectangleF(.34f, .34f, .32f, .32f);
        public RectangleF MipMapBounds = new RectangleF(.67f, .34f, .16f, .32f);//half width
        public RectangleF PoolBounds = new RectangleF(.01f, .01f, .32f, .32f);
        public RectangleF LinesBounds = new RectangleF(.34f, .01f, .32f, .32f);

        public float[] FuelRequestMinMax = new float[2] { 0, overlap * overlap / count * 6 };
        public float[] FuelUsedMinMax = new float[2] { 0, 1 };
        public float[] MipMapMinMax = new float[2] { 0, 1 };
        public float[] PoolMinMax = new float[2] { 0, 1 };
        public float[] LinesMinMax = new float[2] { 0, 1 };

        public void RenderInstrumentation()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, CommonPatterns.QuadElementBuffer);
            //draw float buffer from texture to back buffer. one call for each one we'd like to display
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            texShader.Use(FuelRequestTexture, FuelRequestBounds, FuelRequestMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(FuelUsedTexture, FuelUsedBounds, FuelUsedMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(Step5CalculateActivationsShader.MipMapImageHandle, MipMapBounds, MipMapMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(FuelPoolTexture, PoolBounds, PoolMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(LinesTexture, LinesBounds, LinesMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            CheckGPUErrors("Error rendering to back buffer:");
        }
        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(PointVertexArrayBuffer);
            GL.DeleteVertexArray(PointArrayObject);
            GL.DeleteFramebuffer(FuelRequestBuffer);
            GL.DeleteFramebuffer(FuelUsedBuffer);
            GL.DeleteFramebuffer(FuelPoolBuffer);
            GL.DeleteTexture(FuelRequestTexture);
            GL.DeleteTexture(LinesTexture);
            Step1CreateFuelRequestShader.Dispose();
            Step5CalculateActivationsShader.Dispose();
            texShader.Dispose();
            base.OnUnload();
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            Step1CreateFuelRequestShader.ViewPortSize = ClientSize;

            //create 2D tecture for fuel remaining projection
            GL.BindTexture(TextureTarget.Texture2D, FuelPoolTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);

            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelPoolBuffer);
            //GL.ClearColor(1f, 1f, 1f, 1f);
            //GL.Clear(ClearBufferMask.ColorBufferBit);
            //GL.ClearColor(0f, 0f, 0f, 0.0f);
            //CheckGPUErrors("Error resetting fuel pool:");

            //create 2D tecture for fuel requested projection
            GL.BindTexture(TextureTarget.Texture2D, FuelRequestTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);

            //create 2D texture for fuel used projection
            GL.BindTexture(TextureTarget.Texture2D, FuelUsedTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            
            //create 2D texture for lines to be rendered onto
            GL.BindTexture(TextureTarget.Texture2D, LinesTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

            base.OnResize(e);
        }
    }
}
