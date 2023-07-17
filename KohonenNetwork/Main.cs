using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace KohonenNetwork
{
    public partial class Main : Form
    {
        delegate void UpdateControlDelegate(Control control, string propertyName, object propertyValue);

        public double LearningRate = 0.1;
        public static int Neighbourhood = 24;
        private int _maxIteration = 1000;

        private readonly List<Vector> _inputList = new List<Vector>();
        private readonly List<Neuron> _neuronList = new List<Neuron>();
        List<Neuron> _neuronListCopy = new List<Neuron>();

        private readonly int[,] _neuronPosition = new int[50, 50];

        bool _isMapWorking = false;
        int _numberOfIterations = 0;
        public double NeighbourhoodPrecise = Neighbourhood;

        public Main()
        {
            InitializeComponent();
            text_iterations.Text = _maxIteration.ToString();
            text_learning.Text = LearningRate.ToString();
            text_neigh.Text = Neighbourhood.ToString();
            neighbourLabel.Text = "-";
            learningLabel.Text = "-";
            iterCount.Text = "-";
        }

        private void B_start_Click(object sender, EventArgs e)
        {
            Thread iter = new Thread(Iterations);

            if (_isMapWorking == true) _isMapWorking = false;

            Thread.Sleep(150);

            if (text_learning.Text == "") return;
            if (text_neigh.Text == "") return;
            if (text_iterations.Text == "") return;

            GenerateInput();
            GenerateNeurons();
            DrawMapInit();

            Thread.Sleep(150);

            LearningRate = Convert.ToDouble(text_learning.Text.Replace('.', ','));
            Neighbourhood = Convert.ToInt32(text_neigh.Text);
            _maxIteration = Convert.ToInt32(text_iterations.Text);
            _numberOfIterations = 0;
            _isMapWorking = true;
            iter.Start();
        }

        // Generating input data
        private void GenerateInput()
        {
            _inputList.Clear();
            Random rand = new Random();
            int rweight = 1;
            int gweight = 1;
            int bweight = rand.Next(0, 255); ;
            int x = 0;
            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    rweight = (bweight + rweight * x + x) % 255;
                    gweight = (rweight + gweight * x / (x + 1)) % 255;
                    bweight = (gweight + bweight * x / (x + 3)) % 255;
                    _inputList.Add(new Vector(rweight, gweight, bweight));

                    x++;
                }
            }
        }

        private void GenerateNeurons()
        {
            _neuronList.Clear();
            Random rand = new Random(23154846);
            int x = 0;

            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    int rweight = rand.Next(0, 255);
                    int gweight = rand.Next(0, 255);
                    int bweight = rand.Next(0, 255);

                    _neuronPosition[i, j] = x++;
                    _neuronList.Add(new Neuron(j, i, rweight, gweight, bweight));
                }
            }
        }

        private void DrawMapInit()
        {
            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    int red = Convert.ToInt32(_neuronList.ElementAt(_neuronPosition[i, j]).RWeight);
                    int green = Convert.ToInt32(_neuronList.ElementAt(_neuronPosition[i, j]).GWeight);
                    int blue = Convert.ToInt32(_neuronList.ElementAt(_neuronPosition[i, j]).BWeight);

                    SolidBrush myBrush = new SolidBrush(Color.FromArgb(red, green, blue));
                    CreateGraphics().FillRectangle(myBrush, new Rectangle(10 + (10 * i), 10 + (10 * j), 10, 10));
                }
            }
        }
        private void Iterations()
        {
            if (_isMapWorking == false)
            {
                _neuronListCopy = _neuronList;
                return;
            }

            Vector inputVector = _inputList.ElementAt(PickRandomInput());

            int winner = WinnerOfIteration(inputVector);

            TrainNetwork(winner, inputVector);

            UpdateMap(_neuronList.ElementAt(winner).GetX(), _neuronList.ElementAt(winner).GetY());

            LearningRateDecay();
            NeighbourhoodDecay();

            Thread iterationNumber = new Thread(UpdateMeta);
            iterationNumber.Start();
            if (_numberOfIterations <= _maxIteration)
            {
                _numberOfIterations++;
                Iterations();
            }

            UpdateControl(b_Kmeans, "Enabled", true);
            _neuronListCopy = _neuronList;
        }

        // Calculation of the neuron with the minimum distance to the input vector
        private int WinnerOfIteration(Vector input)
        {
            int id = 0;
            double activeWinner = 999999;
            foreach (Neuron neuron in _neuronList)
            {
                double distance = neuron.CheckDistance(input);
                if (activeWinner > distance)
                {
                    activeWinner = distance;
                    int x = neuron.GetX();
                    int y = neuron.GetY();
                    id = _neuronPosition[x, y];
                }
            }
            return id;
        }

        // Changing weights in a rectangular area centered at the transmitted point
        private void UpdateMap(int x, int y)
        {
            int i = x - Neighbourhood;
            if (i < 0) i = 0;

            int ii = x + Neighbourhood;
            if (ii > 50) ii = 50;

            int kj = y - Neighbourhood;
            if (kj < 0) kj = 0;

            int jj = y + Neighbourhood;
            if (jj > 50) jj = 50;

            for (i += 0; i < ii; i++)
            {
                for (int j = kj; j < jj; j++)
                {
                    int red = Convert.ToInt32(_neuronList.ElementAt(_neuronPosition[i, j]).RWeight);
                    int green = Convert.ToInt32(_neuronList.ElementAt(_neuronPosition[i, j]).GWeight);
                    int blue = Convert.ToInt32(_neuronList.ElementAt(_neuronPosition[i, j]).BWeight);

                    SolidBrush myBrush = new SolidBrush(Color.FromArgb(red, green, blue));
                    CreateGraphics().FillRectangle(myBrush, new Rectangle(10 + (10 * i), 10 + (10 * j), 10, 10));
                }
            }
        }

        private int PickRandomInput()
        {
            Random index = new Random();
            int input = index.Next(0, 2499);
            return input;
        }

        private void TrainNetwork(int winner, Vector inputVector)
        {
            // Defining the boundaries of the area of updating weights
            int x = _neuronList.ElementAt(winner).GetX();
            int y = _neuronList.ElementAt(winner).GetY();
            int i = x - Neighbourhood;
            int ii = x + Neighbourhood;
            int jTemp = y - Neighbourhood;
            int jj = y + Neighbourhood;

            if (i < 0) i = 0;
            if (ii > 50) ii = 50;
            if (jTemp < 0) jTemp = 0;
            if (jj > 50) jj = 50;

            for (i += 0; i < ii; i++)
            {
                for (int j = jTemp; j < jj; j++)
                {
                    double lrInf = LearningRate * CalculateInfluence(x, y, i, j);
                    _neuronList.ElementAt(_neuronPosition[i, j]).UpdateNodeWeights(inputVector, lrInf);
                }
            }
        }

        // Reducing the radius of the neighborhood area
        public void NeighbourhoodDecay()
        {
            NeighbourhoodPrecise = (double)Neighbourhood * Math.Exp(-(double)_numberOfIterations / 2 / (double)_maxIteration);
            Neighbourhood = (int)Math.Ceiling(NeighbourhoodPrecise);
        }

        // Reducing the learning rate
        public void LearningRateDecay()
        {
            LearningRate *= Math.Exp(-(double)_numberOfIterations / 55 / (double)_maxIteration);
        }

        // We calculate the influence of neighboring neurons on the winning neuron
        public double CalculateInfluence(int winnerX, int winnerY, int idX, int idY)
        {
            double distX = winnerX - idX;
            double distY = winnerY - idY;
            double inf = -(((distX * distX) + (distY * distY))) / (2 * (Neighbourhood * Neighbourhood));

            return Math.Exp(inf);
        }

        private void B_cancel_Click(object sender, EventArgs e)
        {
            _isMapWorking = false;
            Thread.Sleep(150);
        }

        private void B_Kmeans_Click(object sender, EventArgs e)
        {
            try
            {
                Convert.ToInt32(text_groups.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Enter the number of output groups");
                return;
            }
            try
            {
                UpdateControl(b_Kmeans, "Enabled", false);
                Kmeans groups = new Kmeans();
                Random rand = new Random();
                groups.Clusters.Clear();

                // Selection of random color values from the source data
                for (int i = 0; i < Convert.ToInt32(text_groups.Text); i++)
                {
                    int seed = rand.Next(0, 2499);
                    int r = (int)_neuronListCopy.ElementAt(seed).RWeight;
                    int g = (int)_neuronListCopy.ElementAt(seed).GWeight;
                    int b = (int)_neuronListCopy.ElementAt(seed).BWeight;
                    groups.Clusters.Add(new Vector(r, g, b));
                }

                for (int i = 0; i < 50; i++)
                {
                    SetClusters(_neuronListCopy, groups);
                    UpdateCentroids(_neuronListCopy, groups);
                }

                foreach (Neuron node in _neuronListCopy)
                {
                    node.RWeight = groups.Clusters.ElementAt(node.GroupId).Red;
                    node.GWeight = groups.Clusters.ElementAt(node.GroupId).Green;
                    node.BWeight = groups.Clusters.ElementAt(node.GroupId).Blue;
                }

                DrawGroups(_neuronListCopy);
            }
            catch (Exception)
            {
                b_Kmeans.Enabled = true;
                MessageBox.Show("Clustering error");
            }
        }

        // Assigning a group identifier to each neuron
        private void SetClusters(List<Neuron> neuronListCopy, Kmeans groups)
        {
            int groupId = 0;
            foreach (Neuron node in neuronListCopy)
            {
                double minDistance = Double.MaxValue;

                for (int i = 0; i < groups.Clusters.Count(); i++)
                {
                    double currDistance = node.CheckDistance(groups.Clusters.ElementAt(i));
                    if (minDistance > currDistance)
                    {
                        minDistance = currDistance;
                        groupId = i;
                    }
                }
                node.GroupId = groupId;
            }
        }

        private void UpdateCentroids(List<Neuron> neuronListCopy, Kmeans groups)
        {
            for (int i = 0; i < groups.Clusters.Count(); i++)
            {
                int countNodes = 0;

                Vector meanVector = new Vector(0, 0, 0);

                foreach (Neuron node in neuronListCopy.Where(node => node.GroupId == i))
                {
                    meanVector.Red += Convert.ToInt32(node.RWeight);
                    meanVector.Green += Convert.ToInt32(node.GWeight);
                    meanVector.Blue += Convert.ToInt32(node.BWeight);

                    countNodes++;
                }
                meanVector.Red = (int)Math.Round((double)meanVector.Red / countNodes);
                meanVector.Green = (int)Math.Round((double)meanVector.Green / countNodes);
                meanVector.Blue = (int)Math.Round((double)meanVector.Blue / countNodes);

                groups.Clusters.ElementAt(i).Red += (int)Math.Round(0.1 * ((double)meanVector.Red - (double)groups.Clusters.ElementAt(i).Red));
                groups.Clusters.ElementAt(i).Green += (int)Math.Round(0.1 * ((double)meanVector.Green - (double)groups.Clusters.ElementAt(i).Green));
                groups.Clusters.ElementAt(i).Blue += (int)Math.Round(0.1 * ((double)meanVector.Blue - (double)groups.Clusters.ElementAt(i).Blue));
            }
        }

        private void DrawGroups(List<Neuron> neuronListCopy)
        {
            SelectedGroups wynik = new SelectedGroups();
            wynik.Show();

            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    int red = Convert.ToInt32(neuronListCopy.ElementAt(_neuronPosition[i, j]).RWeight);
                    int green = Convert.ToInt32(neuronListCopy.ElementAt(_neuronPosition[i, j]).GWeight);
                    int blue = Convert.ToInt32(neuronListCopy.ElementAt(_neuronPosition[i, j]).BWeight);

                    SolidBrush myBrush = new SolidBrush(Color.FromArgb(red, green, blue));
                    wynik.CreateGraphics().FillRectangle(myBrush, new Rectangle(5 + (10 * i), 5 + (10 * j), 10, 10));
                }
            }
        }
        private void UpdateControl(Control control, string propertyName, object propertyValue)
        {
            if (control.InvokeRequired)
            {
                try
                {
                    control.Invoke(new UpdateControlDelegate(UpdateControl), new object[] { control, propertyName, propertyValue });
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                PropertyInfo prop = control.GetType().GetProperty(propertyName);
                prop?.SetValue(control, propertyValue);
            }
        }

        private void UpdateMeta()
        {
            UpdateControl(learningLabel, "Text", Math.Round(LearningRate, 4).ToString());
            UpdateControl(neighbourLabel, "Text", Math.Round(NeighbourhoodPrecise).ToString());
            UpdateControl(iterCount, "Text", (_numberOfIterations - 1).ToString());
        }
    }
}
