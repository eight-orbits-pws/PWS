using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eight_Orbits {
	public partial class TextOutput : Component {
		public TextOutput() {
			InitializeComponent();
		}

		public TextOutput(IContainer container) {
			container.Add(this);

			InitializeComponent();
		}
	}
}
