using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;

namespace Eight_Orbits.Entities {
	class Orb : Circle, Visual {
		public static List<Orb> All = new List<Orb>(256);

		public OrbStates state = OrbStates.SPAWN;
		private short bulletTime = 0;
		private byte killstreak = 0;
		public byte KillStreak { set {
				if (isBullet) killstreak = value;
				else killstreak = 0;
			} get {
				if (isBullet) return killstreak;
				else return 0;
		}	}
		public Keys owner = Keys.None;

		public bool eaten = false;
		public bool info = false;
		public bool isBullet = false;

		private byte id = 0;
		public byte ID { get { return id; } }
		
		//public IPoint pos;
		//public IVector v;
		//public float r;
		//public Brush color;

		public Orb() {
			this.id = (byte) All.Count;
			this.color = Color.White;
			this.pos = Map.generateSpawn(OrbR);
			this.r = OrbR;
			this.v = IVector.Zero;

			All.Add(this);
			Map.OnClear += Remove;
			OnUpdate += Update;
			window.DrawWhite += Draw;
		}

		public void Remove() {
			Map.OnClear -= Remove;
			OnUpdate -= Update;
			window.DrawWhite -= Draw;
			window.DrawBullet -= Draw;
			All.Remove(this);
		}

		public void Pew() {
			this.pos = HEAD[owner].pos;
			this.v = HEAD[owner].v;
			this.state = OrbStates.BULLET;
			this.isBullet = true;
			//MapOrbs.Add(this);
			window.DrawBullet += Draw;
		}

		public void Update() {
			switch (state) {
				case OrbStates.WHITE:
					//v.L = Math.Min(v.L, speed);
					v.L -= Math.Sqrt(Math.Max(0, v.L)) / 64d;
					if (v.L < 0.125) v.L = 0;
					break;

				case OrbStates.BULLET:
					this.bulletTime++;
					v.L = PHI * speed + speed * 2 / Math.Sqrt(this.bulletTime);

					if (bulletTime > 25 && Map.OutOfBounds(pos, OrbR)) {
						color = Color.White;
						state = OrbStates.WHITE;
						bulletTime = 0;
						if (killstreak > 1) MVP.Add(MVPTypes.COLLATERAL, HEAD[owner].DisplayKey, killstreak.ToString());
						owner = Keys.None;
						killstreak = 0;
						isBullet = false;
						window.DrawBullet -= Draw;
						window.DrawWhite += Draw;
					} break;

				default: return;
			}

			//collisions
			if (pos.X < OrbR) v.X = Math.Abs(v.X);
			else if (pos.X > W - OrbR) v.X = -Math.Abs(v.X);

			else if (pos.Y < OrbR) v.Y = Math.Abs(v.Y);
			else if (pos.Y > W / 2 - OrbR) v.Y = -Math.Abs(v.Y);

			else if (pos.X + pos.Y < C + OrbR * sqrt2 && v * new IVector(-1, -1) > 0) {
				v.A += Math.PI / 4d;
				v.Y = Math.Abs(v.Y);
				v.A -= Math.PI / 4d;
			} else if (W - pos.X + pos.Y < C + OrbR * sqrt2 && v * new IVector(1, -1) > 0) {
				v.A -= Math.PI / 4d;
				v.Y = Math.Abs(v.Y);
				v.A += Math.PI / 4d;
			} else if (W / 2 + pos.X - pos.Y < C + OrbR * sqrt2 && v * new IVector(-1, 1) > 0) {
				v.A += Math.PI - Math.PI / 4d;
				v.Y = Math.Abs(v.Y);
				v.A += Math.PI / 4d - Math.PI;
			} else if (W + W / 2f - pos.X - pos.Y < C + OrbR * sqrt2 && v * new IVector(1, 1) > 0) {
				v.A += Math.PI / 4d + Math.PI;
				v.Y = Math.Abs(v.Y);
				v.A += -Math.PI - Math.PI / 4d;
			}

			//update
			pos += v;
		}

		public void Move(IPoint log) {
			if (isBullet) return;
			if (state == OrbStates.OWNER) {
				v.A = pos ^ log;
				v.L = Math.Min(pos * log, speed * 2d);
			} else {
				IVector n = log - pos;
				v.L = n.L = speed;

				v += n;
			}

			pos += v;
		}

		public void Draw(ref PaintEventArgs e) {
			Brush clr;
			if (state == OrbStates.TRAVELLING) {
				clr = Brushes.White;
			} else if (state == OrbStates.BULLET) {
				double c = Math.Pow(Math.Sin(bulletTime / 8), 2) * 2 / 3;
				clr = new SolidBrush(Color.FromArgb((int)(color.R + (255 - color.R) * c), (int)(color.G + (255 - color.G) * c), (int)(color.B + (255 - color.B) * c)));
			} else
				clr = new SolidBrush(color);

			e.Graphics.FillEllipse(clr, (float) pos.X - this.r, (float) pos.Y - this.r, r * 2, r * 2);
			if (ContrastMode && state.GetHashCode() >= 3) e.Graphics.DrawEllipse(Pens.Black, (float) pos.X-r, (float) pos.Y-r, r * 2, r * 2);
		}

		public void DrawKills(ref PaintEventArgs e) {
			e.Graphics.TranslateTransform((float) pos.X, (float) pos.Y);
			e.Graphics.RotateTransform((float) (v.A / Math.PI * 180d + 90d));

			string str = HEAD[owner].Kills.ToString();
			Font font = new Font(FONT, r);
			SizeF sz = e.Graphics.MeasureString(str, font);
			e.Graphics.DrawString(str, font, Brushes.Black, -sz.Width/2, -sz.Height/2);
			e.Graphics.ResetTransform();
		}

		public void newOwner() {
			this.owner = Keys.None;
			this.color = Color.White;
			this.state = OrbStates.WHITE;
			this.r = OrbR;
			//MapOrbs.Add(this);
			window.DrawWhite += Draw;
			eaten = false;
			info = true;
		}

		public void newOwner(Keys newowner) {
			OnUpdate -= Update;
			window.DrawWhite -= Draw;
			if (state == OrbStates.SPAWN) Map.newOrb();
			eaten = true;
			info = false;
			this.owner = newowner;
			this.state = OrbStates.TRAVELLING;
			this.color = HEAD[newowner].color;
		}

		public bool noOwner() {
			return this.owner == Keys.None;
		}

    }
}
