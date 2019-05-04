using System;
using System.Collections.Generic;
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

		public IVector Copy() {
			return new IVector(this.x, this.y);
		}

		public static IVector operator +(IVector a, IVector b) {
			return new IVector(a.X + b.X, a.Y + b.Y);
		}

		public static IVector operator -(IVector a, IVector b) {
			return new IVector(a.X - b.X, a.Y - b.Y);
		}

		public static IVector operator -(IVector v) {
			return new IVector(-v.X, -v.Y);
		}

		public static IVector operator *(double scalar, IVector v) {
			return new IVector(scalar * v.X, scalar * v.Y);
		}

		public static IVector operator *(IVector v, double scalar) {
			return new IVector(scalar * v.X, scalar * v.Y);
		}

		public static IVector operator /(IVector v, double d) {
			return new IVector(v.X / d, v.Y / d);
		}

		//dot product
		public static double operator *(IVector a, IVector b) {
			return a.X * b.X + a.Y * b.Y;
		}

		//normalize
		public static IVector operator ~(IVector v) {
			v.L = 1;
			return v;
		}

		public void Normalize() {
			l = 1;
			x = Cos(a);
			y = Sin(a);
		}

		public static IVector Up { get { return new IVector(0, -1); } }
		public static IVector Right { get { return new IVector(1, 0); } }
		public static IVector Down { get { return new IVector(0, 1); } }
		public static IVector Left { get { return new IVector(-1, 0); } }
		public static IVector Zero { get { return new IVector(0, 0); } }
	}

	/// <summary>
	/// Point
	/// </summary>
	public struct IPoint {

		private double x;
		private double y;

		public double X {
			get { return x;  }
			set { x = value; }
		}

		public double Y {
			get { return y; }
			set { y = value; }
		}

		public IPoint(double x, double y) {
			this.x = x;
			this.y = y;
		}

		public IPoint Copy() {
			return new IPoint(x, y);
		}

		public static IPoint Center { get { return new IPoint(Program.W / 2, Program.W / 4); } }

		public static explicit operator System.Drawing.PointF (IPoint p) {
			return new System.Drawing.PointF((float) p.X, (float) p.Y);
		}

		public static explicit operator System.Drawing.Point (IPoint p) {
			return new System.Drawing.Point((int) p.X, (int) p.Y);
		}

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
		public static IVector operator -(IPoint a, IPoint b) {
			return new IVector(a.X - b.X, a.Y - b.Y);
		}

		//distance between
		public static double operator *(IPoint a, IPoint b) {
			return Sqrt(Pow(b.X - a.X, 2) + Pow(b.Y - a.Y, 2));
		}

		//angle between
		public static double operator ^(IPoint a, IPoint b) {
			return Atan2(b.Y - a.Y, b.X - a.X);
		}

		public override string ToString() {
			return Round(x).ToString() + ", " + Round(y).ToString();
		}
	}

	public struct Ray {
		//start, end, hitreg bools
	}
}
