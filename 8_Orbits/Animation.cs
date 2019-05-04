using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Eight_Orbits {

	public class Animation : Visual {
		private PointF pos;
		private int duration;

		private Animatable R;
		private Animatable D;
		private AnimatableColor color;

		private int tick = 0;

		public Animation() {
			
		}

		public Animation(IPoint start, int ticks, float fromR, float toR, float fromD, float toD, Color fromColor, int toAlpha) {
			this.pos = (PointF) start;
			this.duration = ticks;
			this.R = new Animatable(fromR, toR, ticks);

			this.D = new Animatable(fromD, toD, ticks);

			this.color = new AnimatableColor(fromColor, Color.FromArgb(toAlpha, fromColor), duration);
		}

		public Animation(IPoint start, int ticks, float fromR, float toR, float fromD, float toD, Color fromColor, Color toColor) {
			this.pos = (PointF) start;
			this.duration = ticks;
			this.R = new Animatable(fromR, toR, ticks);

			this.D = new Animatable(fromD, toD, ticks);

			this.color = new AnimatableColor(fromColor, toColor, duration);
		}

		public void Update() {
			tick++;
			if (tick >= duration) End();
		}

		public void Draw(ref PaintEventArgs e) {
			Pen pen = new Pen((Color) color, (float) D);
			e.Graphics.DrawEllipse(pen, pos.X - R - D/2f, pos.Y - R - D/2, R*2f+D, R*2f+D);
		}

		public void End() {
			AnimationControl.Remove(this);
		}
	}
	/// <summary>
	/// new
	/// </summary>
	static class AnimationControl {
		public static bool Disabled = false;

		private static HashSet<Animation> Set = new HashSet<Animation>();
		private static HashSet<Animation> End = new HashSet<Animation>();

		public static void Add(Animation a) {
			if (Disabled) return;

			Set.Add(a);
		}

		public static void Remove(Animation a) {
			if (Disabled) return;
			
			End.Add(a);
		}

		public static void Draw(ref PaintEventArgs e) {
			if (Disabled) return;

			foreach (Animation a in Set) { a.Update(); a.Draw(ref e); }
			foreach (Animation a in End) Set.Remove(a);

			End.Clear();
		}

		public static Color Lurp(Color a, Color b, float d) {
			return Color.FromArgb((int)(a.A + (b.A - a.A) * d), (int)(a.R + (b.R - a.R) * d), (int)(a.G + (b.G - a.G) * d), (int)(a.B + (b.B - a.B) * d));
			//} catch (ArgumentException) {
				//return a;
			//}
		}
	}

	public class Animatable {
		private float b;
		private float c;
		private float e;
		private int tick;

		private int startTick;
		private int duration;
		private AnimationTypes type;

		public Animatable(int duration) : this(0, 0, duration) { }

		public Animatable(float f, int duration) : this(f, f, duration) { }

		public Animatable(float b, float e, int duration) : this(b, e, duration, AnimationTypes.LINEAR) { }

		public Animatable(float b, float e, int duration, AnimationTypes type) {
			this.b = b;
			this.c = b;
			this.e = e;
			this.tick = this.startTick = Program.Tick;
			this.duration = duration;
			this.type = type;
			if (Program.AnimationsEnabled) Program.OnUpdate += Calc;
		}

		public void Set(float n) {
			if (n == e || !Program.AnimationsEnabled) return;
			this.b = c;
			this.e = n;
			this.startTick = Program.Tick;
		}

		public event GameEvent OnEnd;

		public bool Ended() {
			return Program.Tick - startTick == duration;
		}

		public void Reset() {
			this.startTick = Program.Tick;
		}

		private void Calc() {
			if (Ended()) OnEnd?.Invoke();
			double p = (double) (Program.Tick - startTick) / duration;
			p = Math.Max(0, Math.Min(p, 1));

			switch (type) {
				case AnimationTypes.LINEAR: break;

				case AnimationTypes.SQRT:
					p = Math.Sqrt(p);
					break;

				case AnimationTypes.SQUARED:
					p = Math.Pow(p, 2);
					break;

				case AnimationTypes.CUBED:
					p = Math.Pow(p, 3);
					break;

				case AnimationTypes.SIN:
					p = Math.Sin(p * Math.PI / 2);
					break;

				case AnimationTypes.COS:
					p = Math.Cos(p * Math.PI / 2);
					break;
			}
			c = (float) (b + (e - b) * p);
		}

		public static float operator +(Animatable a) { return a.c; }
		public static float operator +(float a, Animatable b) { return a + b.c; }
		public static float operator +(Animatable a, float b) { return a.c + b; }
		public static float operator -(Animatable a) { return -a.c; }
		public static float operator -(float a, Animatable b) { return a - b.c; }
		public static float operator -(Animatable a, float b) { return a.c - b; }
		public static float operator *(Animatable a, float b) { return a.c * b; }
		public static float operator *(float b, Animatable a) { return a.c * b; }
		public static float operator /(Animatable a, float b) { return a.c / b; }
		public static explicit operator int (Animatable a) { return (int) a.c; }
		public static explicit operator float (Animatable a) { return a.c; }
	}

	public class AnimatableColor {
		public Color b;
		public Color e;
		public int StartTick;
		public int duration;

		public AnimatableColor(Color b, Color e, int duration) {
			this.b = b;
			this.e = e;
			this.StartTick = Program.Tick;
			this.duration = duration;
		}

		public static explicit operator Color (AnimatableColor value) {
			float d = (float) (Program.Tick - value.StartTick) / value.duration;
			if (0 < d && d < 1) return AnimationControl.Lurp(value.b, value.e, (float) (Program.Tick - value.StartTick) / value.duration);
			else return value.e;
		}
	}
}
