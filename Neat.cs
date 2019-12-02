using Eight_Orbits;
using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using static Eight_Orbits.Program;
using static System.Math;
using System.Runtime.InteropServices;

namespace Neural_Network
{
    delegate Task FireEvent(Neat sender, double d);

    struct Gene
    {
        public int from;
        public Axon axon;
        public bool enabled;
        public int innovation;

        public Gene(int from, int to, double weight, bool enabled, int innovation)
        {
            this.from = from;
            this.axon = new Axon(to, weight);
            this.enabled = enabled;
            this.innovation = innovation;
        }

        public override string ToString()
        {
            return innovation + ": " + from + " -> " + axon.destination + ", " + axon.weight;
        }
    }

	class Neat {
		public Keys Key { get; set; }
		public static List<Neat> All = new List<Neat>();
        bool lastOut = false;
		
		readonly object ThinkLock = new { };
        public List<InputNeuron> Input { get; } = new List<InputNeuron>(66);
        public List<StdNeuron> Neurons { get; }
        public OutputNeuron Output { get; set; }
        public List<Gene> Genes { get; } = new List<Gene>();
        public int Count { get { return Neurons.Count; } }
        private Ray ray;

        float mutate_weights = 0.80f;
        float mutate_perturb = 0.90f;
        float mutate_step = 0.10f;

        float mutate_crossover = 0.75f;
        float mutate_enable = 0.25f;

        private static int seconds = 60;
        private static int ticks = 60 * seconds;

        private int inactiveTicks = 0;

        internal void ResetNeurons()
        {
            Reset?.Invoke();
        }

        float mutate_node = 0.50f;

        float mutate_link = 2.00f;
        float mutate_bias = 0.40f;

        public int innovation = 0;

		public Neuron this[int index] { get {
				if (index < 0) return Input[-1-index];
				else if (index == 0) return Output;
				else return Neurons[index-1];
			}
		}

		public event Action Fire;
		public event Action KeyUp;
		public event Action Reset;
		public event Action OnRemove;

		public Neat() {
			// ---
			for (int i = 0; i < 66; i++) Input.Add(new InputNeuron(this, -i - 1));
			Neurons = new List<StdNeuron>();
			Output = new OutputNeuron(this, 0);
            // ---
        }

        public Neat(Neat parent) : this()
        {
            CopyFrom(parent);
        }

        private void CopyFrom(Neat parent)
        {
			lock (ThinkLock) {
                for (int i = 0; i < parent.Neurons.Count; i++)
                {
                    Neurons.Add(parent.Neurons[i].Clone(this));
                }
			}
            for (int i = 0; i < parent.Genes.Count; i++)
            {
                Gene gene = parent.Genes[i];
                Genes.Add(new Gene(gene.from, gene.axon.destination, gene.axon.weight, gene.enabled, gene.innovation));
            }

            innovation = parent.innovation;
        }

        public void AddKey()
        {
            //Don't touch rest of func
            Head head = new Head(this);
            Key = head.KeyCode;

            try
            {
                Program.HEADS.Add(Key, head);
            }
            catch (ArgumentException)
            {
                HEADS.Add(Keys.Pa1, head);
                Key = Keys.Pa1;
            }

            OnUpdateNNW += update;
            All.Add(this);
        }
		public void Remove() {
			OnRemove?.Invoke();

			Fire = null;
			KeyUp = null;
			Reset = null;
			OnRemove = null;
			
			HEADS[Key].Remove();
			OnUpdateNNW -= update;
			All.Remove(this);
		}

		readonly HashSet<double> distances = new HashSet<double>();
        private static readonly double diagonal = Sqrt(W * W + H * H);
		private static readonly double deltaA = PI / 6d;
        
