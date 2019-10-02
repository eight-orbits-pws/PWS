using System;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;
using Eight_Orbits.Properties;
using System.Collections.Generic;
using System.ComponentModel;
using Neural_Network;
using System.Threading;
using System.Threading.Tasks;

namespace Eight_Orbits.Entities {
	class Head : Circle, Visual {
		private static byte bots = 0;

		public Keys KeyCode = Keys.None;
		public string DisplayKey;
		public byte Points = 0;
		public byte Kills = 0;
		public int debug_kills = 0;
        public HashSet<Keys> killed = new HashSet<Keys>();
		public byte index = 0;

		public IPoint orbitCenter;
		public Tail tail = new Tail();

		private float z = 0;
		private sbyte dashFrame = -1;
		private bool DashHideText = false;

		private bool bot = false;

		public bool Died { get; private set; } = false;

		public bool INVINCIBLE = false;

		public Activities act = Activities.DEFAULT;
		public IKey key;

		private Head() {
			index = (byte) ActiveKeys.Count;
			r = HeadR * SZR;
			v = IVector.Up;
			pos = Map.generateSpawn(HeadR);

			Map.OnStartGame += Reset;
			Map.OnRevive += Revive;
			Map.OnClear += clear;
			OnUpdate += Update;
			if (AnimationsEnabled) window.DrawHead += Draw;
		}

		public Head(Neat nnw) : this() {
			if (bots < 10) KeyCode = (Keys) new KeysConverter().ConvertFromString("D" + bots);
			else KeyCode = (Keys) new KeysConverter().ConvertFromString("F" + (bots + 3));

			if (bots < 10) DisplayKey = "B" + bots;
			else DisplayKey = "B" + (char) (bots+55);

			color = Color.FromArgb(255, 0, 255, 255);
			window.writeln(DisplayKey);
            
			this.key = new IKey(KeyCode, DisplayKey, color);
			ActiveKeys.Add(KeyCode);

			nnw.Key = KeyCode;
			nnw.Fire += Action;
			nnw.KeyUp += key.Release;

			this.bot = true;
			bots++;
		}

		public Head(Keys key) : this() {
			this.KeyCode = key;

			DisplayKey = getKeyString(KeyCode);
			window.writeln(DisplayKey);
			
			color = generate_color();

			this.key = new IKey(KeyCode, DisplayKey, color);
		}

		public Head(Head head) {
			this.KeyCode = head.KeyCode;

			DisplayKey = head.DisplayKey;
			window.writeln(DisplayKey);
			
			color = generate_color();

			this.key = new IKey(KeyCode, DisplayKey, color);
			index = (byte) ActiveKeys.Count;
			r = HeadR * SZR;
			v = IVector.Up;
			pos = head.pos;

			this.key.points = this.Points = head.Points;
			this.act = head.act;

			Map.OnStartGame += Reset;
			Map.OnRevive += Revive;
			Map.OnClear += clear;
			OnUpdate += Update;
			if (AnimationsEnabled) window.DrawHead += Draw;
		}

		~Head() {
			Map.SetMaxPoints();
			try {
				IKey.WIDTH = W / ActiveKeys.Count;
			} catch (DivideByZeroException) {
				IKey.WIDTH = 0;
			}
		}

		public void Remove() {
			if (bot) bots--;
			ActiveKeys.Remove(KeyCode);
			InactiveKeys.Remove(KeyCode);
			HEADS.Remove(KeyCode);
			key.Remove();
			Map.OnStartGame -= Reset;
			Map.OnStartRound -= Revive;
			OnUpdate -= Update;
			if (AnimationsEnabled) window.DrawHead -= Draw;

			Map.SetMaxPoints();
			try {
				IKey.WIDTH = W / (float) ActiveKeys.Count;
			} catch (DivideByZeroException) {
				IKey.WIDTH = 0;
			}
		}

		public void clear() {
			NewColor();

			if (Died) {
				OnUpdate += Update;
				window.DrawHead += Draw;
				Died = false;
			} else {
				z = 0;
				dashFrame = -1;
				DashHideText = false;
			}
		}

