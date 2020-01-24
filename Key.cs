using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;

namespace Eight_Orbits {
	class IKey : Visual {
		private static float Ox000w = W;
		public static float WIDTH {
			get => Ox000w;
			set {
				Ox000w = value;
				foreach (Keys id in HEADS.Keys)
					HEADS[id].key.w.Set(Ox000w);
			}
		}
		public static readonly float HEIGHT = H - W / 2;

		public static void UpdateAll() {
			foreach (Keys id in HEADS.Keys) HEADS[id].key.Update();
		}

		private static int get_index(Keys key) {
			lock (ActiveLock) {
				if (ActiveKeys.Contains(key))
					return ActiveKeys.IndexOf(key);
				else
					return ActiveKeys.Count + InactiveKeys.IndexOf(key);
			}
		}

		//public byte index = 0;
		private readonly Animatable x = new Animatable(W, 48, true);
		private readonly Animatable w = new Animatable(0, 48, true);
		private readonly Animatable a = new Animatable(0, 10, true);
		public byte points = 0;
		public Keys owner = Keys.None;
		readonly string DKey;
		public bool pressed = false;
		private bool dead = false;
		//private Color color;
		private Color transit = Color.Black;

		private readonly Font std_font = new Font(FONT, 20 * SZR);

		public IKey(Keys head, string DisplayKey, Color color) {
			//index = (byte) Active.Count;
			WIDTH = W / (float) (ActiveKeys.Count + 1);
			w.Set(WIDTH);
			owner = head;
			DKey = DisplayKey;
			//this.color = color;
			window.DrawKeys += Draw;
		}

		public void Remove() {
			window.DrawKeys -= Draw;
			x.Remove();
			w.Remove();
			a.Remove();
		}

		public void Press() {
			if (dead) return;
			pressed = true;
			transit = Color.White;
			a.Set(1);
		}

		public void Release() {
			if (dead) return;
			pressed = false;
			a.Set(0);
		}

		public void Revive() {
			dead = false;
			a.Set(0);
		}

		public void Die() {
			dead = true;
			pressed = false;
			transit = Color.Black;
			a.Set(1);
		}

		public void Add(int pts) => _=new Coin(this.x, this.w / 2, W / 2 + HEIGHT * 3 / 4, pts, Color.White);

		public void Update() => this.x.Set(get_index(owner) * WIDTH);

		public void Draw(Graphics g) {
			Color color = ChaosMode? Color.Red : HEADS[owner].color;
			Color txtColor;

			if (pressed) txtColor = AnimatableColor.Lurp(transit, color, (float) a);
			else txtColor = Color.White;

			g.FillRectangle(new SolidBrush(AnimatableColor.Lurp(color, transit, (float) a)), (float) x, W/2, (float) w, HEIGHT);
			Font font;
			SizeF sz = g.MeasureString(DKey, std_font);
			if (sz.Width > WIDTH) {
				font = new Font(FONT, 20 * SZR * WIDTH / sz.Width);
				sz = g.MeasureString(DKey, font);
			} else font = new Font(std_font, FontStyle.Regular);

			g.DrawString(DKey, font, new SolidBrush(txtColor), x + w/2 - sz.Width/2, W/2 + HEIGHT / 4 - sz.Height/2);

			string pts = points.ToString();
			sz = g.MeasureString(pts, std_font);
			if (sz.Width > WIDTH) {
				font = new Font(FONT, 20 * SZR * WIDTH / sz.Width);
				sz = g.MeasureString(pts, font);
			} else font = new Font(std_font, FontStyle.Regular);

			g.DrawString(pts, font, new SolidBrush(txtColor), x + w/2 - sz.Width/2, W/2 + HEIGHT * 3/4 - sz.Height/2);
		}
	}
}
