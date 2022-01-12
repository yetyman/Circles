using System;
using System.Collections.Generic;
using System.Text;

namespace NodeDirectedFuelMap
{
    public class ReinforcePoints : ManipulatePoints
    {
        public void Reinforce(int index, int force)
        {
                Points[index + 2] += (float)Math.Pow((1 - Points[index + 2]) / 3d, 2d);
                Points[index + 3] += (float)Math.Pow((1 - Points[index + 3]) / 3d, 2d);
        }
        public void Fade(int index, int force)
        {
                Points[index + 2] -= (float)Math.Pow((1 - Points[index + 2]) / 3d, 2d);
                Points[index + 3] -= (float)Math.Pow((1 - Points[index + 3]) / 3d, 2d);
        }
    }
}