		public void Action() {
			key.Press();

			switch (act) {
				case Activities.DEFAULT:
					if (dashFrame < 0) return;
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
			lock (Orb.OrbLock) {
				Orb orb = Orb.All[OrbId];

				if (!orb.NoOwner)
					HEADS[orb.owner].tail.Remove(OrbId);

				orb.NewOwner(this.KeyCode);
				tail.Add(OrbId);
			}
		}

		public void Revive() {
			this.act = Activities.STARTROUND;
			this.pos = IPoint.Center;
			this.v.A = 2 * Math.PI / ActiveKeys.Count * ActiveKeys.IndexOf(KeyCode) + StartRotation;
			Kills = 0;
			this.debug_kills = 0;
			this.killed.Clear();
			key.Revive();
		}

		public void Reset() {
			this.debug_kills = 0;
			this.Points = 0;
			this.Kills = 0;
			key.points = Points;
		}

		public event Action OnDie;
		public event Action OnReward;
		private volatile object lock_die = new { };

		public void Die() {
			lock (lock_die) {
				if (Died) return;
				Died = true;
			}
			OnUpdate -= Update;
			if (AnimationsEnabled)
				window.DrawHead -= Draw;

			if (Tick < 60 + Map.StartRoundTime)
				MVP.Add(MVPTypes.EARLY_KILL, DisplayKey);

			this.act = Activities.DEAD;

			ActiveKeys.Remove(this.KeyCode);
			InactiveKeys.Insert(0, this.KeyCode);
			key.Die();
			tail.Die();
			z = 0;
			DashHideText = false;

			if (AnimationsEnabled && ActiveKeys.Count <= 12 && !(Map is BotArena))
				new Animation(pos, 80, 0, W, HeadR, (float)PHI * HeadR, Color.FromArgb(150, this.color), 0);
			if (AnimationsEnabled)
				new Animation(pos, 12, 0, 0, HeadR, HeadR, this.color, 32, AnimationTypes.CUBED);
			else
				window.writeln(DisplayKey + " died: " + Points + " pts");

			Map.Sort();
			IKey.UpdateAll();
			if (ActiveKeys.Count == 1 && Map.phase != Phases.ENDROUND) new Thread(Map.EndRound).Start();

            if (OnDie != null) Parallel.Invoke(OnDie);
        }

		private readonly object lock_reward = new {};
		public byte Reward(byte OrbId, Keys victim) {
			byte temp;
			lock (MVP.RecordsLock) {
				lock (lock_reward) {
					if (killed.Contains(victim))
						return 0;
					else
						killed.Add(victim);
					if (ChaosMode && Leader == victim) Leader = this.KeyCode;
					OnReward?.Invoke();
					temp = this.Points;
					this.Points += ++Kills;
					this.Points += Orb.All[OrbId].KillStreak++;
					key.Add(this.Points - temp);
					key.points = Points;

					if (Orb.All[OrbId].KillStreak > 1)
						MVP.Add(MVPTypes.COLLATERAL, DisplayKey, Orb.All[OrbId].KillStreak.ToString());
					if (Died)
						MVP.Add(MVPTypes.GHOSTKILL, DisplayKey);
				}
			}
			return (byte) (Points - temp);
		}

		public void Update() {
			if (!Ingame) return;
			tail.logAdd(pos.Copy());
			tail.Update();
			v.L = speed;
			if (dashFrame < 0) dashFrame++;

			switch (act) {
				case Activities.DEFAULT:
					pos += v;
					break;

				case Activities.STARTROUND:
					pos += v;
					v.A += speed/(72 * SZR);
					break;

				case Activities.DASHING:
					int w = 43;
					v *= 2d / (Math.Pow(2d * dashFrame / w - 1d, 2d) + 1d);

					pos += v;
					
					z = HeadR - (float) Math.Cos(dashFrame / (double) w * Math.PI * 2) * HeadR;
					
					if (dashFrame++ == w) {
						act = Activities.DEFAULT;
						dashFrame = -1;
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

				default: return;//throw new NotImplementedException();
			}
			
			Activities temp = this.act;
			if (this.act == Activities.ORBITING)
				this.act = Activities.DEFAULT;

			//collisions
			if (pos.X < HeadR) {
				v.X = Math.Abs(v.X);
				NewColor();
			} else if (pos.X > W - HeadR) {
				v.X = -Math.Abs(v.X);
				NewColor();
			} else if (pos.Y < HeadR) {
				v.Y = Math.Abs(v.Y);
				NewColor();
			} else if (pos.Y > W / 2 - HeadR) {
				v.Y = -Math.Abs(v.Y);
				NewColor();
			}

			else if (pos.X + pos.Y < C + HeadR * sqrt2 && v * new IVector(-1, -1) >= 0) {
				v.A += Math.PI / 4d;
				v.Y = Math.Abs(v.Y);
				v.A -= Math.PI / 4d;
				NewColor();
			} else if (W - pos.X + pos.Y < C + HeadR * sqrt2 && v * new IVector(1, -1) >= 0) {
				v.A -= Math.PI / 4d;
				v.Y = Math.Abs(v.Y);
				v.A += Math.PI / 4d;
				NewColor();
			} else if (W / 2 + pos.X - pos.Y < C + HeadR * sqrt2 && v * new IVector(-1, 1) >= 0) {
				v.A += Math.PI - Math.PI / 4d;
				v.Y = Math.Abs(v.Y);
				v.A += Math.PI / 4d - Math.PI;
				NewColor();
			} else if (W + W / 2f - pos.X - pos.Y < C + HeadR * sqrt2 && v * new IVector(1, 1) >= 0) {
				v.A += Math.PI / 4d + Math.PI;
				v.Y = Math.Abs(v.Y);
				v.A += -Math.PI - Math.PI / 4d;
				NewColor();
			} else {
				this.act = temp;
			}
		}

		public void Draw(Graphics g) {
			r = HeadR;
			if (act == Activities.DEAD) return;
			Bitmap bmp = new Bitmap((int) Math.Ceiling(r * 2), (int) Math.Ceiling(r * 2));
			Graphics frame = Graphics.FromImage(bmp);
			SizeF sz = g.MeasureString(DisplayKey, new Font(Program.FONT, r-1));

			frame.TranslateTransform(r, r);
			frame.RotateTransform((float)(this.v.A / Math.PI * 180d + 90d));
			frame.ScaleTransform((r - z) / r, 1);
			frame.FillEllipse(new SolidBrush(color), -r, -r, r * 2, r * 2);
			if (ContrastMode) frame.DrawEllipse(Pens.Black, .5f - r, .5f - r, r * 2 - 1, r * 2 - 1);
			if (!DashHideText && !(ChaosMode && Map.phase == Phases.NONE && state == States.INGAME)) frame.DrawString(DisplayKey, new Font(Program.FONT, r-1),	Brushes.White, -sz.Width / 2, -sz.Height / 2);
			
			g.DrawImage(bmp, (float) pos.X - r, (float) pos.Y - r);
		}

		public static string getKeyString(Keys k) {
			string c = (new KeysConverter()).ConvertToString(k).ToUpper();
			c = c.Replace("OEMTILDE", "~").Replace("TAB", "TB").Replace("CAPITAL", "CL").Replace("SHIFTKEY", "SH").Replace("CONTROLKEY", "CT").Replace("SPACE", "SP").Replace("BACK", "BS")
				.Replace("DEL", "DL").Replace("HOME", "HM").Replace("PGUP", "PU").Replace("PGDN", "PD").Replace("END", "ND")
				.Replace("DIVIDE", "/").Replace("MULTIPLY", "*").Replace("SUBTRACT", "-").Replace("ADD", "+").Replace("NUMPAD", "").Replace("DECIMAL", ".")
				.Replace("OEMMINUS", "-").Replace("OEMPLUS", "=").Replace("OEMOPENBRACKETS", "[").Replace("OEM6", "]").Replace("OEM5", @"\")
				.Replace("OEM1", ":").Replace("OEM7", "'").Replace("OEMCOMMA", "<").Replace("OEMPERIOD", ">").Replace("OEMQUESTION", "?")
				.Replace("LEFT", ""+(char)8592).Replace("UP", ""+(char)8593).Replace("RIGHT", ""+(char)8594).Replace("DOWN", ""+(char)8595);
			return c;
		}

		public static Color generate_color() {
			int a, r, g, b;
			double x = R.NextDouble();

			a = 255;
			r = (int) Math.Round(Math.Pow(Math.Cos(Math.PI * (x + 0/3d)), 2) * 255);
			g = (int) Math.Round(Math.Pow(Math.Cos(Math.PI * (x + 1/3d)), 2) * 255);
			b = (int) Math.Round(Math.Pow(Math.Cos(Math.PI * (x + 2/3d)), 2) * 255);

			return Color.FromArgb(a, r, g, b);
		}

		public void NewColor(bool red) => this.color = red? Color.Red : generate_color();

		public void NewColor() {
			if (Gamemode == Gamemodes.CHAOS_RAINBOW)
				this.color = generate_color();
		}

		public override bool Equals(object obj) => this.GetHashCode() == obj.GetHashCode();

		public override int GetHashCode() => this.KeyCode.GetHashCode();

		public static bool operator ==(Head h, Head H) => h.KeyCode.GetHashCode() == H.KeyCode.GetHashCode();

		public static bool operator !=(Head h, Head H) => h.KeyCode.GetHashCode() != H.KeyCode.GetHashCode();
	}
}
