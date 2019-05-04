using System;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;
using Eight_Orbits.Properties;
using System.Collections.Generic;
using System.ComponentModel;

namespace Eight_Orbits.Entities {
    class Head : Circle, Visual {

		public Keys keyCode = Keys.None;
		public string DisplayKey;
		public byte Points = 0;
		public byte Kills = 0;

		private IPoint orbitCenter;
		public Tail tail = new Tail();

		private float z = 0;
		private byte dashFrame = 0;
		private bool DashHideText = false;

		private bool dead = false;
		public bool Died {  get { return dead; } }
		//private bool dashing = false;
		//private bool orbiting = false;

		public Activities act = Activities.DEFAULT;
		public IKey key;

		public Head(Keys key) {
			r = HeadR * SZR;
			v = new IVector(R.NextDouble() * 2 - 1d, R.NextDouble() * 2 - 1d);
			keyCode = key;
			DisplayKey = getKeyString(keyCode);
			Console.WriteLine(DisplayKey);

			pos = Map.generateSpawn(HeadR);
			color = Color.FromArgb(255, R.Next(256), R.Next(256), R.Next(256));

			this.key = new IKey(keyCode, DisplayKey, color);

			Map.OnStartGame += Reset;
			Map.Revive += Revive;
			OnUpdate += Update;
			if (AnimationsEnabled) window.DrawHead += Draw;
		}

		~Head() {
			Map.SetMaxPoints();
			try {
				IKey.Width = W / Active.Count;
			} catch (DivideByZeroException) {
				IKey.Width = 0;
			}
		}

		public void Remove() {
			key.Remove();
			Map.OnStartGame -= Reset;
			Map.OnStartRound -= Revive;
			OnUpdate -= Update;
			if (AnimationsEnabled) window.DrawHead -= Draw;

			Map.SetMaxPoints();
			try {
				IKey.Width = W / Active.Count;
			} catch (DivideByZeroException) {
				IKey.Width = 0;
			}
		}

		public void Action() {
			key.Press();

			switch (act) {
				case Activities.DEFAULT:
					//dash or orbit
					if (Map.InOrbit(pos)) {
						act = Activities.ORBITING;
						orbitCenter = Map.getOrbitCenter();
					} else {
						act = Activities.DASHING;
						tail.Shoot();
					} break;

				case Activities.ORBITING:
					act = Activities.DEFAULT;
					break;

				case Activities.DASHING:
					break;

				case Activities.DEAD:
					break;
			}
		}

		public void Eat(byte OrbId) {
			Orb orb = Orb.All[OrbId];

			if (!orb.noOwner()) HEAD[orb.owner].tail.Remove(OrbId);

			orb.newOwner(this.keyCode);
			tail.Add(OrbId);
		}

		public void Revive() {
			if (dead) {
				OnUpdate += Update;
				window.DrawHead += Draw;
				dead = false;
			} else {
				z = 0;
				DashHideText = false;
			}

			this.act = Activities.STARTROUND;
			this.pos = IPoint.Center;
			this.v.A = 2 * Math.PI / Active.Count * Active.IndexOf(keyCode);
			Kills = 0;
			key.Revive();
		}

		public void Reset() {
			this.Points = 0;
			this.Kills = 0;
			key.points = Points;
		}

		public event GameEvent OnDie;

		public void Die() {
			OnDie?.Invoke();
			OnUpdate -= Update;
			if (AnimationsEnabled) window.DrawHead -= Draw;

			dead = true;
			this.act = Activities.DEAD;

			Active.Remove(this.keyCode);
			Dead.Insert(0, this.keyCode);
			key.Die();
			tail.Die();
			z = 0;
			DashHideText = false;

			if (AnimationsEnabled) AnimationControl.Add(new Animation(pos, 64, 0, W, HeadR, (float) PHI * HeadR, Color.FromArgb(200, this.color), 0));
			else Console.WriteLine(DisplayKey + " died: " + Points + " pts");

			Map.Sort();

			if (Active.Count == 1) Map.EndRound();
		}

		public void Reward(byte OrbId) {
			this.Points += ++Kills;
			this.Points += Orb.All[OrbId].KillStreak++;
			key.points = Points;

			if (Orb.All[OrbId].KillStreak > 1) MVP.Add(MVPTypes.COLLATERAL, DisplayKey, Orb.All[OrbId].KillStreak.ToString());
		}

