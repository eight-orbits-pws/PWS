using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static Eight_Orbits.Program;

namespace Eight_Orbits.Entities {
	class Orb : Circle, Visual {
		public static readonly List<Orb> All = new List<Orb>(256);
		public static readonly object OrbLock = new { };

        public volatile byte state = 0; //SPAWN, WHITE, TRAVELLING, BULLET, OWNER
		private volatile short bulletTime = 0;
		private byte killstreak = 0;
		public byte KillStreak { set {
				if (isBullet) killstreak = value;
				else killstreak = 0;
			} get {
				if (isBullet) return killstreak;
				else return 0;
		}	}
		public volatile Keys Owner = Keys.None;

		//public bool eaten = false;
		//public bool info = false;
		public bool isBullet => this.state == (byte) OrbStates.BULLET;
		public bool isWhite => this.state == (byte) OrbStates.WHITE || this.state == (byte) OrbStates.SPAWN;
		public bool isDangerTo(Keys _key) => (this.state == (byte) OrbStates.OWNER || this.state == (byte) OrbStates.BULLET) && this.Owner != _key && !(KingOfTheHill && HEADS[Owner].INVINCIBLE);

		private readonly Animatable contrast_color = new Animatable(0, 5, false);

		public readonly byte ID;

		public Orb() : this(false) { }

		public Orb(bool spawn) {
			if (All.Count >= Map.MaxOrbs) return;
			this.color = Color.White;
			this.pos = spawn? Map.generateSpawnStartRound(OrbR) : Map.generateSpawn(OrbR);
			this.r = OrbR;
			this.v = IVector.Zero;
			this.ID = (byte) All.Count;

			if (Hidden) new Animation(pos, 12, 0, 0, r, r, color, color);

			lock (OrbLock) All.Add(this);
			Map.OnClearRemove += Remove;
			OnUpdate += Update;
			window.DrawWhite += Draw;
			window.UpdateColors += update_color;
		}

		public void Remove() {
			OnUpdate -= Update;
			window.DrawWhite -= Draw;
			window.DrawTail -= Draw;
			window.DrawBullet -= Draw;
			window.UpdateColors -= update_color;
			contrast_color.Remove();
			// removing out of All happens in Map.Clear()
		}

		public void Pew() {
			lock (OrbLock) {
				if (Owner == Keys.None) return;
				this.pos = HEADS[Owner].pos;
				this.v = HEADS[Owner].v;
				this.state = (byte) OrbStates.BULLET;
				OnUpdate += Update;
				window.DrawBullet += Draw;
			}
		}

		readonly object update_lock = new { };

		double d;
		IVector n;
		public void Update() {
			lock (update_lock) {
				switch (state) {
					case 1:
						if (r < OrbR) color = Color.Red;
						v.L -= Math.Sqrt(Math.Max(0, v.L)) / 26d;
						if (v.L < 0.125)
							v.L = 0;
						break;

					case 3:
						this.bulletTime++;
						v.L = PHI * speed + speed * 2 / Math.Max(1, Math.Sqrt(this.bulletTime));

						if (bulletTime >= 9 && Map.OutOfBounds(pos, OrbR)) {
							if (YeetMode && !HEADS[Owner].Died) {
								HEADS[Owner].Eat(ID);
							} else {
								color = Color.White;
								state = (byte) OrbStates.WHITE;
								bulletTime = 0;
								Owner = Keys.None;
								window.DrawWhite += Draw;
							}
							
							bulletTime = 0;
							killstreak = 0;
							window.DrawBullet -= Draw;
						}
						break;

					default:
						return;
				}

				//collisions
				if (pos.X < OrbR) {
					v.X = Math.Abs(v.X);
					pos.X = 2d * OrbR - pos.X;
				} else if (pos.X > W - OrbR) {
					v.X = -Math.Abs(v.X);
					pos.X = 2d * (W - OrbR) - pos.X;
				} else if (pos.Y < OrbR) {
					v.Y = Math.Abs(v.Y);
					pos.Y = 2 * OrbR - pos.Y;
				} else if (pos.Y > W / 2d - OrbR) {
					v.Y = -Math.Abs(v.Y);
					pos.Y = 2 * (W / 2d - OrbR) - pos.Y;

				}

				if (pos.X + pos.Y < C + OrbR * sqrt2 && v * new IVector(-1, -1) >= 0) {
					d = Math.Sqrt(Math.Abs(pos.X + pos.Y - C - OrbR * sqrt2) / sqrt2);
					n = ~new IVector(-1, -1);
					pos -= 2 * d * n;
					v -= 2 * (v * n) * n;
				} else if (W - pos.X + pos.Y < C + OrbR * sqrt2 && v * new IVector(1, -1) >= 0) {
					d = Math.Sqrt(Math.Abs(W - pos.X + pos.Y - C - OrbR * sqrt2) / sqrt2);
					n = ~new IVector(1, -1);
					pos -= 2 * d * n;
					v -= 2 * (v * n) * n;
				} else if (W / 2 + pos.X - pos.Y < C + OrbR * sqrt2 && v * new IVector(-1, 1) >= 0) {
					d = Math.Sqrt(Math.Abs(W / 2 + pos.X - pos.Y - C - OrbR * sqrt2) / sqrt2);
					n = ~new IVector(-1, 1);
					pos -= 2 * d * n;
					v -= 2 * (v * n) * n;
				} else if (W + W / 2f - pos.X - pos.Y < C + OrbR * sqrt2 && v * new IVector(1, 1) >= 0) {
					d = Math.Sqrt(Math.Abs(W + W / 2 - pos.X - pos.Y - C - OrbR * sqrt2) / sqrt2);
					n = ~new IVector(1, 1);
					pos -= 2 * d * n;
					v -= 2 * (v * n) * n;
				}

				//update
				pos += v;
			}
		}

