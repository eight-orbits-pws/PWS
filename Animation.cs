using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Eight_Orbits {
	public class Animatable {
		private float b;
		private float c;
		private float e;

		private int startTick;
		private readonly int duration;
		private readonly bool keep;
		private readonly AnimationTypes type;

		public event Action OnEnd;

		private bool ended;

		public Animatable(int duration)						: this(0, 0, duration, AnimationTypes.LINEAR, false) {}
		public Animatable(float f, int duration, bool keep)	: this(f, f, duration, AnimationTypes.LINEAR, keep) {}
		public Animatable(float b, float e, int duration)	: this(b, e, duration, AnimationTypes.LINEAR, false) {}
		public Animatable(float b, float e, int duration, AnimationTypes type)	: this(b, e, duration, type, false) {}
		public Animatable(float b, float e, int duration, AnimationTypes type, bool keep) {
			ended = false;
			OnEnd = null;

			this.b = b;
			this.c = b;
			this.e = e;
			this.startTick = Program.Tick;
			this.duration = duration;
			this.keep = keep;
			this.type = type;

			this.startTick = Program.Tick;

			if (keep) Program.OnUpdate += update;
			else if (Program.SyncUpdate) Program.OnUpdateAnimation += update;
			else {
				this.ended = true;
				this.c = e;
			}
		}

		public Animatable(Animatable parent) {
			ended = false;
			OnEnd = null;

			this.b = parent.b;
			this.c = parent.c;
			this.e = parent.e;
			this.startTick = parent.startTick;
			this.duration = parent.duration;
			this.type = parent.type;
			
			this.keep = false;
			if (Program.SyncUpdate) Program.OnUpdateAnimation += update;
		}

		public void Remove() {
			this.OnEnd = null;
			this.c = this.e;
			if (keep) Program.OnUpdate -= update;
			else Program.OnUpdateAnimation -= update;
		}

		public void Set(float n) {
			if (n == this.e) return;
			if (Program.SyncUpdate) ended = false;
			this.startTick = Program.Tick;
			this.b = Program.SyncUpdate? this.c : n;
			this.e = n;
		}
		public bool Ended => !Program.SyncUpdate || (Program.Tick - startTick >= duration && !ended);

		public void Reset() {
			ended = false;
			this.startTick = Program.Tick;
			this.c = this.b;
		}

		private void update() {
			double p;
			if (ended) {
				p = 1;
			} else if (Ended) {
				p = 1;
				ended = true;
				OnEnd?.Invoke();
			} else {
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
		//public static HashSet<Animation> All = new HashSet<Animation>();

		private PointF pos;
		private readonly int duration;

		private readonly Animatable R;
		private readonly Animatable D;
		private readonly AnimatableColor color;

		private readonly int starttick = 0;

		private readonly PaintEvent draw;
		private readonly Action update;

		public Animation() {
			if (!Program.SyncUpdate) return;
			starttick = Program.Tick;
			Program.OnUpdateAnimation += update = new Action(Update);
			Program.window.DrawHead += draw = new PaintEvent(Draw);
			Program.Map.OnClearRemove += new Action(End);
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
			Program.OnUpdateAnimation -= update;
			Program.window.DrawHead -= draw;
			R.Remove();
			D.Remove();
			color.Remove();
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

		private readonly PaintEvent draw;
		private readonly Action update;

		public Coin(IPoint pos, int points, Color color) {
			if (points == 0 || !Program.SyncUpdate) return;
			this.font = new Font(Program.FONT, 16 * Program.Scale, FontStyle.Bold);
			this.text = "+" + points;
			SizeF sz = (SizeF) Program.window?.CreateGraphics().MeasureString(text, font);
			
			this.offset = -sz.Width/2f;
			this.pos = new PointF((float)pos.X - sz.Width/2, (float)pos.Y - sz.Height/2);
			this.shadow = new PointF((float) pos.X - sz.Width/2 + 1f, (float) pos.Y - sz.Height/2 + 1f);
			this.m = new Animatable(-.72f, 0, 100, AnimationTypes.SIN);
			this.c = new SolidBrush(color);

			Program.OnUpdateAnimation += update = new Action(Update);
			Program.window.DrawAnimation += draw = new PaintEvent(Draw);
			m.OnEnd += new Action(Remove);
		}

		public Coin(IPoint pos, string text, Color color) : this(pos, -1, color) {
			if (!Program.SyncUpdate) return;
			this.text = text;
			SizeF sz = Program.window.CreateGraphics().MeasureString(text, font);
			this.offset = -sz.Width/2f;
			this.pos = new PointF((float)pos.X - sz.Width/2, (float)pos.Y - sz.Height/2);
			this.shadow = new PointF((float) pos.X - sz.Width/2 + 1f, (float) pos.Y - sz.Height/2 + 1f);
		}

		public Coin(Animatable x, float offset, float y, int points, Color color) : this(new IPoint((float) x + offset, y), points, color) {
			this.x = new Animatable(x);
			this.offset += offset;
		}

		~Coin() {
			this.Remove();
		}

		public void Update() {
			if (x != null) pos.X = (float) x + offset;
		
			pos.Y += m;
			shadow.Y += m;
		}

		public void Draw(Graphics g) {
			if (Program.ContrastMode) g.DrawString(text, font, Brushes.Black, pos.X + 1f, pos.Y + 1f);
			g.DrawString(text, font, c, pos);
		}

		public void Remove() {
			Program.OnUpdateAnimation -= update;
			Program.window.DrawAnimation -= draw;
			m?.Remove();
			x?.Remove();
		}
	}
	
	public class AnimatableColor {
		public Color b;
		public Color e;
		private readonly Animatable p;

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

		public void Remove() {
			p.Remove();
		}

		public static explicit operator Color (AnimatableColor value) {
			return Lurp(value.b, value.e, (float) (value.p));
		}

		public static Color Lurp(Color a, Color b, float d) {
			return Color.FromArgb((int)(a.A + (b.A - a.A) * d), (int)(a.R + (b.R - a.R) * d), (int)(a.G + (b.G - a.G) * d), (int)(a.B + (b.B - a.B) * d));
		}
	}
}
