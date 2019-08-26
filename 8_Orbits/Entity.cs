using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Eight_Orbits.Properties {
	interface Visual {
		void Update();
		void Draw(Graphics g);
	}

	public class Circle {
		public IPoint pos;
		public IVector v;
		public float r;
		public Color color;

		public Circle() {
			this.pos = new IPoint();
			this.v = IVector.Zero;
			this.r = 0;
			this.color = Color.Transparent;
		}

		public Circle(IPoint pos, float r) {
			this.pos = pos;
			this.v = IVector.Zero;
			this.r = r;
			this.color = Color.Transparent;
		}

		public Circle(IPoint pos, float r, Color c) {
			this.pos = pos;
			this.v = IVector.Zero;
			this.r = r;
			this.color = c;
		}

		public Circle(Circle c) {
			this.pos = c.pos.Copy();
			this.v = c.v.Copy();
			this.r = c.r;
			this.color = c.color;
		}

		public Circle Copy() {
			return new Circle(this);
		}

		public bool Collide(Circle c) {
			return this.pos * c.pos < this.r + c.r - 3;
		}
	}
}