		public void Move(IPoint log) {
			lock (update_lock) {
				if (isBullet) return;
				if (state == (byte) OrbStates.OWNER) {
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
			if (state == (byte) OrbStates.TRAVELLING) {
				clr = Brushes.White;
				if (Owner != Keys.None && (!HEADS.ContainsKey(Owner) || HEADS[Owner].Died)) NewOwner();
			} else if (state == (byte) OrbStates.BULLET) {
				double c = Math.Pow(Math.Sin(bulletTime / 8), 2) * 2 / 3;
				clr = new SolidBrush(Color.FromArgb((int)(color.R + (255 - color.R) * c), (int)(color.G + (255 - color.G) * c), (int)(color.B + (255 - color.B) * c)));
			} else
				clr = new SolidBrush(Color.FromArgb(!isWhite && KingOfTheHill && HEADS[Owner].INVINCIBLE? 128 : 255, color));

			g.FillEllipse(clr, (float) pos.X - this.r, (float) pos.Y - this.r, r * 2, r * 2);
			if (ContrastMode && state.GetHashCode() >= 3) {
				g.DrawEllipse(Pens.Black, (float)pos.X - r, (float)pos.Y - r, r * 2, r * 2);
				contrast_color.Set(BoolToInt(Map.InOrbit(pos)&&!isBullet));
				g.FillEllipse(new SolidBrush(AnimatableColor.Lurp(Color.Black, Color.White, (float) contrast_color)), (float) pos.X - 3, (float) pos.Y - 3, 6, 6);
			}

			if (!isWhite && HEADS[Owner].isBounty) g.DrawEllipse(new Pen(Color.Red, 2f), (float) pos.X - r, (float) pos.Y - r, r * 2, r * 2);

			///Debug draw ID
			//g.DrawString(this.ID.ToString(), new Font(FONT, 13), Brushes.Black, (PointF) pos);
		}

		public void DrawKills(Graphics g) {
			GraphicsState gstate = g.Save();
			g.TranslateTransform((float) pos.X, (float) pos.Y);
			g.RotateTransform((float) (v.A / Math.PI * 180d + 90d));
			if (ContrastMode) g.FillEllipse(new SolidBrush(color), -4,-4,8,8);

			string str;
			if (this.Owner != Keys.None) str = HEADS[Owner].Kills.ToString();
			else {
				str = "!";
			}
			Font font = new Font(FONT, r);
			SizeF sz = g.MeasureString(str, font);
			g.DrawString(str, font, Brushes.White, -sz.Width / 2, -sz.Height / 2);

			g.Restore(gstate);
		}

		public void NewOwner() {
			lock (OrbLock) {
				this.Owner = Keys.None;
				this.color = Color.White;
				this.state = (byte) OrbStates.WHITE;
				this.r = OrbR;
				OnUpdate += Update;
				window.DrawWhite += Draw;
			}
		}

		public void NewOwner(Keys newowner) {
			lock (OrbLock) {
				OnUpdate -= Update;
				window.DrawWhite -= Draw;
				if (state == (byte) OrbStates.SPAWN) Map.newOrb();
				if (TutorialActive && newowner != Keys.F13 && newowner != Keys.F14 && state == (byte) OrbStates.SPAWN) new Animation(pos, 80, 0, W, HeadR, (float)PHI * HeadR, Color.FromArgb(150, 255, 255, 255), 0);
				this.Owner = newowner;
				this.state = (byte) OrbStates.TRAVELLING;
				this.color = HEADS[newowner].color;
			}
		}


		private void update_color() {
			if (this.Owner != Keys.None) this.color = HEADS[Owner].color;
		}
	}
}
