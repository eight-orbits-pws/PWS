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
		private Ray ray;

		public Neuron this[int index] { get {
				if (index < 0) return input[1-index];
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
			for (int i = 0; i < 64; i++) input.Add(new InputNeuron(this));
			neurons = new List<Neuron>(256);
			neurons.Add(new StdNeuron(this));
			output = new OutputNeuron(this);
			create();
			// ---

			//Don't touch rest of func
			Head head = new Head(this);
			key = head.keyCode;
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
			foreach (InputNeuron node in input) node.create(0);
			foreach (Neuron node in neurons) node.create(1);
		}
		
		private void fetch_input() {
			//inorbit, 0, r, v
			//twelve rays
			//each has { dist, type_info, type_info, type_info }
			//type: player | blast | orb(w) | orb(p)
			//64 input nodes total
			Head head;
			try {
				head = HEADS[key];
			} catch (KeyNotFoundException) {
				return;
			}

			if (head.Died || state != States.INGAME) return;
			
			ray.Set(head.pos, head.v);
			Dictionary<double, byte> distances = new Dictionary<double, byte>();
			double distance;
			byte info;
			double deltaA = Math.PI / 6d;

			input[0].add(BoolToInt(Map.InOrbit(head.pos)));
			foreach (Orbit orbit in Map.Orbits) if (ray.Hit(orbit)) distances.Add(ray.Distance(orbit), 0);
			if (distances.Count == 0) distances.Add(W, 0);
			ray.laser.L = distances.Keys.Min();
			input[1].add(distances.Keys.Min() / W);
			input[2].add(head.v.A / Math.PI);
			input[3].add(head.v.L / (speed * 2));

			for (int i = 0; i < 12; i++) {
				//raycast
				distances.Clear();
				try {
					for (int j = Blast.All.Count - 1; j >= 0; j--)
						if (ray.Hit(Blast.All[j])) distances.Add(ray.Distance(Blast.All[j]), 2);

					foreach (Keys k in ActiveKeys)
						if (k != key && ray.Hit(HEADS[k]))
							distances.Add(ray.Distance(HEADS[k]), 1);

					for (int j = Orb.All.Count - 1; j >= 0; j--)
						if (ray.Hit(Orb.All[j]) && Orb.All[j].state != OrbStates.WHITE) {
							if (Orb.All[j].noOwner()) distances.Add(ray.Distance(Orb.All[j]), 2);
							else distances.Add(ray.Distance(Orb.All[j]), 3);
						}
				} catch (Exception) {
					// nothing
				}

				if (distances.Count == 0) distances.Add(W, 0);
				distance = distances.Keys.Min();
				info = distances[distance];

				if (info > 0) input[4 + info + 5*i].add(1);
				input[4 + 5*i].add(distance / W);

				ray.laser.A += deltaA;
			}
		}
		
		private async void update() {
			temp_update();
			if (ApplicationRunning) return;

			await Task.Run((Action) fetch_input);
			
			foreach (Neuron nr in input) await nr.calc(this);
			foreach (Neuron nr in neurons) await nr.calc(this);
			
			await output.calc(this);
			bool outp = output.Value > 0;
			if (outp && !lastOut) Fire?.Invoke();
			else if (!outp && lastOut) KeyUp?.Invoke();
			lastOut = outp;

			Reset?.Invoke();
		}

		private void temp_update() {
			bool outp = lastOut;
			if (R.NextDouble() < 1 / 4d) outp = !lastOut;

			if (outp && !lastOut) Fire?.Invoke();
			else if (!outp && lastOut) KeyUp?.Invoke();

			lastOut = outp;
		}

		public Neat clone() {
			throw new NotImplementedException();
		}

		public void reproduce() {
			throw new NotImplementedException();
		}
	}

	class Axon {
		int sendto = 0;
		double value = 0;
		bool enabled = true;

		public int SendTo { get { return sendto; } }
		public double Value { get { return value; } }

		public Axon(Neuron sender, int sendto) {
			sender.Fire += calc;
			this.sendto = sendto;
			this.value = MathNNW.R;
		}

		public async Task calc(Neat sender, double d) {
			if (!enabled) return;
			if (double.IsNaN(d)) throw new ArithmeticException();
			try {
				sender[sendto].add(d * value);
			} catch (NullReferenceException) {
				enabled = false;
			}
		}

		public void mutate() {
			throw new NotImplementedException();
		}
	}

	class InputNeuron : Neuron {
		public InputNeuron(Neat sender) : base(sender) {}

		public override event FireEvent Fire;

		public override async Task calc(Neat nnw) {
			value = input;
			await Fire.Invoke(nnw, value);
		}

		protected override void remove() {
			Fire = null;
			axons = null;
		}
	}

	class StdNeuron : Neuron {
		//private double input = 0;
		//private double bias = 0;
		//private double value = 0;
		//private List<Axon> axons = new List<Axon>();

		public StdNeuron(Neat sender) : base(sender) { }
		
		public override event FireEvent Fire;
		public override async Task calc(Neat nnw) {
			value = MathNNW.ReLU(bias + input);
			await Fire?.Invoke(nnw, value);
		}

		protected override void remove() {
			Fire = null;
			axons = null;
		}
	}

	class OutputNeuron : Neuron {
		public OutputNeuron(Neat sender) : base(sender) { }

		public override async Task calc(Neat sender) {
			await Task.Run(() => value = MathNNW.Output(input + bias));
		}
		
		public override event FireEvent Fire;
		protected override void remove() { }
	}

	static class MathNNW {
		public static double ReLU(double x) { return Max(0, x); }
		public static double Satlin(double x) { return Max(0, Min(x, 1)); }
		public static double Satlins(double x) { return Max(-1, Min(x, 1)); }
		public static double Output(double x) { return Max(0, Sign(x)); } // 0 of 1
		public static double Radial(double x) { return Max(0, 1D-Abs(x)); }
		public static double Sinus(double x) { return Sin(x); }
		public static double R { get { return (new Random()).NextDouble(); } }
	}
}
