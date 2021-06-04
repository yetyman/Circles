using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FuelMap
{
    public class ManipulatePoints
    {
        //i believe that most functionality here may eventually be parallized into a shader, compute shader or otherwise.
        //for now, for clarity, i'll use c# and update the buffered data as i have been, 
        //this will like be the first bottleneck to overcome as the graph becomes functional

        private const float half = .5f;
        private const float circleScale = half;

        public float[] points;
        public int firstOpenSpace = 0;
        public object firstOpenLock = new object();


        public const float MinimumRadius = .01f;
        public const float MinimumIntensity = .0001f;
        public float DefaultImpulseRadius = MinimumRadius;
        public float DefaultFuelRadius = MinimumRadius;
        public float DefaultNodeCreationRadius = MinimumRadius;
        public float DefaultImpulseIntensity = MinimumIntensity;
        public float DefaultFuelIntensity = MinimumIntensity;
        public float DefaultNodeCreationIntensity = MinimumIntensity;

        public static int cornerSpace => circleCorners.Length;
        public int allocatedSpace => points.Length;
        public int pointCount => (firstOpenSpace - cornerSpace - 1) / 8;
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
            lock (firstOpenLock)
            {
                //include the corners array in buffered data
                var actualLength = cornerSpace + maxPointCount * 8;

                points = new float[actualLength];

                //copy corners to buffered data. probably static, but we'll see
                for (int i = 0; i < cornerSpace; i++)
                    points[i] = circleCorners[i];

                firstOpenSpace = cornerSpace;
            }
        }

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
        void WaitforDuplicates(int index)
        {
            while (indexes.Contains(index)) ;
            lock(indexes)
                indexes.Add(index);
        }
        void RemoveFromDuplicates(int index)
        {
            lock(indexes)
                indexes.Remove(index);
        }
        /// <summary>
        /// 
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
            lock (firstOpenLock)
            {
                lp = firstOpenSpace;
                firstOpenSpace += 8;
                //Console.WriteLine(Thread.CurrentThread.Name + " Adding point index " + pointLock.pointIndex);
            }

            WaitforDuplicates(lp);

            points[lp + 0] = x;//p sition1
            points[lp + 1] = y;//position2
            points[lp + 2] = r1.Value;//size1
            points[lp + 3] = r2.Value;//size2
            points[lp + 4] = r3.Value;//size3
            points[lp + 5] = o1.Value;//opacity1
            points[lp + 6] = o2.Value;//opacity2
            points[lp + 7] = o3.Value;//opacity3

            RemoveFromDuplicates(lp);
        }

        /// <summary>
        /// rare, but occasionally done. given that this is presumed rare, it might also be a good place to consolidate memory space
        /// </summary>
        /// <param name="index"></param>
        public void RemovePoint(int index)
        {
            index += cornerSpace;
            
            WaitforDuplicates(index);

            points[index + 0] = 0;//p sition1
            points[index + 1] = 0;//position2
            points[index + 2] = 0;//size1
            points[index + 3] = 0;//size2
            points[index + 4] = 0;//size3
            points[index + 5] = 0;//opacity1
            points[index + 6] = 0;//opacity2
            points[index + 7] = 0;//opacity3

            //consolidate into continuous memory

            lock (firstOpenLock)//lock end pointer
            {
                int lp = firstOpenSpace - 8;
                if (lp == index + cornerSpace)
                {
                    //here i am removing the highest point and placing it at the newly removed location. IF the removed point is the last point then this logical path

                    //i bet this fringe case comes with weird locking implications in the first half of this method, but i haven't considered them yet.
                    //fringe case but basically never
                    firstOpenSpace -= 8;
                    return;
                }
                else
                {
                    points[index + 0] = points[lp + 0];//p sition1
                    points[index + 1] = points[lp + 1];//position2
                    points[index + 2] = points[lp + 2];//size1
                    points[index + 3] = points[lp + 3];//size2
                    points[index + 4] = points[lp + 4];//size3
                    points[index + 5] = points[lp + 5];//opacity1
                    points[index + 6] = points[lp + 6];//opacity2
                    points[index + 7] = points[lp + 7];//opacity3
                    //the last point has been moved back to somewhere else in memory
                                                       //Console.WriteLine(Thread.CurrentThread.Name + " point index " + lp.pointIndex + " moved to point index " + pointLock.pointIndex);
                    firstOpenSpace -= 8;
                }
                //normally i would worry about cached references to the old removed point interfering with this new point if they had not yet been locked, but point removal should only happen to points that aren't touched at all for a long time. so no such interaction should occur. may require more consideration later
            }

            RemoveFromDuplicates(index);
        }

        public void UpdatePoint(int index, float? x = null, float? y = null, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {
            index += cornerSpace;
            
            WaitforDuplicates(index);

            if (x.HasValue) points[index + 0] = x.Value;
            if (y.HasValue) points[index + 1] = y.Value;
            if (r1.HasValue) points[index + 2] = r1.Value;
            if (r2.HasValue) points[index + 3] = r2.Value;
            if (r3.HasValue) points[index + 4] = r3.Value;
            if (o1.HasValue) points[index + 5] = o1.Value;
            if (o2.HasValue) points[index + 6] = o2.Value;
            if (o3.HasValue) points[index + 7] = o3.Value;

            RemoveFromDuplicates(index);
        }
        public void UpdatePointRel(int index, float? x = null, float? y = null, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {
            index += cornerSpace;
            
            WaitforDuplicates(index);

            if (x.HasValue) points[index + 0] += x.Value;
            if (y.HasValue) points[index + 1] += y.Value;
            if (r1.HasValue) points[index + 2] += r1.Value;
            if (r2.HasValue) points[index + 3] += r2.Value;
            if (r3.HasValue) points[index + 4] += r3.Value;
            if (o1.HasValue) points[index + 5] += o1.Value;
            if (o2.HasValue) points[index + 6] += o2.Value;
            if (o3.HasValue) points[index + 7] += o3.Value;
            
            RemoveFromDuplicates(index);
        }

    }
}
