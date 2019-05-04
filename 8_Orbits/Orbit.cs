using System;
using System.Drawing;
using System.Windows.Forms;
using Eight_Orbits.Properties;
using static Eight_Orbits.Program;

namespace Eight_Orbits.Entities {
	class Orbit : Visual {
		
		public IPoint pos;
		public float r;
		public Color color;
		
		public Orbit(float x, float y, float r) {
			this.color = Color.FromArgb(255, 200, 200, 200);
			this.pos = new IPoint((double) x * W, (double) y * W / 2);
			this.r = W * r - 4f;
		}

		public void Draw(ref PaintEventArgs e) {
			if (ContrastMode) this.color = Color.FromArgb(unchecked((int)0xFF555555));
			else this.color = Color.FromArgb(unchecked((int)0xFFC8C8C8));

			e.Graphics.FillEllipse(new SolidBrush(color), (float) pos.X - r, (float) pos.Y - r, r * 2f, r * 2f);
			e.Graphics.FillEllipse(new SolidBrush(window.MapColor), (float) pos.X - 5f, (float) pos.Y - 5f, 10, 10);
			//return e;
		}

		public void Update() { return; }

		public bool Inside(IPoint pos) {
			return this.pos * pos < this.r - 1f;
		}
	}
}
