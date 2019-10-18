using static System.Math;

namespace Eight_Orbits.Properties {

	public struct IVector {

		private double x;
		private double y;
		private double l;
		private double a;

		public double X {
			get { return x; }
			set {
				x = value;
				l = Sqrt(x*x + y*y);
				a = Atan2(y, x);
			}
		}

		public double Y {
			get { return y; }
			set {
				y = value;
				l = Sqrt(x*x + y*y);
				a = Atan2(y, x);
			}
		}

		public double L {
			get { return l; }
			set {
				l = value;
				x = l * Cos(a);
				y = l * Sin(a);
			}
		}

		public double A {
			get { return a; }
			set {
				a = value;
				x = l * Cos(a);
				y = l * Sin(a);
			}
		}

		public IVector(double x, double y) {
			this.x = x;
			this.y = y;
			this.l = Sqrt(x*x + y*y);
			this.a = Atan2(y, x);
		}

		public IVector(IVector v) {
			this.x = v.X;
			this.y = v.Y;
			this.l = v.L;
			this.a = v.A;
		}

		public IVector Copy => new IVector(this.x, this.y);

		public static IVector operator +(IVector a, IVector b) => new IVector(a.X + b.X, a.Y + b.Y);
		public static IVector operator -(IVector a, IVector b) => new IVector(a.X - b.X, a.Y - b.Y);
		public static IVector operator -(IVector v) => new IVector(-v.X, -v.Y);
		public static IVector operator *(double scalar, IVector v) => new IVector(scalar * v.X, scalar * v.Y);
		public static IVector operator *(IVector v, double scalar) => new IVector(scalar * v.X, scalar * v.Y);
		public static IVector operator /(IVector v, double d) => new IVector(v.X / d, v.Y / d);
		//dot product
		public static double operator *(IVector a, IVector b) => a.X * b.X + a.Y * b.Y;
		//normalize
		public static IVector operator ~(IVector v) {
			v.L = 1;
			return v;
		}

		public static explicit operator IVector (System.Drawing.SizeF sz) => new IVector(sz.Width, sz.Height);

		public static bool operator ==(IVector a, IVector b) => a.x == b.x && a.y == b.y;
		public static bool operator !=(IVector a, IVector b) => a.x != b.x || a.y != b.y;

		public override bool Equals(object obj) => this.GetHashCode() == obj.GetHashCode();
		public override int GetHashCode() => ((int)x ^ (int)y) ^ ((int)(x % 1D) ^ (int)(y % 1D));

		public static IVector Up => new IVector(0, -1);
		public static IVector Right => new IVector(1, 0);
		public static IVector Down => new IVector(0, 1);
		public static IVector Left => new IVector(-1, 0);
		public static IVector Zero => new IVector(0, 0);
	}

	/// <summary>
	/// Point
	/// </summary>
	public struct IPoint {
		public double X { get; set; }

		public double Y { get; set; }

		public IPoint(double x, double y) {
			this.X = x;
			this.Y = y;
		}

		public IPoint Copy() => new IPoint(X, Y);

		public static IPoint Zero => new IPoint(0, 0);
		public static IPoint Center => new IPoint(Program.W / 2, Program.W / 4);

		public static explicit operator System.Drawing.PointF(IPoint p) => new System.Drawing.PointF((float)p.X, (float)p.Y);

		public static explicit operator System.Drawing.Point(IPoint p) => new System.Drawing.Point((int)p.X, (int)p.Y);

		//operator overloads
		public static IPoint operator +(IPoint p, IVector v) {
			p.X += v.X; p.Y += v.Y;
			return p;
		}

		public static IPoint operator -(IPoint p, IVector v) {
			p.X -= v.X; p.Y -= v.Y;
			return p;
		}

		//create vector from b to a
		public static IVector operator -(IPoint a, IPoint b) => new IVector(a.X - b.X, a.Y - b.Y);

		//distance between
		public static double operator *(IPoint a, IPoint b) => Sqrt(Pow(b.X - a.X, 2) + Pow(b.Y - a.Y, 2));

		//angle between
		public static double operator ^(IPoint a, IPoint b) => Atan2(b.Y - a.Y, b.X - a.X);

		public static bool operator ==(IPoint a, IPoint b) => a.X == b.X && a.Y == b.Y;

		public static bool operator !=(IPoint a, IPoint b) => a.X != b.X || a.Y != b.Y;

		public override string ToString() => Round(X).ToString() + ", " + Round(Y).ToString();

		public override int GetHashCode() => (int)(X * Y);

		public override bool Equals(object obj) => obj.Equals(this);
	}

