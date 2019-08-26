using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neural_Network {
	/*interface NNW {
		Neuron this[int index] { get; }
		int Count { get; }
		event Action OnRemove;
		void Remove();
		void SetKey(System.Windows.Forms.Keys key);
		void fetch_input();
		void create();
		void update();
		NNW clone();
		void reproduce();
		event Action Fire;
		event Action KeyUp;
		event Action Reset;
	}*/

	abstract class Neuron {
		protected double input;
		protected double bias;
		protected double value;
		protected int index;
		public List<Axon> axons = new List<Axon>();

		public double Input { get { return input; } }
		public double Bias { get { return bias; } }
		public double Value { get { return value; } }
		public int Index { get { return index; } }
		public List<Axon> Axons { get { return axons; } }

		public abstract event FireEvent Fire;

		public Neuron(Neat sender) {
			sender.Reset += reset;
			sender.OnRemove += remove;
		}

		public Neuron(Neat sender, int index) : this(sender) {
			this.index = index;
			this.input = 0;
			this.bias = 0;
			this.value = 0;
		}

		public abstract Task calc(Neat nnw);
		
		public void add(double d) {
			input += d;
		}

		public void create(int lastneuron) {
			int L = lastneuron - Math.Max(0, index);
			for (int i = 0; i <= L; i++) if (MathNNW.R < 1d) axons.Add(new Axon(this, i));
		}

		public void mutate() {
			throw new NotImplementedException();
		}

		protected virtual void reset() {
			input = 0;
		}

		protected abstract void remove();
	}

	
}