		private void fetch_input() {
			//inorbit, rotation, x, y, orbs in tail
			//twelve rays
			//each has distance to first { orbit, blast, orb, orb, player }
			//type: player | blast | orb(w) | orb(p) | wall | orbits
			//65 input nodes total
			if (!HEADS.ContainsKey(Key) || Map.phase != Phases.NONE) return;
			Head head = HEADS[Key];

			if (head.Died) return;
			
			ray.Set(head.pos, head.v);
			
			Input[0].add(BoolToInt(Map.InOrbit(head.pos)));
			Input[1].add(head.v.A / PI);
			Input[2].add(head.pos.X / W * 2 - 1);
			Input[3].add(head.pos.Y / W);
			lock (Orb.OrbLock) Input[4].add(head.tail.length == 0? -1 : 1 / head.tail.length);


			for (int i = 0; i < 12; i++) {
				foreach (Circle orbit in Map.Orbits) if (ray.Hit(orbit)) distances.Add(ray.Distance(orbit));
				if (distances.Count == 0) distances.Add(diagonal);
				Input[5 + 5*i].add(distances.Min() / diagonal);
				distances.Clear();

				lock (Blast.BlastLock) foreach (Circle blast in Blast.All) if (ray.Hit(blast)) distances.Add(ray.Distance(blast));
				if (distances.Count == 0) distances.Add(diagonal);
				Input[6 + 5*i].add(distances.Min() / diagonal);
				distances.Clear();

				lock (Orb.OrbLock) {
                    int color = 760;
                    double d = diagonal;
                    double d_temp;
                    foreach (Orb orb in Orb.All) {
                        if (orb.state == (byte) OrbStates.TRAVELLING) d_temp = ray.AutoDistance(orb);
                        else d_temp = diagonal;
                        
                        if (d_temp < d) {
                            d = d_temp;
                            color = (orb.color.R + orb.color.G + orb.color.B);
                        }
                    }
				    Input[7 + 5*i].add(d / diagonal);
                    Input[8 + 5*i].add(color / 760d);
                }

				lock (ActiveLock) foreach (Keys k in ActiveKeys) if (k != Key && ray.Hit(HEADS[k])) distances.Add(ray.Distance(HEADS[k]));
				if (distances.Count == 0) distances.Add(diagonal);
				Input[9 + 5*i].add(distances.Min() / diagonal);
				distances.Clear();

				ray.laser.A += deltaA;
			}

            // 66th input neuron
            Input[65].add(1); // Always on
		}

		private void update() {
            if (Map.phase != Phases.NONE)
            {
                inactiveTicks = 0;
                return;
            }

			lock (ThinkLock) {
				fetch_input();

                foreach (Gene gene in Genes)
                {
                    if (!gene.enabled)
                        continue;

                    this[gene.axon.destination].add(this[gene.from].Value * gene.axon.weight);
                }

				foreach (Neuron nr in Input) nr.calc(this);
				foreach (Neuron nr in Neurons) nr.calc(this);
				Output.calc(this);
			}

            bool outp = Output.Value > 0;

            if (outp && !lastOut) Fire?.Invoke(); // Check if it *started* pressing the key
            else if (!outp && lastOut) KeyUp?.Invoke(); // Check if it *stopped* pressing the key

            if (outp != lastOut)
            {
                inactiveTicks = 0;
            }
            else
            {
                inactiveTicks++;
                if (inactiveTicks > ticks)
                {
                    HEADS[Key].Die();
                }
            }

            lastOut = outp;
        }
		
        public void SetupGenZero()
        {
            Mutate();
        }

        public void addNeurons(int max)
        {
            if (max > Count)
                for (int i = Count + 1; i <= max; i++)
                    Neurons.Add(new StdNeuron(this, max));
        }

        public void FromParents(Neat parent, Neat other)
        {

			lock (ThinkLock) {
				OnRemove?.Invoke();
				Neurons.Clear();
				Genes.Clear();
			}

            if (other == null || R.NextDouble() > mutate_crossover)
            {
                CopyFrom(parent);
                VaryColor(parent, null);
            }
            else
            {
                Dictionary<int, Gene> genes = new Dictionary<int, Gene>();

                foreach (Gene gene in parent.Genes.Concat(other.Genes))
                {
                    if (!genes.ContainsKey(gene.innovation) || R.NextDouble() > 0.5)
                    {
                        genes[gene.innovation] = gene;
                    }
                }

                int max = 0;

                foreach (Gene gene in genes.Values)
                {
                    max = Max(max, Max(gene.from, gene.axon.destination));
                    Genes.Add(gene);
                }

                addNeurons(max);
                innovation = Max(parent.innovation, other.innovation);

                VaryColor(parent, other);
            }

            MutateEnable();
            Mutate();
        }

