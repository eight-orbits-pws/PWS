using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Eight_Orbits {
	public class Animatable {
		private float b;
		private float c;
		private float e;

		private int startTick;
		private int duration;
		private AnimationTypes type;

		public Animatable(int duration)						: this(0, 0, duration, AnimationTypes.LINEAR) {}
		public Animatable(float f, int duration)			: this(f, f, duration, AnimationTypes.LINEAR) {}
		public Animatable(float b, float e, int duration)	: this(b, e, duration, AnimationTypes.LINEAR) {}

		public Animatable(float b, float e, int duration, AnimationTypes type) {
			this.b = b;
			this.c = b;
			this.e = e;
			this.startTick = Program.Tick;
			this.duration = duration;
			this.type = type;

			this.startTick = Program.Tick;
			Program.OnUpdate += update;
		}

		public void Remove() {
			this.OnEnd = null;
			Program.OnUpdate -= update;
		}

		public void Set(float n) {
			if (n == this.e) return;
			ended = false;
			this.startTick = Program.Tick;
			this.b = this.c;
			this.e = n;
		}

		public event Action OnEnd;

		private bool ended = false;
		public bool Ended => Program.Tick - startTick >= duration && !ended;

		public void Reset() {
			ended = false;
			this.startTick = Program.Tick;
		}
		
		private void update() {
			double p;
			if (Ended) {
				p = 1;
				ended = true;
				OnEnd?.Invoke();
			} else if (ended)
				p = 1;
			else {
				p = (double)(Program.Tick - startTick) / duration;
				p = Math.Max(0, Math.Min(p, 1));

				switch (type) {
					case AnimationTypes.LINEAR:
						break;

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
						p = Math.Cos((1-p) * Math.PI / 2);
						break;
				}
			}

			this.c = (float) (b + (this.e - b) * p);
		}

		public static float operator +(Animatable a) => a.c;
		public static float operator +(float a, Animatable b) => a + b.c;
		public static float operator +(Animatable a, float b) => a.c + b;
		public static float operator -(Animatable a) => -a.c;
		public static float operator -(float a, Animatable b) => a - b.c;
		public static float operator -(Animatable a, float b) => a.c - b;
		public static float operator *(Animatable a, float b) => a.c * b;
		public static float operator *(float b, Animatable a) => a.c * b;
		public static float operator /(Animatable a, float b) => a.c / b;
		public static explicit operator int(Animatable a) { return (int)a.c; }
		public static explicit operator float(Animatable a) => a.c;
	}

	public class Animation : Visual {
		public static HashSet<Animation> All = new HashSet<Animation>();

		private PointF pos;
		private readonly int duration;

		private readonly Animatable R;
		private readonly Animatable D;
		private readonly AnimatableColor color;

		private readonly int starttick = 0;

		public Animation() {
			starttick = Program.Tick;
			Program.OnUpdate += Update;
			Program.window.DrawHead += Draw;
			Program.Map.OnClearRemove += End;
		}

		public Animation(IPoint start, int ticks, float fromR, float toR, float fromD, float toD, Color fromColor, int toAlpha) : this() {
			this.pos = (PointF) start;
			this.duration = ticks;
			this.R = new Animatable(fromR, toR, ticks);
			this.D = new Animatable(fromD, toD, ticks);
			this.color = new AnimatableColor(fromColor, Color.FromArgb(toAlpha, fromColor), duration);
		}

		public Animation(IPoint start, int ticks, float fromR, float toR, float fromD, float toD, Color fromColor, Color toColor) : this() {
			this.pos = (PointF) start;
			this.duration = ticks;
			this.R = new Animatable(fromR, toR, ticks);
			this.D = new Animatable(fromD, toD, ticks);
			this.color = new AnimatableColor(fromColor, toColor, duration);
		}

		public Animation(IPoint start, int ticks, float fromR, float toR, float fromD, float toD, Color fromColor, Color toColor, AnimationTypes type) : this() {
			this.pos = (PointF) start;
			this.duration = ticks;
			this.R = new Animatable(fromR, toR, ticks, type);
			this.D = new Animatable(fromD, toD, ticks, type);
			this.color = new AnimatableColor(fromColor, toColor, duration, type);
		}
		
		public Animation(IPoint start, int ticks, float fromR, float toR, float fromD, float toD, Color fromColor, int toAlpha, AnimationTypes type) : this() {
			this.pos = (PointF) start;
			this.duration = ticks;
			this.R = new Animatable(fromR, toR, ticks, type);
			this.D = new Animatable(fromD, toD, ticks, type);
			this.color = new AnimatableColor(fromColor, Color.FromArgb(toAlpha, fromColor), duration, type);
		}

		public void Update() {
			if (Program.Tick - starttick >= duration) End();
		}

		public void Draw(Graphics g) {
			g.DrawEllipse(new Pen((Color) color, (float) D), pos.X - R - D/2f, pos.Y - R - D/2, R*2f+D, R*2f+D);
		}

		public void End() {
			Program.OnUpdate -= Update;
			Program.window.DrawHead -= Draw;
		}
	}

	public class Coin : Visual {
		
		PointF pos;
		PointF shadow;
		readonly Animatable m;
		readonly Animatable x;
		readonly float offset;
		readonly Brush c;
		readonly string text;
		readonly Font font;

		public Coin(IPoint pos, int points, Color color) {
			if (points == 0) return;
			this.font = new Font(Program.FONT, 16 * Program.Scale, FontStyle.Bold);
			this.text = "+" + points;
			SizeF sz = Program.window.CreateGraphics().MeasureString(text, font);
			this.offset = -sz.Width/2;
			this.pos = new PointF((float)pos.X - sz.Width/2, (float)pos.Y - sz.Height/2);
			this.shadow = new PointF((float) pos.X - sz.Width/2 + 1f, (float) pos.Y - sz.Height/2 + 1f);
			this.m = new Animatable(-.72f, 0, 100, AnimationTypes.SIN);
			this.c = new SolidBrush(color);

			Program.window.OnUpdateAnimation += Update;
			Program.window.DrawAnimation += Draw;
			m.OnEnd += Remove;
		}

		public Coin(ref Animatable x, float offset, float y, int points, Color color) : this(new IPoint((float) x + offset, y), points, color) {
			this.x = x;
			this.offset += offset;
		}

		public void Update() {
			if (x != null) {
				pos.X = (float) x + offset;
			}
			pos.Y += m;
			shadow.Y += m;
		}

		public void Draw(Graphics g) {
			if (Program.ContrastMode) g.DrawString(text, font, Brushes.Black, pos.X + 1f, pos.Y + 1f);
			g.DrawString(text, font, c, pos);
		}

		public void Remove() {
			Program.window.OnUpdateAnimation -= Update;
			Program.window.DrawAnimation -= Draw;
			m.OnEnd -= Remove;
		}
	}
	
	public class AnimatableColor {
		public Color b;
		public Color e;
		private Animatable p;

		public AnimatableColor(Color b, Color e, int duration) {
			this.b = b;
			this.e = e;
			this.p = new Animatable(0, 1, duration);
		}

		public AnimatableColor(Color b, Color e, int duration, AnimationTypes type) {
			this.b = b;
			this.e = e;
			this.p = new Animatable(0, 1, duration, type);
		}

		public static explicit operator Color (AnimatableColor value) {
			return Lurp(value.b, value.e, (float) (value.p));
		}

		public static Color Lurp(Color a, Color b, float d) {
			return Color.FromArgb((int)(a.A + (b.A - a.A) * d), (int)(a.R + (b.R - a.R) * d), (int)(a.G + (b.G - a.G) * d), (int)(a.B + (b.B - a.B) * d));
		}
	}
}
