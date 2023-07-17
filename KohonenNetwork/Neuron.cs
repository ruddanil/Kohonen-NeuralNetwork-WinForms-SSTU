using System;

namespace KohonenNetwork
{
    class Neuron
    {
        private readonly int _x;
        private readonly int _y;
        public double RWeight;
        public double GWeight;
        public double BWeight;
        public int GroupId;

        public Neuron(int x, int y, int r, int g, int b)
        {
            _x = x;
            _y = y;
            _ = new Random(23);
            RWeight = r;
            GWeight = g;
            BWeight = b;
        }

        public int GetX()
        {
            return _x;
        }

        public int GetY()
        {
            return _y;
        }

        // Distance between the neuron and the transmitted vector
        public double CheckDistance(Vector input)
        {
            double distance = 0;

            distance += Math.Pow(input.Red - RWeight, 2) + Math.Pow(input.Green - GWeight, 2) + Math.Pow(input.Blue - BWeight, 2);

            return Math.Sqrt(distance);
        }

        public void UpdateNodeWeights(Vector input, double lrInf)
        {
            RWeight += lrInf * (input.Red - RWeight);
            GWeight += lrInf * (input.Green - GWeight);
            BWeight += lrInf * (input.Blue - BWeight);
        }
    }
}
