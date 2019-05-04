using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;

namespace Eight_Orbits {
	class IKey : Visual {
		public static float Width = W;
		public static float Height = H - W / 2;

		public byte index = 0;
		private Animatable x = new Animatable(W, 48);
		private Animatable w = new Animatable(0, 48);
		private Animatable a = new Animatable(0, 10);
		public byte points = 0;
		public Keys owner = Keys.None;
		string DKey;
		private bool pressed = false;
		private bool dead = false;
		private Color color;
		private Color transit = Color.Black;

		public IKey(Keys head, string DisplayKey, Color color) {
			//keylist.Add(this);
			index = (byte) Active.Count;
			Width = W / (index + 1);
			Height = H - W / 2;
			owner = head;
			DKey = DisplayKey;
			this.color = color;

			window.DrawKeys += Draw;
		}

		public void Remove() {
			window.DrawKeys -= Draw;
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

		public void Update() { return; }

		public void Draw(ref PaintEventArgs e) {
			Color txtColor;
			if (Active.Contains(owner)) {
				index = (byte)Active.IndexOf(owner);
			} else {
				index = (byte)(Active.Count + Dead.IndexOf(owner));
			}

			if (pressed) txtColor = AnimationControl.Lurp(transit, color, (float) a);
			else txtColor = Color.White;

			w.Set(Width);
			x.Set(index * Width);
			e.Graphics.FillRectangle(new SolidBrush(AnimationControl.Lurp(color, transit, (float) a)), (float) x, W/2, (float) w, Height);
			Font font = new Font(FONT, 20 * SZR);
			SizeF sz = e.Graphics.MeasureString(DKey, font);
			sz.Width /= 2;
			sz.Height /= 2;
			e.Graphics.DrawString(DKey, font, new SolidBrush(txtColor), x + w/2 - sz.Width, W/2 + Height / 4 - sz.Height);

			string pts = points.ToString();
			sz = e.Graphics.MeasureString(pts, font);
			sz.Width /= 2;
			sz.Height /= 2;
			e.Graphics.DrawString(pts, font, new SolidBrush(txtColor), x + w/2 - sz.Width, W/2 + Height * 3/4 - sz.Height);
		}
	}
}
