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
using System.Threading;

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

        internal void ResetNeurons()
        {
            Reset?.Invoke();
        }

        float mutate_node = 0.50f;

        float mutate_link = 2.00f;
        float mutate_bias = 0.40f;

        private int innovation = 0;

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

        public Neat(Neat parent)
        {
            Neurons = new List<StdNeuron>(parent.Neurons.Count);
            CopyFrom(parent);
        }

        private void CopyFrom(Neat parent)
        {
			lock (ThinkLock) {
				for (int i = 0; i < parent.Input.Count; i++)
					Input.Add(parent.Input[i].Clone(this));
				for (int i = 0; i < parent.Neurons.Count; i++)
					Neurons.Add(parent.Neurons[i].Clone(this));
				Output = parent.Output.Clone(this);
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
                return;

			lock (ThinkLock) {
				fetch_input();
				foreach (Neuron nr in Input) nr.fireAxons(this);
				foreach (Neuron nr in Neurons) nr.fireAxons(this);
				Output.fireAxons(this);

				foreach (Neuron nr in Input) nr.calc(this);
				foreach (Neuron nr in Neurons) nr.calc(this);
				Output.calc(this);
			}

            bool outp = Output.Value > 0;
            if (outp && !lastOut) Fire?.Invoke(); // Check if it *started* pressing the key
            else if (!outp && lastOut) KeyUp?.Invoke(); // Check if it *stopped* pressing the key
            lastOut = outp;
            //}
            //catch (Exception e)
            //{
           //     if (Map.phase != Phases.NONE)
           //         return;
            //    Console.WriteLine(e);
            //}
		}
		
        public void SetupGenZero()
        {
            Mutate();
            SetupAxons();
        }

        public void SetupAxons()
        {
			lock (ThinkLock) {
				foreach (Neuron neuron in Input) neuron.axons.Clear();
				foreach (Neuron neuron in Neurons) neuron.axons.Clear();
				Output.axons.Clear();

				foreach (Gene gene in Genes)
					if (gene.enabled)
						this[gene.from].axons.Add(gene.axon);
			}
        }

        public void FromParent(Neat parent)
        {

			lock (ThinkLock) {
				OnRemove?.Invoke();
				Input.Clear();
				Neurons.Clear();
				Genes.Clear();
			}
            CopyFrom(parent);

            MutateEnable();
            Mutate();

            SetupAxons();

            VaryColor(parent);
        }

        private void VaryColor(Neat parent)
        {
            Head head = HEADS[Key];
            Head from = HEADS[parent.Key];

            HSV hsv;
            hsv.h = from.color.GetHue();
            hsv.s = from.color.GetSaturation();
            hsv.v = from.color.GetBrightness();

            hsv.h += (float)R.NextDouble() * 180 - 90;
            if (hsv.h < 0)
                hsv.h += 360;
            if (hsv.h >= 360)
                hsv.h -= 360;

            head.color = ColorFromHSL(hsv);
        }

        public struct HSV { public float h; public float s; public float v; }

        public static Color ColorFromHSL(HSV hsl)
        {
            if (hsl.s == 0)
            { int L = (int)hsl.v; return Color.FromArgb(255, L, L, L); }

            double min, max, h;
            h = hsl.h / 360d;

            max = hsl.v < 0.5d ? hsl.v * (1 + hsl.s) : (hsl.v + hsl.s) - (hsl.v * hsl.s);
            min = (hsl.v * 2d) - max;

            Color c = Color.FromArgb(255, (int)(255 * RGBChannelFromHue(min, max, h + 1 / 3d)),
                                          (int)(255 * RGBChannelFromHue(min, max, h)),
                                          (int)(255 * RGBChannelFromHue(min, max, h - 1 / 3d)));
            return c;
        }
        private static double RGBChannelFromHue(double m1, double m2, double h)
        {
            h = (h + 1d) % 1d;
            if (h < 0) h += 1;
            if (h * 6 < 1) return m1 + (m2 - m1) * 6 * h;
            else if (h * 2 < 1) return m2;
            else if (h * 3 < 2) return m1 + (m2 - m1) * 6 * (2d / 3d - h);
            else return m1;

        }

        private byte clamp(int i)
        {
            return (byte)Math.Max(0, Math.Min(256, i));
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

		protected override void remove() {
			axons = null;
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
            input = 0;
        }

		protected override void remove() {
			axons = null;
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
            input = 0;
        }
		
		protected override void remove() { }

        public OutputNeuron Clone(Neat sender)
        {
            return new OutputNeuron(sender, index);
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

    public static byte[] compile(Neat neat) {
        List<byte> bytes = new List<byte>();

        return bytes.ToArray();
    }

    public static Neat decompile(byte[] bytes) {
        Neat neat = new Neat();

        return neat;
    }
}
