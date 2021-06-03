using System;
using System.Collections.Generic;
using System.Text;

namespace FuelMap
{
    public class ReinforcePoints : ManipulatePoints
    {
        public void Reinforce(int index, int force)
        {
            var ilock = PointLocks.GetOne(index);
            lock (ilock)
            {
                points[ilock.pointIndex + 2] += (float)Math.Pow((1 - points[ilock.pointIndex + 2]) / 3d, 2d);
                points[ilock.pointIndex + 3] += (float)Math.Pow((1 - points[ilock.pointIndex + 3]) / 3d, 2d);

                PointLocks.ReleaseOne(ilock);
            }
        }
        public void Fade(int index, int force)
        {
            var ilock = PointLocks.GetOne(index);
            lock (ilock)
            {
                points[ilock.pointIndex + 2] -= (float)Math.Pow((1 - points[ilock.pointIndex + 2]) / 3d, 2d);
                points[ilock.pointIndex + 3] -= (float)Math.Pow((1 - points[ilock.pointIndex + 3]) / 3d, 2d);

                PointLocks.ReleaseOne(ilock);
            }
        }
    }
}