		public void Update() {
			tail.logAdd(pos.Copy());

			v.L = speed;

			switch (act) {
				case Activities.DEFAULT:
					pos += v;
					break;

				case Activities.STARTROUND:
					pos += v;
					v.A += speed/(72 * SZR);
					break;

				case Activities.DASHING:
					int w = 61;
					v *= 2d / (Math.Pow(2d * dashFrame / w - 1d, 2d) + 1d);

					pos += v;
					
					z = HeadR - (float) Math.Cos(dashFrame / (double) w * Math.PI * 2) * HeadR;
					
					if (dashFrame++ == w) {
						act = Activities.DEFAULT;
						dashFrame = 0;
					} else if (dashFrame == w/4) {
						DashHideText = true;
					} else if (dashFrame == w*3/4) {
						DashHideText = false;
					}
					break;

				case Activities.ORBITING:
					IVector n = pos - orbitCenter;
					double angle = speed / n.L;
					n.A += Math.PI / 2;

					double direction = Math.Sign(v * n);
					
					n.A += direction * angle - Math.PI / 2;
					v.A = n.A + direction * Math.PI / 2;

					pos = orbitCenter + n;
					break;

				default: throw new NotImplementedException();
			}

			//collisions
			if (pos.X < HeadR) v.X = Math.Abs(v.X);
			else if (pos.X > W - HeadR) v.X = -Math.Abs(v.X);

			else if (pos.Y < HeadR) v.Y = Math.Abs(v.Y);
			else if (pos.Y > W / 2 - HeadR) v.Y = -Math.Abs(v.Y);

			else if (pos.X + pos.Y < C + HeadR * sqrt2 && v * new IVector(-1, -1) > 0) {
				v.A += Math.PI / 4d;
				v.Y = Math.Abs(v.Y);
				v.A -= Math.PI / 4d;
			} else if (W - pos.X + pos.Y < C + HeadR * sqrt2 && v * new IVector(1, -1) > 0) {
				v.A -= Math.PI / 4d;
				v.Y = Math.Abs(v.Y);
				v.A += Math.PI / 4d;
			} else if (W / 2 + pos.X - pos.Y < C + HeadR * sqrt2 && v * new IVector(-1, 1) > 0) {
				v.A += Math.PI - Math.PI / 4d;
				v.Y = Math.Abs(v.Y);
				v.A += Math.PI / 4d - Math.PI;
			} else if (W + W / 2f - pos.X - pos.Y < C + HeadR * sqrt2 && v * new IVector(1, 1) > 0) {
				v.A += Math.PI / 4d + Math.PI;
				v.Y = Math.Abs(v.Y);
				v.A += -Math.PI - Math.PI / 4d;
			}
		}

		public void Draw(ref PaintEventArgs e) {
			tail.Draw(ref e);
			r = HeadR;
			Bitmap bmp = new Bitmap((int) Math.Ceiling(r * 2), (int) Math.Ceiling(r * 2));
			Graphics frame = Graphics.FromImage(bmp);
			SizeF sz = e.Graphics.MeasureString(DisplayKey, new Font(Program.FONT, r-1));

			frame.TranslateTransform(r, r);
			frame.RotateTransform((float)(this.v.A / Math.PI * 180d + 90d));
			frame.ScaleTransform((r - z) / r, 1);
			frame.FillEllipse(new SolidBrush(color), -r, -r, r * 2, r * 2);
			if (ContrastMode) frame.DrawEllipse(Pens.Black, .5f - r, .5f - r, r * 2 - 1, r * 2 - 1);
			if (!DashHideText) frame.DrawString(DisplayKey, new Font(Program.FONT, r-1),	Brushes.Black, -sz.Width / 2, -sz.Height / 2);
			
			e.Graphics.DrawImage(bmp, (float) pos.X - r, (float) pos.Y - r);
			
			//key.Draw(ref e);
			//return e;
		}

		public static string getKeyString(Keys k) {
			string c = (new KeysConverter()).ConvertToString(k).ToUpper();
			c = c.Replace("OEMTILDE", "~").Replace("TAB", "TB").Replace("CAPITAL", "CL").Replace("SHIFTKEY", "SH").Replace("CONTROLKEY", "CT").Replace("SPACE", "SP").Replace("BACK", "BS")
				.Replace("DEL", "DL").Replace("HOME", "HM").Replace("PGUP", "PU").Replace("PGDN", "PD").Replace("END", "ND")
				.Replace("DIVIDE", "/").Replace("MULTIPLY", "*").Replace("SUBTRACT", "-").Replace("ADD", "+").Replace("NUMPAD", "").Replace("DECIMAL", ".")
				.Replace("OEMMINUS", "-").Replace("OEMPLUS", "=").Replace("OEMOPENBRACKETS", "[").Replace("OEM6", "]").Replace("OEM5", @"\")
				.Replace("OEM1", ":").Replace("OEM7", "'").Replace("OEMCOMMA", "<").Replace("OEMPERIOD", ">").Replace("OEMQUESTION", "?")
				.Replace("LEFT", ""+(char)8592).Replace("UP", ""+(char)8592).Replace("RIGHT", ""+(char)8594).Replace("DOWN", ""+(char)8595);
			return c;
		}
    }
}