        private void VaryColor(Neat parent, Neat other)
        {
            Head head = HEADS[Key];

            Head from = HEADS[parent.Key];
            Head ohead = other == null ? from : HEADS[other.Key];

            double hue1 = Head.FromColor(from.color);
            double hue2 = Head.FromColor(ohead.color);

            double average = (hue1 + hue2) / 2;
            if (Math.Abs(hue1 - hue2) > 0.5)
                average = 0.5 + average;

            average += R.NextDouble() / 2 - 0.25;
            head.color = Head.FromDouble(average);
        }

        public void MutateEnable()
        {
            for (int i = 0; i < Genes.Count; i++)
            {
                Gene gene = Genes[i];
                if (!gene.enabled && R.NextDouble() < mutate_enable)
                    gene.enabled = true;
            }
        }

        public void Mutate()
        {
            if (R.NextDouble() < mutate_weights)
                MutateWeights();

            double p = mutate_link;
            while (p > 0)
            {
                if (R.NextDouble() < p)
                    MutateLink(false);
                p = p - 1;
            }

            if (R.NextDouble() < mutate_bias)
                MutateLink(true);

            if (R.NextDouble() < mutate_node)
                MutateNode();
        }

        public void MutateWeights()
        {
            for (int i = 0; i < Genes.Count; i++)
            {
                Gene gene = Genes[i];
                if (R.NextDouble() < mutate_perturb)
                    gene.axon.weight += (R.NextDouble() * mutate_step * 2) - mutate_step;
                else
                    gene.axon.weight = R.NextDouble() * 4 - 2;
            }
        }

        public void MutateLink(bool forceBias)
        {
            Neuron n1 = forceBias ? this[-66] : RandomNeuron(true);
            Neuron n2 = RandomNeuron(false);

            foreach (Gene gene in Genes)
                if (gene.from == n1.Index && gene.axon.destination == n2.Index)
                    return;

            Genes.Add(new Gene(n1.Index, n2.Index, R.NextDouble() * 4 - 2, true, innovation++));
        }

        public void MutateNode()
        {
            if (Genes.Count == 0)
                return;

            Gene gene = Genes[R.Next(0, Genes.Count)];
            if (!gene.enabled)
                return;

            gene.enabled = false;

            StdNeuron neuron = new StdNeuron(this, Neurons.Count + 1);
            Neurons.Add(neuron);

            Genes.Add(new Gene(gene.from, neuron.Index, gene.axon.weight, true, innovation++));
            Genes.Add(new Gene(neuron.Index, gene.axon.destination, 1, true, innovation++));
        }

        public Neuron RandomNeuron(bool input)
        {
            return this[R.Next(input ? -Input.Count : 0, Neurons.Count + 1)];
        }

		public Neat Clone() {
            return new Neat(this);
		}

	}

    struct Axon {

        public int destination;
        public double weight;

        public Axon(int destination, double weight) {
            this.destination = destination;
            this.weight = weight;
        }

    }

	class InputNeuron : Neuron {
		public InputNeuron(Neat sender, int index) : base(sender, index) {}

		public override void calc(Neat nnw) {
			value = input;
            input = 0;
		}

        public InputNeuron Clone(Neat sender)
        {
            return new InputNeuron(sender, index);
        }
	}

	class StdNeuron : Neuron {
		//private double input = 0;
		//private double bias = 0;
		//private double value = 0;
		//private List<Axon> axons = new List<Axon>();

		public StdNeuron(Neat sender, int index) : base(sender, index) { }

		public override void calc(Neat nnw) {
			value = MathNNW.ReLU(input);
            if (double.IsNaN(value)) value = 0;
            input = 0;
        }