	public struct Ray {
		//start, end, hitreg bools
		//all rays have length W
		public IPoint gun;
		public IVector laser;

		public Ray(IPoint p, IVector v) {
			gun = p;
			laser = v;
		}

		public void Set(IPoint p, IVector v) {
			gun = p;
			laser = v;
		}

		public bool Hit(Circle c) {
			IVector v = c.pos - gun;
			if (v.L <= c.r) return true;
			else if (v * laser <= 0) return false;
			else if (Abs(v.A - laser.A) % PI <= Abs(Asin(c.r / v.L))) return true;//within offset
			else return false;
		}

		public double Distance(Circle c) {
			///    x=(-(X+aY)+sqrt(-(a2+1)r2-(aX-Y)2)) / (a2+1)
			/// of x=(-(X+aY)-sqrt(-(a2+1)r2-(aX-Y)2)) / (a2+1)
			IVector v = c.pos - gun;
			double a = Tan(c.pos ^ gun);
			IVector A = new IVector(1, a);
			double X_aY = v.X + a * v.Y;
			double aX_Y_2 = Pow(a * v.X - v.Y, 2);

			double a2_1 = a * a + 1D;
			double r2 = c.r * c.r;
			
			double x0 = (-X_aY + Sqrt((a2_1) * r2 - aX_Y_2)) / a2_1;
			double x1 = (-X_aY - Sqrt((a2_1) * r2 - aX_Y_2)) / a2_1;

			if (x0 * x1 < 0) {
				if (x0 < 0) laser.L = (x1 * A).L;
				else laser.L = (x0 * A).L;
			} else if (Abs(x0) < Abs(x1)) laser.L = (x0 * A).L;
			else laser.L = (x1 * A).L;
			
			return laser.L; //in pixels
		}

		public double AutoDistance(Circle c) {
			double d = laser.L;

			if (this.Hit(c)) d = Min(d, this.Distance(c));

			return d;
		}

		/*public double Distance() {
			IVector lefttop = new IPoint(0, Program.C) - gun;
			IVector topleft = new IPoint(Program.C, 0) - gun;
			IVector righttop = new IPoint(Program.W, Program.C) - gun;
			IVector topright = new IPoint(Program.W - Program.C, 0) - gun;
			IVector rightbottom = new IPoint(Program.W, Program.W/2d - Program.C) - gun;
			IVector bottomright = new IPoint(Program.W - Program.C, Program.W / 2) - gun;
			IVector leftbottom = new IPoint(0, Program.W - Program.C) - gun;
			IVector bottomleft = new IPoint(Program.C, Program.W / 2d) - gun;
			double a = Tan(laser.A);
			double x = 0, y = 0;

			if (lefttop.A - topleft.A < lefttop.A - laser.A) ;//{ x=25;y=9;} //hit topleft corner
			else if (topleft.A - topright.A < topleft.A - laser.A) { x = -gun.Y / a; y = -gun.Y; } //hit top
			else if (topright.A - righttop.A < topright.A - laser.A) ;//{ x=25;y=9;} //hit topright corner
			else if (-rightbottom.A + bottomright.A < rightbottom.A - laser.A) ;//{ x=25;y=9;} //hit bottomright corner
			else if (bottomright.A - bottomleft.A < bottomright.A - laser.A) { x = (Program.W / 2d - gun.Y) / a; y = Program.W / 2 - gun.Y; } //hit bottom
			else if (-bottomleft.A + leftbottom.A < bottomleft.A - laser.A) ;//{ x=25;y=9;} //hit bottomleft corner
			else if (leftbottom.A - lefttop.A < leftbottom.A - laser.A) { x = Program.W-gun.X; y = a * x; } //hit left
			else { x = Program.W - gun.X; y = a * x; } //hit right

			laser.L = Sqrt(x * x + y * y);
			//Program.window.CreateGraphics().DrawLine(System.Drawing.Pens.Blue, (System.Drawing.PointF) gun, (System.Drawing.PointF) (gun + ~laser * Program.W));
			//Program.window.CreateGraphics().FillEllipse(System.Drawing.Brushes.Red, new System.Drawing.RectangleF((System.Drawing.PointF) (gun + laser - new IVector(3, 3)), new System.Drawing.SizeF(6, 6)));
			return laser.L;
		}*/

		/*public static HashSet<Circle> AllEntities() {
			HashSet<Circle> all = new HashSet<Circle>();

			foreach (Circle head in Program.HEAD.Values) all.Add(new Circle(head));
			foreach (Circle orb in Entities.Orb.All) all.Add(new Circle(orb));
			foreach (Circle blast in Entities.Blast.All) all.Add(new Circle(blast));

			return all;
		}*/
	}
}
