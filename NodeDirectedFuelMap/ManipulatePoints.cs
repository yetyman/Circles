using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace NodeDirectedFuelMap
{
    public class ManipulatePoints
    {
        //i believe that most functionality here may eventually be parallized into a shader, compute shader or otherwise.
        //for now, for clarity, i'll use c# and update the buffered data as i have been, 
        //this will like be the first bottleneck to overcome as the graph becomes functional


        public float[] Points;
        public float[] InactivePoints;
        public Neuron[] ActiveNeurons;
        public Neuron[] InactiveNeurons;
        protected int firstOpenSpace = 0;
        public int firstOpenInactiveSpace = 0;


        public const float MinimumRadius = .01f;
        public const float MinimumIntensity = .0001f;
        public const float DefaultImpulseRadius = MinimumRadius;
        public const float DefaultFuelRadius = MinimumRadius;
        public const float DefaultNodeCreationRadius = MinimumRadius;
        public const float DefaultImpulseIntensity = MinimumIntensity;
        public const float DefaultFuelIntensity = MinimumIntensity;
        public const float DefaultNodeCreationIntensity = MinimumIntensity;

        public const int pointSize = 8;
        public int allocatedSpace => Points.Length;
        public int PointCount => (firstOpenSpace - 1) / pointSize;

        private int maxActiveCount;
        private int maxUnusedCount;
        private int maxPointCount;
        /// <summary>
        /// allocate total possible memory from the get go for all points
        /// </summary>
        /// <param name="maxPointCount"></param>
        public void Allocate(int maxPointCount)
        {
            this.maxPointCount = maxPointCount;
            maxActiveCount = maxPointCount / 100;
            maxUnusedCount = maxPointCount;

            var actualLength = maxActiveCount * pointSize;
            var actualMaxLength = maxUnusedCount * pointSize;

            Points = new float[actualLength];
            InactivePoints = new float[actualMaxLength];
            ActiveNeurons = new Neuron[actualLength];//empty pointers in the array are fine. they cost a lot less than translation math for every change for every frame
            InactiveNeurons = new Neuron[maxUnusedCount];//empty pointers in the array are fine. they probably cost a lot less than translation math for every change for every frame
            for (int i = 0; i < maxPointCount; i++)
                NeuronPool.Push(new Neuron() { UniqueId = NeuronIDs++ });

            //change inactive neurons to an array and start sending all of it to the gfx card every frame instead of just the active neurons. this will simplify the line shader, but also bring the math closer to what the shader version will need
            //abstract this line class to simplify that. this is an easy one to abstract and abstract should be compile time so no added cost

            Console.WriteLine($"first open point is now {firstOpenSpace}");
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
            neuron.pointIndex = firstOpenInactiveSpace;

            //set inactive neuron but also set values in the array that we need
            InactiveNeurons[firstOpenInactiveSpace] = neuron;

            InactivePoints[firstOpenInactiveSpace] = neuron.X;
            InactivePoints[firstOpenInactiveSpace+1] = neuron.Y;
            //InactivePoints[firstOpenInactiveSpace+2] = neuron.ImpulseRadius;//probably don't need to update these since inactive point's radiation wont matter
            //InactivePoints[firstOpenInactiveSpace+3] = neuron.FuelRadius;
            //InactivePoints[firstOpenInactiveSpace+4] = neuron.NodeCreationRadius;
            //InactivePoints[firstOpenInactiveSpace+5] = neuron.ImpulseIntensity;
            //InactivePoints[firstOpenInactiveSpace+6] = neuron.FuelIntensity;
            //InactivePoints[firstOpenInactiveSpace+7] = neuron.NodeCreationIntensity;

            firstOpenInactiveSpace += pointSize;

        }
        public void MoveNeuronToActive(Neuron neuron, int index)
        {
            firstOpenInactiveSpace -= pointSize;
            //remove from object cache
            //replace with first open
            InactiveNeurons[neuron.pointIndex] = InactiveNeurons[firstOpenInactiveSpace];//-maxActiveCount]
            InactiveNeurons[neuron.pointIndex].pointIndex = neuron.pointIndex; 
            //remove from primitive cache
            //replace with first open
            InactivePoints[neuron.pointIndex] = InactivePoints[firstOpenInactiveSpace];
            InactivePoints[neuron.pointIndex + 1] = InactivePoints[firstOpenInactiveSpace + 1];

            InactiveNeurons[firstOpenInactiveSpace] = null;

            neuron.pointIndex = index;
            ActiveNeurons[index] = neuron;
        }
        public void MoveNewNeuronToActive(Neuron neuron, int index)
        {
            neuron.pointIndex = index;
            ActiveNeurons[index] = neuron;
        }
        public void DeleteNeuron(Neuron n)
        {
            firstOpenInactiveSpace -= pointSize;
            InactiveNeurons[n.pointIndex] = InactiveNeurons[firstOpenInactiveSpace];
            InactiveNeurons[n.pointIndex].pointIndex = n.pointIndex;

            InactivePoints[n.pointIndex] = InactivePoints[firstOpenInactiveSpace];
            InactivePoints[n.pointIndex + 1] = InactivePoints[firstOpenInactiveSpace + 1];

            InactiveNeurons[firstOpenInactiveSpace] = null;
        }
        private static int NeuronIDs = 0;
        private Stack<Neuron> NeuronPool = new Stack<Neuron>();
        public Neuron CreateNeuron(float x, float y, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {

            if (!r1.HasValue) r1 = DefaultImpulseRadius;
            if (!r2.HasValue) r2 = DefaultFuelRadius;
            if (!r3.HasValue) r3 = DefaultNodeCreationRadius;
            if (!o1.HasValue) o1 = DefaultImpulseIntensity;
            if (!o2.HasValue) o2 = DefaultFuelIntensity;
            if (!o3.HasValue) o3 = DefaultNodeCreationIntensity;

            Neuron n;
            if (NeuronPool.Any())
                n = NeuronPool.Pop();
            else n = new Neuron() { UniqueId = NeuronIDs++ };

            n.X = x;
            n.Y = y;
            n.ImpulseRadius = r1.Value;
            n.FuelRadius = r2.Value;
            n.NodeCreationRadius = r3.Value;
            n.ImpulseIntensity = o1.Value;
            n.FuelIntensity = o2.Value;
            n.NodeCreationIntensity = o3.Value;

            return n;
        }
        public void MoveNeuron(int from, int to)
        {
            Neuron n;

            n = ActiveNeurons[from];
            ActiveNeurons[from] = null;//whenever this location is nulled, the data array ought to also be null at this location.
                
            ActiveNeurons[to] = n;
            ActiveNeurons[to].pointIndex = to;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPoint(float x, float y, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {
            //r3 and o3 may end up redundant, not there yet
            if (!r1.HasValue) r1 = DefaultImpulseRadius;
            if (!r2.HasValue) r2 = DefaultFuelRadius;
            if (!r3.HasValue) r3 = DefaultNodeCreationRadius;
            if (!o1.HasValue) o1 = DefaultImpulseIntensity;
            if (!o2.HasValue) o2 = DefaultFuelIntensity;
            if (!o3.HasValue) o3 = DefaultNodeCreationIntensity;

            int lp = firstOpenSpace;
            firstOpenSpace += pointSize;

            var n = CreateNeuron(x, y, r1, r2, r3, o1, o2, o3);
            MoveNewNeuronToActive(n, lp);

            Points[lp + 0] = x;//position1
            Points[lp + 1] = y;//position2
            Points[lp + 2] = r1.Value;//size1
            Points[lp + 3] = r2.Value;//size2
            Points[lp + 4] = r3.Value;//size3
            Points[lp + 5] = o1.Value;//opacity1
            Points[lp + 6] = o2.Value;//opacity2
            Points[lp + 7] = o3.Value;//opacity3
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ActivatePoint(Neuron neuron)
        {
            int lp = firstOpenSpace;
            firstOpenSpace += pointSize;

            MoveNeuronToActive(neuron, lp);

            Points[lp + 0] = neuron.X;//position1
            Points[lp + 1] = neuron.Y;//position2
            Points[lp + 2] = neuron.ImpulseRadius;//size1
            Points[lp + 3] = neuron.FuelRadius;//size2
            Points[lp + 4] = neuron.NodeCreationRadius;//size3
            Points[lp + 5] = neuron.ImpulseIntensity;//opacity1
            Points[lp + 6] = neuron.FuelIntensity;//opacity2
            Points[lp + 7] = neuron.NodeCreationIntensity;//opacity3

        }

        /// <summary>
        /// rare, but occasionally done. given that this is presumed rare, it might also be a good place to consolidate memory space
        /// </summary>
        /// <param name="index"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemovePoint(int index)
        {
            DeleteNeuron(ActiveNeurons[index]);
        }

        /// <summary>
        /// incredibly common. given that this is presumed rare, it might also be a good place to consolidate memory space
        /// </summary>
        /// <param name="index"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DeactivatePoint(int index)
        {
            MoveNeuronToUnused(ActiveNeurons[index]);

            //consolidate into continuous memory

            int lp = firstOpenSpace - pointSize;
            if (lp == index)
            {

                //weird optimisation, i can get away with only setting size or opacity to zero here. its never checked, we just don't want it to do anything in the next rendering pass
                //points[index + 0] = 0;//position1
                //points[index + 1] = 0;//position2
                Points[index + 2] = 0;//size1
                Points[index + 3] = 0;//size2
                Points[index + 4] = 0;//size3
                //points[index + 5] = 0;//opacity1
                //points[index + 6] = 0;//opacity2
                //points[index + 7] = 0;//opacity3
 
                
                //here i am removing the highest point and placing it at the newly removed location. IF the removed point is the last point then this logical path

                //i bet this fringe case comes with weird locking implications in the first half of this method, but i haven't considered them yet.
                //fringe case but basically never
            }
            else
            {
                Points[index + 0] = Points[lp + 0];//position1
                Points[index + 1] = Points[lp + 1];//position2
                Points[index + 2] = Points[lp + 2];//size1
                Points[index + 3] = Points[lp + 3];//size2
                Points[index + 4] = Points[lp + 4];//size3
                Points[index + 5] = Points[lp + 5];//opacity1
                Points[index + 6] = Points[lp + 6];//opacity2
                Points[index + 7] = Points[lp + 7];//opacity3

                MoveNeuron(lp, index);

                //the last point has been moved back to somewhere else in memory
            }
            firstOpenSpace -= pointSize;
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdatePoint(int index, float r1, float r2, float r3, float o1, float o2, float o3)
        {
            Points[index + 2] = r1;
            Points[index + 3] = r2;
            Points[index + 4] = r3;
            Points[index + 5] = o1;
            Points[index + 6] = o2;
            Points[index + 7] = o3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MovePoint(int index, float x, float y)
        {
            Points[index + 0] = x;
            Points[index + 1] = y;
        }
        public void UpdatePointRel(int index, float? x = null, float? y = null, float? r1 = null, float? r2 = null, float? r3 = null, float? o1 = null, float? o2 = null, float? o3 = null)
        {
            if (x.HasValue) Points[index + 0] += x.Value;
            if (y.HasValue) Points[index + 1] += y.Value;
            if (r1.HasValue) Points[index + 2] += r1.Value;
            if (r2.HasValue) Points[index + 3] += r2.Value;
            if (r3.HasValue) Points[index + 4] += r3.Value;
            if (o1.HasValue) Points[index + 5] += o1.Value;
            if (o2.HasValue) Points[index + 6] += o2.Value;
            if (o3.HasValue) Points[index + 7] += o3.Value;
        }

    }
}
