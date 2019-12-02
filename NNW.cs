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
		protected double value;
		protected int index;

		public double Input { get { return input; } }
		public double Value { get { return value; } set { this.value = value;  } }
		public int Index { get { return index; } }

		public Neuron(Neat sender) {
			sender.Reset += reset;
		}

		public Neuron(Neat sender, int index) : this(sender) {
			this.index = index;
			this.input = 0;
			this.value = 0;
		}

		public abstract void calc(Neat nnw);
		
		public void add(double d) {
			input += d;
		}

		public virtual void reset() {
			input = 0;
		}

	}

	
}
