using System.Drawing;
using Eight_Orbits.Properties;
using static Eight_Orbits.Program;

namespace Eight_Orbits.Entities {
	class Orbit : Circle {
		
		float o;
		Animatable R;

		public Orbit(float x, float y, float r) {
			this.color = Color.FromArgb(255, 200, 200, 200);
			this.pos = new IPoint((double) x * W, (double) y * W / 2);
			this.r = W * r - 5f * Scale;
			this.R = new Animatable(0, this.r, 12, AnimationTypes.SIN);
			this.o = 5f * Scale;
		}

		public void Remove() {
			//this.R.Set(0);
			//new Animation(pos, 12, o, 0, r - o, 0, color, 255, AnimationTypes.SIN);
		}

		public void Draw(Graphics g) {
			if (ContrastMode) this.color = Color.FromArgb(unchecked((int)0xFF555555));
			else this.color = Color.FromArgb(unchecked((int)0xFFC8C8C8));

			g.DrawEllipse(new Pen(color, R - o), (float) pos.X - R/2 - o/2, (float) pos.Y - R/2 - o/2, R + o, R + o);
		}

		public bool Inside(IPoint pos) {
			return this.pos * pos < this.r - 2f;
		}
	}
}