        public StdNeuron Clone(Neat sender)
        {
            return new StdNeuron(sender, index);
        }
    }

	class OutputNeuron : Neuron {
		public OutputNeuron(Neat sender, int index) : base(sender, index) { }

		public override void calc(Neat nnw) {
			value = MathNNW.Output(input);
            if (double.IsNaN(value)) value = 0;
            input = 0;
        }

        public OutputNeuron Clone(Neat sender)
        {
            return new OutputNeuron(sender, index);
        }

        public static byte[] compile(Neat neat)
        {
            List<byte> bytes = new List<byte>();

            byte[] innovation = BitConverter.GetBytes(neat.innovation);
            bytes.Add(innovation[0]);
            bytes.Add(innovation[1]);
            bytes.Add(innovation[2]);
            bytes.Add(innovation[3]);

            byte[] genes = BitConverter.GetBytes(neat.Genes.Count);
            bytes.Add(genes[0]);
            bytes.Add(genes[1]);
            bytes.Add(genes[2]);
            bytes.Add(genes[3]);

            foreach (Gene gene in neat.Genes)
            {
                byte[] num = BitConverter.GetBytes(gene.innovation);
                bytes.Add(num[0]);
                bytes.Add(num[1]);
                bytes.Add(num[2]);
                bytes.Add(num[3]);

                bytes.Add((byte) (gene.enabled ? 1 : 0));

                byte[] from = BitConverter.GetBytes(gene.from);
                bytes.Add(from[0]);
                bytes.Add(from[1]);
                bytes.Add(from[2]);
                bytes.Add(from[3]);

                byte[] to = BitConverter.GetBytes(gene.axon.destination);
                bytes.Add(to[0]);
                bytes.Add(to[1]);
                bytes.Add(to[2]);
                bytes.Add(to[3]);

                byte[] weight = BitConverter.GetBytes(gene.axon.weight);
                bytes.Add(weight[0]);
                bytes.Add(weight[1]);
                bytes.Add(weight[2]);
                bytes.Add(weight[3]);
                bytes.Add(weight[4]);
                bytes.Add(weight[5]);
                bytes.Add(weight[6]);
                bytes.Add(weight[7]);
            }

            return bytes.ToArray();
        }

        private static byte[] remove(List<byte> list, int amount)
        {
            byte[] array = new byte[amount];
            for (int i = 0; i < amount; i++)
            {
                array[i] = list[i];
            }
            list.RemoveRange(0, amount);
            return array;
        }

        public static Neat decompile(byte[] array)
        {
            List<byte> bytes = new List<byte>(array);

            Neat neat = new Neat();
            neat.innovation = BitConverter.ToInt32(remove(bytes, 4), 0);

            int max = 0;

            int genes = BitConverter.ToInt32(remove(bytes, 4), 0);
            for (int i = 0; i < genes; i++)
            {
                int num = BitConverter.ToInt32(remove(bytes, 4), 0);
                bool enabled = remove(bytes, 1)[0] > 0;

                int from = BitConverter.ToInt32(remove(bytes, 4), 0);
                int to = BitConverter.ToInt32(remove(bytes, 4), 0);
                double weight = BitConverter.ToDouble(remove(bytes, 8), 0);

                neat.Genes.Add(new Gene(from, to, weight, enabled, num));

                max = Max(max, Max(from, to));
            }

            neat.addNeurons(max);

            return neat;
        }
    }

	static class MathNNW {
		public static double ReLU(double x) => (int) x > 0? x : 0;
		public static double Satlin(double x) => x > 0? x < 1? x : 1 : 0;
		public static double Satlins(double x) => x > -1? x < 1? x : 1 : -1;
		public static double Output(double x) => x > 0? 1 : 0; // 0 or 1
		public static double Radial(double x) => Max(0, 1D-Abs(x));
		public static double Sinus(double x) => Sin(x);
        public static double Sigmoid(double x) => 2 / (1 + Exp(-5*x)) - 1; // between -1 and 1
		public static double R { get { return Program.R.NextDouble(); } }
	}
}
