using System;
using System.Collections.Generic;
using System.Text;

namespace FuelMap
{
    class ManipulatePoints
    {
        //i believe that most functionality here may eventually be parallized into a shader, compute shader or otherwise.
        //for now, for clarity, i'll use c# and update the buffered data as i have been, 
        //this will like be the first bottleneck to overcome as the graph becomes functional

        private const float half = .5f;
        private const float circleScale = half;

        public float[] points;
        public int firstOpenSpace = 0;
        public object firstOpenLock = new object();

        public HashSet<PointIndex> pointLocks = new HashSet<PointIndex>();

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

                firstOpenSpace = 0;
            }
        }

        /// <summary>
        /// just wrapping for reference passing
        /// </summary>
        public class PointIndex
        {
            public int pointIndex;
            public int references = 1;
            public override int GetHashCode()
            {
                return pointIndex.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                return obj is PointIndex p && p.pointIndex == pointIndex || obj is int i && i+cornerSpace == pointIndex;
            }
        }

        public PointIndex GetPointLock(int index)
        {
            if (pointLocks.TryGetValue(new PointIndex() { pointIndex = index+cornerSpace }, out PointIndex p))
            {
                p.references++;
                return p;
            }
            else
            {
                var newguy = new PointIndex() { pointIndex = index+cornerSpace };
                pointLocks.Add(newguy);
                return newguy;
            }
        }

        public void ReleasePointLock(PointIndex p)
        {
            p.references--;
            if (p.references == 0)
                pointLocks.Remove(p);
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

            PointIndex pointLock;
            lock (firstOpenLock)
            {
                var lp = firstOpenSpace;
                firstOpenSpace += 8;
                pointLock = GetPointLock(lp);
            }
            lock (pointLock)
            {
                if (pointLock.pointIndex != -1)
                {
                    points[pointLock.pointIndex + 0] = x;//p sition1
                    points[pointLock.pointIndex + 1] = y;//position2
                    points[pointLock.pointIndex + 2] = r1.Value;//size1
                    points[pointLock.pointIndex + 3] = r2.Value;//size2
                    points[pointLock.pointIndex + 4] = r3.Value;//size3
                    points[pointLock.pointIndex + 5] = o1.Value;//opacity1
                    points[pointLock.pointIndex + 6] = o2.Value;//opacity2
                    points[pointLock.pointIndex + 7] = o3.Value;//opacity3
                    ReleasePointLock(pointLock);
                }
            }
        }

        /// <summary>
        /// rare, but occasionally done. given that this is presumed rare, it might also be a good place to consolidate memory space
        /// </summary>
        /// <param name="index"></param>
        public void RemovePoint(int index)
        {
            PointIndex pointLock;
            PointIndex lp;
            //clear this point
            pointLock = GetPointLock(index);
            lock (pointLock)
            {
                if (pointLock.pointIndex != -1)
                {
                    points[pointLock.pointIndex + 0] = 0;//p sition1
                    points[pointLock.pointIndex + 1] = 0;//position2
                    points[pointLock.pointIndex + 2] = 0;//size1
                    points[pointLock.pointIndex + 3] = 0;//size2
                    points[pointLock.pointIndex + 4] = 0;//size3
                    points[pointLock.pointIndex + 5] = 0;//opacity1
                    points[pointLock.pointIndex + 6] = 0;//opacity2
                    points[pointLock.pointIndex + 7] = 0;//opacity3
                    pointLock.pointIndex = -1;

                    pointLock.references = 0;
                    ReleasePointLock(pointLock);
                }
            }

            //consolidate into continuous memory
            pointLock = GetPointLock(index);
            lock (pointLock)//lock empty space
            {
                lock (firstOpenLock)//lock end pointer
                {
                    lp = GetPointLock(firstOpenSpace - 8);
                    lock (lp)//lock last point
                    {
                        if (lp.pointIndex == index+cornerSpace)
                        {
                            //i bet this fringe case comes with weird locking implications in the first half of this method, but i haven't considered them yet.
                            //fringe case but basically never
                            firstOpenSpace -= 8;
                            return;
                        }
                        else
                        {
                            points[pointLock.pointIndex + 0] = points[lp.pointIndex + 0];//p sition1
                            points[pointLock.pointIndex + 1] = points[lp.pointIndex + 1];//position2
                            points[pointLock.pointIndex + 2] = points[lp.pointIndex + 2];//size1
                            points[pointLock.pointIndex + 3] = points[lp.pointIndex + 3];//size2
                            points[pointLock.pointIndex + 4] = points[lp.pointIndex + 4];//size3
                            points[pointLock.pointIndex + 5] = points[lp.pointIndex + 5];//opacity1
                            points[pointLock.pointIndex + 6] = points[lp.pointIndex + 6];//opacity2
                            points[pointLock.pointIndex + 7] = points[lp.pointIndex + 7];//opacity3
                                                                          //the last point has been moved back to somewhere else in memory

                            firstOpenSpace -= 8;
                        }
                        lp.pointIndex = index+cornerSpace;
                        //normally i would worry about cached references to the old removed point interfering with this new point if they had not yet been locked, but point removal should only happen to points that aren't touched at all for a long time. so no such interaction should occur. may require more consideration later
                    }
                }
                ReleasePointLock(pointLock);
            }
        }

        public void UpdatePoint(int index, float? x = null, float? y = null, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {
            var pointLock = GetPointLock(index);
            lock (pointLock)
            {
                if (x.HasValue) points[pointLock.pointIndex + 0] = x.Value;
                if (y.HasValue) points[pointLock.pointIndex + 1] = y.Value;
                if (r1.HasValue) points[pointLock.pointIndex + 2] = r1.Value;
                if (r2.HasValue) points[pointLock.pointIndex + 3] = r2.Value;
                if (r3.HasValue) points[pointLock.pointIndex + 4] = r3.Value;
                if (o1.HasValue) points[pointLock.pointIndex + 5] = o1.Value;
                if (o2.HasValue) points[pointLock.pointIndex + 6] = o2.Value;
                if (o3.HasValue) points[pointLock.pointIndex + 7] = o3.Value;

                ReleasePointLock(pointLock);
            }
        }
        public void UpdatePointRel(int index, float? x = null, float? y = null, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {
            var pointLock = GetPointLock(index);
            lock (pointLock)
            {
                if (x.HasValue) points[pointLock.pointIndex + 0] += x.Value;
                if (y.HasValue) points[pointLock.pointIndex + 1] += y.Value;
                if (r1.HasValue) points[pointLock.pointIndex + 2] += r1.Value;
                if (r2.HasValue) points[pointLock.pointIndex + 3] += r2.Value;
                if (r3.HasValue) points[pointLock.pointIndex + 4] += r3.Value;
                if (o1.HasValue) points[pointLock.pointIndex + 5] += o1.Value;
                if (o2.HasValue) points[pointLock.pointIndex + 6] += o2.Value;
                if (o3.HasValue) points[pointLock.pointIndex + 7] += o3.Value;

                ReleasePointLock(pointLock);
            }
        }

    }
}
