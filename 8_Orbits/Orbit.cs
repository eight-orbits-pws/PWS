using System.Drawing;
using Eight_Orbits.Properties;
using static Eight_Orbits.Program;

namespace Eight_Orbits.Entities {
	class Orbit : Circle {
		
		private readonly float o;
		private readonly Animatable R;

		public Orbit(float x, float y, float r) {
			this.color = Color.FromArgb(255, 200, 200, 200);
			this.pos = new IPoint((double) x * W, (double) y * W / 2);
			this.r = W * r - 2f * Scale;
			this.R = new Animatable(0, this.r, 12, AnimationTypes.SIN, false);
			this.o = 5f * Scale;
		}

		public void Remove() {
			this.R.Remove();
		}

		public void Draw(Graphics g) {
			if (ContrastMode) this.color = Color.FromArgb(unchecked((int)0xFF555555));
			else this.color = Color.FromArgb(unchecked((int)0xFFC8C8C8));

			g.DrawEllipse(new Pen(color, R - o), (float) pos.X - R/2 - o/2, (float) pos.Y - R/2 - o/2, R + o, R + o);
		}

		public bool Inside(IPoint pos) => this.pos * pos < this.r - 2f;
	}
}
