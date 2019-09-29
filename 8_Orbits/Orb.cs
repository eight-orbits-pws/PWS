using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;

namespace Eight_Orbits.Entities {
	class Orb : Circle, Visual {
		public static List<Orb> All = new List<Orb>(256);
		public static volatile object OrbLock = new { };

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
		private Animatable contrast_color = new Animatable(0, 5);

		public Orb() : this(false) { }

		public Orb(bool spawn) {
			if (All.Count >= 256) return;
			this.color = Color.White;
			this.pos = spawn? Map.generateSpawnStartRound(OrbR) : Map.generateSpawn(OrbR);
			this.r = OrbR;
			this.v = IVector.Zero;

			lock (OrbLock) All.Add(this);
			Map.OnClearRemove += Remove;
			OnUpdate += Update;
			window.DrawWhite += Draw;
			window.UpdateColors += update_color;
		}

		public void Remove() {
			OnUpdate -= Update;
			window.UpdateColors -= update_color;
			lock (OrbLock) All.Remove(this);
		}

		public void Pew() {
			this.pos = HEADS[owner].pos;
			this.v = HEADS[owner].v;
			this.state = OrbStates.BULLET;
			this.isBullet = true;
			OnUpdate += Update;
			window.DrawBullet += Draw;
		}

		volatile object update_lock = new { };

		public void Update() {
			lock (update_lock) {
				switch (state) {
					case OrbStates.WHITE:
						if (r < OrbR) color = Color.Red;
						v.L -= Math.Sqrt(Math.Max(0, v.L)) / 26d;
						if (v.L < 0.125)
							v.L = 0;
						break;

					case OrbStates.BULLET:
						this.bulletTime++;
						v.L = PHI * speed + speed * 2 / Math.Sqrt(this.bulletTime);

						if (bulletTime >= 12 && Map.OutOfBounds(pos, OrbR)) {
							color = Color.White;
							state = OrbStates.WHITE;
							bulletTime = 0;
							owner = Keys.None;
							killstreak = 0;
							isBullet = false;
							window.DrawBullet -= Draw;
							window.DrawWhite += Draw;
						}
						break;

					default:
						return;
				}

				//collisions
				if (pos.X < OrbR)
					v.X = Math.Abs(v.X);
				else if (pos.X > W - OrbR)
					v.X = -Math.Abs(v.X);

				else if (pos.Y < OrbR)
					v.Y = Math.Abs(v.Y);
				else if (pos.Y > W / 2 - OrbR)
					v.Y = -Math.Abs(v.Y);

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
		}

		public void Move(IPoint log) {
			lock (update_lock) {
				if (isBullet)
					return;
				if (state == OrbStates.OWNER) {
					v.A = pos ^ log;
					v.L = Math.Min(pos * log, speed * 2d);
				} else {
					IVector n = log - pos;
					v.L = n.L = speed;

					v += n;
				}

				pos += v * .999998d;
			}
		}

		public void Draw(Graphics g) {
			Brush clr;
			if (state == OrbStates.TRAVELLING) {
				clr = Brushes.White;
				if (owner != Keys.None && HEADS[owner].Died) newOwner();
			} else if (state == OrbStates.BULLET) {
				double c = Math.Pow(Math.Sin(bulletTime / 8), 2) * 2 / 3;
				clr = new SolidBrush(Color.FromArgb((int)(color.R + (255 - color.R) * c), (int)(color.G + (255 - color.G) * c), (int)(color.B + (255 - color.B) * c)));
			} else
				clr = new SolidBrush(color);

			g.FillEllipse(clr, (float) pos.X - this.r, (float) pos.Y - this.r, r * 2, r * 2);
			if (ContrastMode && state.GetHashCode() >= 3) {
				g.DrawEllipse(Pens.Black, (float)pos.X - r, (float)pos.Y - r, r * 2, r * 2);
				contrast_color.Set(BoolToInt(Map.InOrbit(pos)&&!isBullet));
				g.FillEllipse(new SolidBrush(AnimatableColor.Lurp(Color.Black, Color.White, (float) contrast_color)), (float) pos.X - 3, (float) pos.Y - 3, 6, 6);
			}

			//debug draw ID
			//g.DrawString(this.ID.ToString(), new Font(FONT, 8), Brushes.Gray, (PointF) pos);
		}

		public void DrawKills(Graphics g) {
			g.TranslateTransform((float) pos.X, (float) pos.Y);
			g.RotateTransform((float) (v.A / Math.PI * 180d + 90d));
			if (ContrastMode) g.FillEllipse(new SolidBrush(color), -4,-4,8,8);

			string str;
			if (this.owner != Keys.None) str = HEADS[owner].Kills.ToString();
			else {
				str = "!";
			}
			Font font = new Font(FONT, r);
			SizeF sz = g.MeasureString(str, font);
			g.DrawString(str, font, Brushes.White, -sz.Width / 2, -sz.Height / 2);

			g.ResetTransform();
		}

		public void newOwner() {
			lock (OrbLock) {
				this.owner = Keys.None;
				this.color = Color.White;
				this.state = OrbStates.WHITE;
				this.r = OrbR;
				OnUpdate += Update;
				window.DrawWhite += Draw;
				eaten = false;
				info = true;
			}
		}

		public void newOwner(Keys newowner) {
			lock (OrbLock) {
				OnUpdate -= Update;
				window.DrawWhite -= Draw;
				if (state == OrbStates.SPAWN) Map.newOrb();
				if (TutorialActive && newowner != Keys.F13 && newowner != Keys.F14 && state == OrbStates.SPAWN) new Animation(pos, 80, 0, W, HeadR, (float)PHI * HeadR, Color.FromArgb(150, 255, 255, 255), 0);
				eaten = true;
				info = false;
				this.owner = newowner;
				this.state = OrbStates.TRAVELLING;
				this.color = HEADS[newowner].color;
			}
		}

		public bool noOwner() {
			return this.owner == Keys.None;
		}

		private void update_color() {
			if (this.owner != Keys.None) this.color = HEADS[owner].color;
		}
    }
}
