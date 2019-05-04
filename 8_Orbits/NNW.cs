using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neural_Network {
	interface NNW {
		void fetch_input();
		void update();
		void get_output();
		void clone();
		void reproduce();
		void mutate();
		void crossover();
	}
}
