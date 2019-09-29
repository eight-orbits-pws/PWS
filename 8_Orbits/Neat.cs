using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eight_Orbits.Properties;
using System.Windows.Forms;
using Eight_Orbits.Entities;
using Eight_Orbits;
using static Eight_Orbits.Program;
using static System.Math;

namespace Neural_Network {
	delegate Task FireEvent(Neat sender, double d);

	class Neat {
		private Keys key;
		public static List<Neat> All = new List<Neat>();

		List<InputNeuron> input = new List<InputNeuron>(64);
		List<Neuron> neurons;
		OutputNeuron output;

		bool lastOut = false;

		public List<InputNeuron> Input { get { return input; } }
		public List<Neuron> Neurons { get { return neurons; } }
		public OutputNeuron Output { get { return output; } }
		public int Count { get { return neurons.Count; } }

		public Neuron this[int index] { get {
				if (index < 0) return input[-1-index];
				else if (index == 0) return output;
				else return neurons[index-1];
			}
		}

		public event Action Fire;
		public event Action KeyUp;
		public event Action Reset;
		public event Action OnRemove;

		public Neat() {
			// ---
			for (int i = 0; i < 64; i++) input.Add(new InputNeuron(this, -i - 1));
			neurons = new List<Neuron>();
            for (int i = 0; i < 32; i++) neurons.Add(new StdNeuron(this, i + 1));
			output = new OutputNeuron(this, 0);
			create();
			// ---

			//Don't touch rest of func
			Head head = new Head(this);
			key = head.KeyCode;
			try {
				Program.HEADS.Add(key, head);
			} catch (ArgumentException) {
				HEADS.Add(Keys.Pa1, head);
				key = Keys.Pa1;
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
			
			HEADS[key].Remove();
			OnUpdateNNW -= update;
			All.Remove(this);
		}

		public Neat(Neat parent) {
			input = parent.Input;
		}

		public void SetKey(Keys key) {
			this.key = key;
		}

		private void create() {
            Random random = new Random();

            foreach (InputNeuron node in input)
                foreach (StdNeuron neuron in neurons)
                    if (random.NextDouble() < 0.2d)
                        node.Axons.Add(new Axon(neuron.Index, random.NextDouble() * 2 - 1));

            foreach (StdNeuron node in neurons)
            {
                if (random.NextDouble() < 0.5d)
                    node.Axons.Add(new Axon(0, random.NextDouble() * 2 - 1));
                if (random.NextDouble() < 0.5d)
                    output.Axons.Add(new Axon(node.Index, random.NextDouble() * 2 - 1));
            }
        }
		
		private void fetch_input() {
			//inorbit, rotation, x, y, orbs in tail
			//twelve rays
			//each has distance to first { orbit, blast, orb, orb, player }
			//type: player | blast | orb(w) | orb(p) | wall | orbits
			//65 input nodes total

			Head head;
			try {
				head = HEADS[key];
			} catch (KeyNotFoundException) {
				return;
			}

			if (head.Died || Map.phase != Phases.NONE) return;
			
			ray.Set(head.pos, head.v);
			HashSet<double> distances = new HashSet<double>();
			double deltaA = Math.PI / 6d;
			
			input[0].add(BoolToInt(Map.InOrbit(head.pos)));
			input[1].add(head.v.A / PI);
			input[2].add(head.pos.X / W * 2 - 1);
			input[3].add(head.pos.Y / W);
			input[4].add(head.tail.length == 0? -1 : 1 / head.tail.length);

			for (int i = 0; i < 12; i++) {
				foreach (Circle orbit in Map.Orbits) if (ray.Hit(orbit)) distances.Add(ray.Distance(orbit));
				if (distances.Count == 0) distances.Add(-1);
				input[5 + 5*i].add(distances.Min());
				distances.Clear();

				lock (Blast.BlastLock) foreach (Circle blast in Blast.All) if (ray.Hit(blast)) distances.Add(ray.Distance(blast));
				if (distances.Count == 0) distances.Add(-1);
				input[6 + 5*i].add(distances.Min());
				distances.Clear();

				lock (Orb.OrbLock) foreach (Orb orb in Orb.All) if (orb.noOwner() && ray.Hit(orb)) distances.Add(ray.Distance(orb));
				if (distances.Count == 0) distances.Add(-1);
				input[7 + 5*i].add(distances.Min());
				distances.Clear();

				lock (Orb.OrbLock) foreach (Orb orb in Orb.All) if (ray.Hit(orb) && !orb.noOwner() && orb.owner != key) distances.Add(ray.Distance(orb));
				if (distances.Count == 0) distances.Add(-1);
				input[8 + 5*i].add(distances.Min());
				distances.Clear();

				lock (ActiveLock) foreach (Keys k in ActiveKeys) if (k != key && ray.Hit(HEADS[k])) distances.Add(ray.Distance(HEADS[k]));
				if (distances.Count == 0) distances.Add(-1);
				input[9 + 5*i].add(distances.Min());
				distances.Clear();

				ray.laser.A += deltaA;
			}
		}
		
		private void update() {
            if (Map.phase != Phases.NONE)
                return;

			fetch_input();

            foreach (Neuron nr in input) nr.fireAxons(this);
            foreach (Neuron nr in neurons)  nr.fireAxons(this);
            output.fireAxons(this);

            foreach (Neuron nr in input) nr.calc(this);
            foreach (Neuron nr in neurons) nr.calc(this);
            output.calc(this);

            bool outp = output.Value > 0;
            if (outp && !lastOut) Fire?.Invoke(); // Check if it *started* pressing the key
			else if (!outp && lastOut) KeyUp?.Invoke(); // Check if it *stopped* pressing the key
			lastOut = outp;
		}

		public Neat clone() {
			throw new NotImplementedException();
		}

		public void reproduce() {
			throw new NotImplementedException();
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
	}

	class StdNeuron : Neuron {
		//private double input = 0;
		//private double bias = 0;
		//private double value = 0;
		//private List<Axon> axons = new List<Axon>();

		public StdNeuron(Neat sender, int index) : base(sender, index) { }

		public override void calc(Neat nnw) {
			value = MathNNW.Satlins(input + bias);
            input = 0;
        }

		protected override void remove() {
			axons = null;
		}
	}

	class OutputNeuron : Neuron {
		public OutputNeuron(Neat sender, int index) : base(sender, index) { }

		public override void calc(Neat nnw) {
			value = MathNNW.Satlins(input + bias);
            input = 0;
        }
		
		protected override void remove() { }
	}

	static class MathNNW {
		public static double ReLU(double x) => Max(0, x);
		public static double Satlin(double x) => Max(0, Min(x, 1));
		public static double Satlins(double x) => Max(-1, Min(x, 1));
		public static double Output(double x) => Max(0, Sign(x)); // 0 or 1
		public static double Radial(double x) => Max(0, 1D-Abs(x));
		public static double Sinus(double x) => Sin(x);
		public static double R { get { return new Random().NextDouble(); } }
	}
}
