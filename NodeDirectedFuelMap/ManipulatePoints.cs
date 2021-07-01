using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NodeDirectedFuelMap
{
    public class ManipulatePoints
    {
        //i believe that most functionality here may eventually be parallized into a shader, compute shader or otherwise.
        //for now, for clarity, i'll use c# and update the buffered data as i have been, 
        //this will like be the first bottleneck to overcome as the graph becomes functional

        private const float half = .5f;
        private const float circleScale = half;

        public float[] points;
        public Neuron[] Neurons;
        public int firstOpenSpace = 0;
        public object firstOpenLock = new object();


        public const float MinimumRadius = .01f;
        public const float MinimumIntensity = .0001f;
        public const float DefaultImpulseRadius = MinimumRadius;
        public const float DefaultFuelRadius = MinimumRadius;
        public const float DefaultNodeCreationRadius = MinimumRadius;
        public const float DefaultImpulseIntensity = MinimumIntensity;
        public const float DefaultFuelIntensity = MinimumIntensity;
        public const float DefaultNodeCreationIntensity = MinimumIntensity;

        public const int pointSize = 8;
        public static int cornerSpace => circleCorners.Length;
        public int allocatedSpace => points.Length;
        public int PointCount => (firstOpenSpace - cornerSpace - 1) / pointSize;
        public static float[] circleCorners = new float[]
        {
            -circleScale,  circleScale, 0.0f,  //Top-left vertex
            -circleScale, -circleScale, 0.0f,  //Bottom-left vertex
             circleScale,  circleScale, 0.0f,  //Top-right vertex
             circleScale, -circleScale, 0.0f,  //Bottom-right vertex
        };

        /// <summary>
        /// allocate total possible memory from the get go for all points
        /// </summary>
        /// <param name="maxPointCount"></param>
        public void Allocate(int maxPointCount)
        {
            var maxUnusedCount = maxPointCount;
            var maxActiveCount = maxUnusedCount / 10;

            lock (firstOpenLock)
            {
                //include the corners array in buffered data
                var actualLength = cornerSpace + maxActiveCount * pointSize;

                points = new float[actualLength];
                Neurons = new Neuron[actualLength];//empty pointers in the array are fine. they cost a lot less than translation math for every change for every frame
                unusedNeurons.EnsureCapacity(maxUnusedCount);

                //copy corners to buffered data. probably static, but we'll see
                for (int i = 0; i < cornerSpace; i++)
                    points[i] = circleCorners[i];

                firstOpenSpace = cornerSpace;
                        Console.WriteLine($"first open point is now {firstOpenSpace}");
            }
        }

        public class Neuron
        {
            public int UniqueId;

            public int pointIndex;

            public float X = 0;
            public float Y = 0;
            public float ImpulseRadius = DefaultImpulseRadius;
            public float FuelRadius = DefaultFuelRadius;
            public float NodeCreationRadius = DefaultNodeCreationRadius;
            public float ImpulseIntensity = DefaultImpulseIntensity;
            public float FuelIntensity = DefaultFuelIntensity;
            public float NodeCreationIntensity = DefaultNodeCreationIntensity;
            public List<Neuron> To = new List<Neuron>();
            public List<Neuron> From = new List<Neuron>();

            public override int GetHashCode()
            {
                return UniqueId;
            }
            public override bool Equals(object obj)
            {
                if (obj is Neuron n)
                    return UniqueId == n.UniqueId;
                else if (obj is int i)
                    return UniqueId == i;
                else return false;
            }
        }
        public void MoveNeuronToUnused(Neuron neuron)
        {
            neuron.pointIndex = 0;
            lock (unusedNeurons)
                unusedNeurons.Add(neuron);
        }
        public void MoveNeuronToActive(Neuron neuron, int index)
        {
            lock (unusedNeurons)
                unusedNeurons.Remove(neuron);

            lock (neuron)
            {
                neuron.pointIndex = index;
                Neurons[index] = neuron;
                Console.WriteLine($"the new neuron at {index} is {(Neurons[index] != null ? "Not " : "")} Null");
            }
        }
        private static int NeuronIDs = 0;
        public Neuron CreateNeuron(float x, float y, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {

            if (!r1.HasValue) r1 = DefaultImpulseRadius;
            if (!r2.HasValue) r2 = DefaultFuelRadius;
            if (!r3.HasValue) r3 = DefaultNodeCreationRadius;
            if (!o1.HasValue) o1 = DefaultImpulseIntensity;
            if (!o2.HasValue) o2 = DefaultFuelIntensity;
            if (!o3.HasValue) o3 = DefaultNodeCreationIntensity;

            //for now just create, soon pool instead
            var n = new Neuron()
            {
                X = x,
                Y = y,
                ImpulseRadius = r1.Value,
                FuelRadius = r2.Value,
                NodeCreationRadius = r3.Value,
                ImpulseIntensity = o1.Value,
                FuelIntensity = o2.Value,
                NodeCreationIntensity = o3.Value,

                UniqueId = NeuronIDs++
            };

            return n;
        }
        public void DeleteNeuron(Neuron n)
        {
            lock (unusedNeurons)
                lock (n)
                    unusedNeurons.Remove(n);
        }
        public object obj = new object();
        public void MoveNeuron(int from, int to)
        {
            Neuron n = null;

            //Console.WriteLine(Thread.CurrentThread.Name + $"Assigning {(Neurons[from] != null ? "Y" : "N")}{from} to {(Neurons[to] != null ? "Y" : "N")}{to}, from {from} should now be null");
            //if (Neurons[from] == null)
            //    Console.WriteLine($"Warning {from} is already null");

            try
            {
                lock (Neurons[from])
                {
                    n = Neurons[from];
                    Neurons[from] = null;//whenever this location is nulled, the data array ought to also be null at this location.
                }
            }catch(Exception ex)
            {
                ;//time to read log messages and figure out exactly what led to this
            }

            lock (Neurons[to])
            {
                Neurons[to] = n;
                Neurons[to].pointIndex = to;

                Console.WriteLine(Thread.CurrentThread.Name + $"Assigned {(Neurons[from] != null ? "Y" : "N")}{from} to {(Neurons[to] != null ? "Y" : "N")}{to}, from {from} should now be null, but is not locked an may not be null now. that's not a problem");
                if (Neurons[to] == null)
                    Console.WriteLine(Thread.CurrentThread.Name + $"Warning {to} is now null!");
            }
        }

        public HashSet<Neuron> unusedNeurons = new HashSet<Neuron>();
        /// <summary>
        /// just wrapping for reference passing
        /// </summary>
        public class PointIndex
        {
            public int pointIndex;
            public int references = 1;
            public object secondaryLock = new object();
            public override int GetHashCode()
            {
                return pointIndex.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                return obj is PointIndex p && p.pointIndex == pointIndex || obj is int i && i + cornerSpace == pointIndex;
            }
        }

        public PointLockPool PointLocks = new PointLockPool();
        public class PointLockPool
        {
            public Dictionary<int, PointIndex> pointLocks = new Dictionary<int, PointIndex>();
            List<PointIndex> PointPool = new List<PointIndex>();

            public PointLockPool()
            {
            }

            public PointIndex GetOne(int index)
            {
                PointIndex p = null;
                try
                {
                    //Console.WriteLine(Thread.CurrentThread.Name + " finding point lock for index " + index);
                    bool b = false;
                    try
                    {
                        Monitor.Enter(pointLocks);
                        //Console.WriteLine(Thread.CurrentThread.Name + " locked pointlocks 1");
                        b = pointLocks.ContainsKey(index + cornerSpace);
                        if (b)
                        {
                            p = pointLocks[index + cornerSpace];
                            Monitor.Enter(p);
                            //Console.WriteLine(Thread.CurrentThread.Name + " point lock found for index " + index);
                            p.references++;
                        }


                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine("GetOne has failed for index "+index);
                        //Console.WriteLine(ex);
                    }
                    finally
                    {
                        Monitor.Exit(pointLocks);
                    }
                    //Console.WriteLine(Thread.CurrentThread.Name + " released pointlocks 1");

                    if (!b)
                    {
                        //Console.WriteLine(Thread.CurrentThread.Name + " point lock not found for index " + index);
                        lock (PointPool)
                        {
                            //Console.WriteLine(Thread.CurrentThread.Name + " point pool not locked. obtaining lock");
                            p = PointPool.FirstOrDefault(x => x.references == 0 && x.pointIndex == -1);
                            if (p == null)
                            {
                                //Console.WriteLine(Thread.CurrentThread.Name + " pooled point lock not found. creating new one");
                                PointPool.Add(p = new PointIndex() { references = 1 });
                                Monitor.Enter(p);

                            }
                            else
                            {
                                Monitor.Enter(p);
                                p.references = 1;//set reference count before point pool releases.
                                //Console.WriteLine(Thread.CurrentThread.Name + " pooled point lock found. ");
                            }
                        }

                        p.pointIndex = index + cornerSpace;
                        //Console.WriteLine(Thread.CurrentThread.Name + " about to lock pointlocks");
                        try
                        {
                            Monitor.Enter(pointLocks);
                            //Console.WriteLine(Thread.CurrentThread.Name + " adding new point lock to active list. index " + index);
                            if (pointLocks.ContainsKey(p.pointIndex))
                            {
                                //Console.WriteLine(Thread.CurrentThread.Name + " apparently two threads are using the same point index " + index);
                                p = pointLocks[p.pointIndex];
                                p.references++;
                            }
                            else
                                pointLocks.Add(p.pointIndex, p);
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine("GetOne has failed for index "+index);
                            //Console.WriteLine(ex);
                        }
                        finally
                        {
                            Monitor.Exit(pointLocks);
                        }

                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("GetOne has failed for index "+index);
                    //Console.WriteLine(ex);
                }
                finally
                {
                    if (Monitor.IsEntered(p))
                        Monitor.Exit(p);
                }

                if (p == null)
                    ;
                else if (p.pointIndex == -1)
                    ;
                return p;
            }

            public void ReleaseOne(PointIndex p)
            {
                //Console.WriteLine(Thread.CurrentThread.Name + " pointlocks about to be locked 1 for index " + p.pointIndex);
                try
                {
                    Monitor.Enter(pointLocks);
                    //Console.WriteLine(Thread.CurrentThread.Name + " pointlocks lock obtained 1. removing index " + p.pointIndex);
                    //Console.WriteLine(Thread.CurrentThread.Name + " point about to be locked 1 for index " + p.pointIndex);
                    try
                    {
                        Monitor.Enter(p);
                        //Console.WriteLine(Thread.CurrentThread.Name + " point lock obtained 1 for index " + p.pointIndex);
                        p.references--;
                        if (p.references == 0)
                        {
                            if (!pointLocks.Remove(p.pointIndex))
                            {
                                throw new Exception("Wut");
                            }
                            p.pointIndex = -1;
                            p.references = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine("GetOne has failed for index " + p?.pointIndex ?? "null");
                        //Console.WriteLine(ex);
                    }
                    finally
                    {
                        if (p != null)
                            Monitor.Exit(p);
                    }
                    //Console.WriteLine(Thread.CurrentThread.Name + " point lock released");
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("GetOne has failed for index "+index);
                    //Console.WriteLine(ex);
                }
                finally
                {
                    Monitor.Exit(pointLocks);
                }
                //Console.WriteLine(Thread.CurrentThread.Name + " pointlocks lock released 1");


            }
        }


        HashSet<int> indexes = new HashSet<int>();
        int adds = 0;
        int removes = 0;
        void WaitForDuplicates(int index)
        {

            lock (indexes) ;
            Console.WriteLine($"Waiting  {index} on thread {Thread.CurrentThread.Name}");
            while (true)
            {
                //lock(indexes)
                if (!indexes.Contains(index)) break;
            }
            adds++;
            lock (indexes)
            {
                Console.WriteLine($"Adding   {index} on thread {Thread.CurrentThread.Name}");
                indexes.Add(index);
            }
        }

        void RemoveFromDuplicates(int index)
        {
            removes++;
            lock (indexes)
            {
                Console.WriteLine($"Removing {index} on thread {Thread.CurrentThread.Name}");
                indexes.Remove(index);
            }
        }
        /// <summary>
        /// a few a frame, relatively rare, but no where near as rare as removes
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="r1">impulse radiation radius</param>
        /// <param name="r2">fuel usage radiation radius</param>
        /// <param name="r3">node creation radiation radius</param>
        /// <param name="o1">impulse intensity</param>
        /// <param name="o2">fuel usage intensity</param>
        /// <param name="o3">node creation intensity</param>
        public void AddPoint(float x, float y, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {
            //r3 and o3 may end up redundant, not there yet
            if (!r1.HasValue) r1 = DefaultImpulseRadius;
            if (!r2.HasValue) r2 = DefaultFuelRadius;
            if (!r3.HasValue) r3 = DefaultNodeCreationRadius;
            if (!o1.HasValue) o1 = DefaultImpulseIntensity;
            if (!o2.HasValue) o2 = DefaultFuelIntensity;
            if (!o3.HasValue) o3 = DefaultNodeCreationIntensity;

            int lp;
            //Console.WriteLine(Thread.CurrentThread.Name + $" BA");
            lock (firstOpenLock)
            {
                lp = firstOpenSpace;
                firstOpenSpace += pointSize;
                //Console.WriteLine(Thread.CurrentThread.Name + $"ADDPT first open point is now {firstOpenSpace}");
                //Console.WriteLine(Thread.CurrentThread.Name + " Adding point index " + pointLock.pointIndex);

                try
                {
                    WaitForDuplicates(lp);
                    //Console.WriteLine(Thread.CurrentThread.Name + $" BB");


                    var n = CreateNeuron(x, y, r1, r2, r3, o1, o2, o3);
                    //Console.WriteLine(Thread.CurrentThread.Name + $" BC");
                    MoveNeuronToActive(n, lp);
                    //Console.WriteLine(Thread.CurrentThread.Name + $" BD");

                    points[lp + 0] = x;//position1
                    points[lp + 1] = y;//position2
                    points[lp + 2] = r1.Value;//size1
                    points[lp + 3] = r2.Value;//size2
                    points[lp + 4] = r3.Value;//size3
                    points[lp + 5] = o1.Value;//opacity1
                    points[lp + 6] = o2.Value;//opacity2
                    points[lp + 7] = o3.Value;//opacity3


                    RemoveFromDuplicates(lp);
                    //Console.WriteLine(Thread.CurrentThread.Name + $" BE");
                }
                catch (Exception ex)
                {
                    ;
                }
                finally
                {
                    if (adds != removes)
                        ;
                }
            }

        }

        /// <summary>
        /// super common, happens a lot. every frame
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="r1">impulse radiation radius</param>
        /// <param name="r2">fuel usage radiation radius</param>
        /// <param name="r3">node creation radiation radius</param>
        /// <param name="o1">impulse intensity</param>
        /// <param name="o2">fuel usage intensity</param>
        /// <param name="o3">node creation intensity</param>
        public void ActivatePoint(Neuron neuron)
        {
            int lp;
            lock (firstOpenLock)
            {
                lp = firstOpenSpace;
                firstOpenSpace += pointSize;
                //Console.WriteLine(Thread.CurrentThread.Name + $"first open point is now {firstOpenSpace}");
                //Console.WriteLine(Thread.CurrentThread.Name + " Adding point index " + pointLock.pointIndex);
            }

            try
            {
                WaitForDuplicates(lp);
                //Console.WriteLine(Thread.CurrentThread.Name + $" BA");


                MoveNeuronToActive(neuron, lp);
                //Console.WriteLine(Thread.CurrentThread.Name + $" BB");

                points[lp + 0] = neuron.X;//position1
                points[lp + 1] = neuron.Y;//position2
                points[lp + 2] = neuron.ImpulseRadius;//size1
                points[lp + 3] = neuron.FuelRadius;//size2
                points[lp + 4] = neuron.NodeCreationRadius;//size3
                points[lp + 5] = neuron.ImpulseIntensity;//opacity1
                points[lp + 6] = neuron.FuelIntensity;//opacity2
                points[lp + 7] = neuron.NodeCreationIntensity;//opacity3


                RemoveFromDuplicates(lp);
                //Console.WriteLine(Thread.CurrentThread.Name + $" BC");
            }
            catch (Exception ex)
            {
                ;
            }
            finally
            {
                if (adds != removes)
                    ;
            }
        }

        /// <summary>
        /// rare, but occasionally done. given that this is presumed rare, it might also be a good place to consolidate memory space
        /// </summary>
        /// <param name="index"></param>
        public void RemovePoint(int index)
        {
            //Console.WriteLine(Thread.CurrentThread.Name + " point index " + index); 
            
            index += cornerSpace;

            try
            {
                WaitForDuplicates(index);

                points[index + 0] = 0;//position1
                points[index + 1] = 0;//position2
                points[index + 2] = 0;//size1
                points[index + 3] = 0;//size2
                points[index + 4] = 0;//size3
                points[index + 5] = 0;//opacity1
                points[index + 6] = 0;//opacity2
                points[index + 7] = 0;//opacity3

                DeleteNeuron(Neurons[index]);

                //consolidate into continuous memory

                //Console.WriteLine(Thread.CurrentThread.Name + " locking first open lock");
                lock (firstOpenLock)//lock end pointer
                {
                    //Console.WriteLine(Thread.CurrentThread.Name + " We want the first thing that still exists to move back and consolidate memory space");
                    int lp = firstOpenSpace - pointSize;
                    //Console.WriteLine(Thread.CurrentThread.Name + $" From {lp} to {index} has obtained fs lock. removed point is {lp}");
                    if (lp == index)
                    {
                        //Console.WriteLine(Thread.CurrentThread.Name + $" AA");
                        //here i am removing the highest point and placing it at the newly removed location. IF the removed point is the last point then this logical path

                        //i bet this fringe case comes with weird locking implications in the first half of this method, but i haven't considered them yet.
                        //fringe case but basically never
                        firstOpenSpace -= pointSize;
                        //Console.WriteLine(Thread.CurrentThread.Name + $" REMOVE first open point is now {firstOpenSpace}");
                        //Console.WriteLine(Thread.CurrentThread.Name + $" AB");
                        return;
                    }
                    else
                    {
                        WaitForDuplicates(lp);
                        //Console.WriteLine(Thread.CurrentThread.Name + $" AC");
                        points[index + 0] = points[lp + 0];//position1
                        points[index + 1] = points[lp + 1];//position2
                        points[index + 2] = points[lp + 2];//size1
                        points[index + 3] = points[lp + 3];//size2
                        points[index + 4] = points[lp + 4];//size3
                        points[index + 5] = points[lp + 5];//opacity1
                        points[index + 6] = points[lp + 6];//opacity2
                        points[index + 7] = points[lp + 7];//opacity3

                        MoveNeuron(lp, index);
                        //Console.WriteLine(Thread.CurrentThread.Name + $" AD");

                        //the last point has been moved back to somewhere else in memory
                        //Console.WriteLine(Thread.CurrentThread.Name + " point index " + lp.pointIndex + " moved to point index " + pointLock.pointIndex);
                        firstOpenSpace -= pointSize;
                        //Console.WriteLine(Thread.CurrentThread.Name + $" first open point is now {firstOpenSpace}");
                        RemoveFromDuplicates(lp);
                    }
                    //normally i would worry about cached references to the old removed point interfering with this new point if they had not yet been locked, but point removal should only happen to points that aren't touched at all for a long time. so no such interaction should occur. may require more consideration later

                }

                RemoveFromDuplicates(index);
                //Console.WriteLine(Thread.CurrentThread.Name + $" AE");

            }
            catch (Exception ex)
            {
                ;
            }
            finally
            {
                if (adds != removes)
                    ;
            }
        }

        /// <summary>
        /// incredibly common. given that this is presumed rare, it might also be a good place to consolidate memory space
        /// </summary>
        /// <param name="index"></param>
        public void DeactivatePoint(int index)
        {
            index += cornerSpace;

            try
            {
                WaitForDuplicates(index);

                points[index + 0] = 0;//position1
                points[index + 1] = 0;//position2
                points[index + 2] = 0;//size1
                points[index + 3] = 0;//size2
                points[index + 4] = 0;//size3
                points[index + 5] = 0;//opacity1
                points[index + 6] = 0;//opacity2
                points[index + 7] = 0;//opacity3

                MoveNeuronToUnused(Neurons[index]);

                //consolidate into continuous memory

                lock (firstOpenLock)//lock end pointer
                {
                    int lp = firstOpenSpace - pointSize;
                    if (lp == index + cornerSpace)
                    {
                        //here i am removing the highest point and placing it at the newly removed location. IF the removed point is the last point then this logical path

                        //i bet this fringe case comes with weird locking implications in the first half of this method, but i haven't considered them yet.
                        //fringe case but basically never
                        firstOpenSpace -= pointSize;
                        return;
                    }
                    else
                    {
                        points[index + 0] = points[lp + 0];//position1
                        points[index + 1] = points[lp + 1];//position2
                        points[index + 2] = points[lp + 2];//size1
                        points[index + 3] = points[lp + 3];//size2
                        points[index + 4] = points[lp + 4];//size3
                        points[index + 5] = points[lp + 5];//opacity1
                        points[index + 6] = points[lp + 6];//opacity2
                        points[index + 7] = points[lp + 7];//opacity3

                        MoveNeuron(lp, index);

                        //the last point has been moved back to somewhere else in memory
                        //Console.WriteLine(Thread.CurrentThread.Name + " point index " + lp.pointIndex + " moved to point index " + pointLock.pointIndex);
                        firstOpenSpace -= pointSize;
                    }
                    //normally i would worry about cached references to the old removed point interfering with this new point if they had not yet been locked, but point removal should only happen to points that aren't touched at all for a long time. so no such interaction should occur. may require more consideration later
                }

                RemoveFromDuplicates(index);
            }
            catch (Exception ex)
            {
                ;
            }
            finally
            {
                if (adds != removes)
                    ;
            }
        }

        public void UpdatePoint(int index, float? x = null, float? y = null, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {
            index += cornerSpace;

            try
            {
                WaitForDuplicates(index);

                if (x.HasValue) points[index + 0] = x.Value;
                if (y.HasValue) points[index + 1] = y.Value;
                if (r1.HasValue) points[index + 2] = r1.Value;
                if (r2.HasValue) points[index + 3] = r2.Value;
                if (r3.HasValue) points[index + 4] = r3.Value;
                if (o1.HasValue) points[index + 5] = o1.Value;
                if (o2.HasValue) points[index + 6] = o2.Value;
                if (o3.HasValue) points[index + 7] = o3.Value;

                if (x.HasValue) Neurons[index].X = x.Value;
                if (y.HasValue) Neurons[index].Y = y.Value;
                if (r1.HasValue) Neurons[index].ImpulseRadius = r1.Value;
                if (r2.HasValue) Neurons[index].FuelRadius = r2.Value;
                if (r3.HasValue) Neurons[index].NodeCreationRadius = r3.Value;
                if (o1.HasValue) Neurons[index].ImpulseIntensity = o1.Value;
                if (o2.HasValue) Neurons[index].FuelIntensity = o2.Value;
                if (o3.HasValue) Neurons[index].NodeCreationIntensity = o3.Value;

                RemoveFromDuplicates(index);
            }
            catch (Exception ex)
            {
                ;
            }
            finally
            {
                if (adds != removes)
                    ;
            }
        }
        public void UpdatePointRel(int index, float? x = null, float? y = null, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {
            index += cornerSpace;

            try
            {
                WaitForDuplicates(index);

                if (x.HasValue) points[index + 0] += x.Value;
                if (y.HasValue) points[index + 1] += y.Value;
                if (r1.HasValue) points[index + 2] += r1.Value;
                if (r2.HasValue) points[index + 3] += r2.Value;
                if (r3.HasValue) points[index + 4] += r3.Value;
                if (o1.HasValue) points[index + 5] += o1.Value;
                if (o2.HasValue) points[index + 6] += o2.Value;
                if (o3.HasValue) points[index + 7] += o3.Value;

                if (x.HasValue) Neurons[index].X += x.Value;
                if (y.HasValue) Neurons[index].Y += y.Value;
                if (r1.HasValue) Neurons[index].ImpulseRadius += r1.Value;
                if (r2.HasValue) Neurons[index].FuelRadius += r2.Value;
                if (r3.HasValue) Neurons[index].NodeCreationRadius += r3.Value;
                if (o1.HasValue) Neurons[index].ImpulseIntensity += o1.Value;
                if (o2.HasValue) Neurons[index].FuelIntensity += o2.Value;
                if (o3.HasValue) Neurons[index].NodeCreationIntensity += o3.Value;

                RemoveFromDuplicates(index);
            }
            catch (Exception ex)
            {
                ;
            }
            finally
            {
                if (adds != removes)
                    ;
            }
        }

    }
}
