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

		public readonly Keys KeyCode;
		public string DisplayKey;
		public byte Points = 0;
		public byte Kills = 0;
        public readonly HashSet<Keys> killed = new HashSet<Keys>();
		public byte index = 0;

		public IPoint orbitCenter;
		public readonly Tail tail;
		private readonly Color color_creation;

		private float z = 0;
		private volatile sbyte dash_frame = -1;
		public sbyte DashFrame => dash_frame;
		private volatile bool DashHideText = false;
		private static readonly int dash_length = 43;

		private readonly bool bot = false;
		
		public bool Dashing => this.act == Activities.DASHING;
		public bool Died => this.act == Activities.DEAD;
		public bool Orbiting => this.act == Activities.ORBITING;

		public bool INVINCIBLE = false;
		public bool INACTIVE = false;

		public bool isBounty = false;
		public bool isDoubleBounty = false;

		public Activities act = Activities.DEFAULT;
		public readonly IKey key;

		private readonly PaintEvent draw_event;
		private readonly Action e_update;
		public event Action OnRemove;

		private Head() {
			index = (byte) ActiveKeys.Count;
			r = HeadR * SZR;
			v = IVector.Up;
			pos = Map.generateSpawn(HeadR);

			draw_event = new PaintEvent(Draw);

			Map.OnStartGame += Reset;
			Map.OnRevive += Revive;
			Map.OnClear += clear;
			OnUpdate += e_update = new Action(Update);
			if (AnimationsEnabled) window.DrawHead += draw_event;
		}

		public Head(Neat nnw) : this() {
			//if (bots < 10) KeyCode = (Keys) new KeysConverter().ConvertFromString("D" + bots);
			KeyCode = (Keys) new KeysConverter().ConvertFromString("F" + (bots+1));
			tail  = new Tail(KeyCode);

			if (bots < 10) DisplayKey = "B" + bots;
			else DisplayKey = "B" + (char) (bots+55);

			color_creation = color = Color.FromArgb(255, 0, 255, 255);
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
			tail  = new Tail(KeyCode);

			DisplayKey = getKeyString(KeyCode);
			window.writeln(DisplayKey);
			
			color_creation = color = GenerateColor();

			this.key = new IKey(KeyCode, DisplayKey, color);
		}

		public Head(Head head) {
			this.KeyCode = head.KeyCode;
			tail  = new Tail(KeyCode);

			DisplayKey = head.DisplayKey;
			window.writeln(DisplayKey);
			
			color_creation = color = GenerateColor();

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
			OnUpdate += e_update = new Action(Update);
			if (AnimationsEnabled) window.DrawHead += draw_event = new PaintEvent(Draw);
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
			Map.OnClear -= clear;
			Map.OnStartGame -= Reset;
			Map.OnStartRound -= Revive;
			OnUpdate -= e_update;
			if (AnimationsEnabled) window.DrawHead -= draw_event;

			OnRemove?.Invoke();
			OnRemove = null;

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
				OnUpdate += e_update;
				window.DrawHead += draw_event;
			} else {
				z = 0;
				dash_frame = -1;
				DashHideText = false;
			}

			this.act = Activities.DEFAULT;
		}

		public void Action() {
			key.Press();

			lock (updatinglocker) {
				switch (act) {
					case Activities.DEFAULT:
						//dash or orbit
						if (Map.InOrbit(pos)) { // dash cooldown deas not apply
							act = Activities.ORBITING;
							orbitCenter = Map.getOrbitCenter();
						} else if (dash_frame == 0) { // dash cooldown
							act = Activities.DASHING;
							tail.Shoot();
						}
						break;

					case Activities.ORBITING:
						act = Activities.DEFAULT;
						break;

					case Activities.DASHING:
						break;

					case Activities.DEAD:
						break;
				}
			}
		}

		public void Eat(byte OrbId) {
			if (Died) return;

			Orb orb = Orb.All[OrbId];

			if (!orb.isWhite) HEADS[orb.Owner].tail.Remove(OrbId);

			orb.NewOwner(this.KeyCode);
			tail.Add(OrbId);
		}

		public void Revive() {
			this.act = Activities.STARTROUND;
			this.pos = IPoint.Center;
			if (!(KingOfTheHill && !(state == States.NEWGAME || Map.phase == Phases.ENDGAME)))
				this.v.A = 2 * Math.PI / ActiveKeys.Count * ActiveKeys.IndexOf(KeyCode) + StartRotation;
			this.Kills = 0;
			this.killed.Clear();
			this.key.Revive();

			if (KingOfTheHill && this.Points >= Map.MaxPoints - 1)
				this.isBounty = true;
		}

		public void Reset() {
			this.Points = 0;
			this.Kills = 0;
			this.key.points = Points;
			this.isBounty = false;
			this.isDoubleBounty = false;
		}

		public event Action OnDie;
		public event Action OnReward;
		private static readonly object lock_die = new { };

		public void Die() {
			lock (lock_die) {
				if (Died) return;
				this.act = Activities.DEAD;
			}

			OnUpdate -= e_update;
			if (AnimationsEnabled)
				window.DrawHead -= draw_event;

			if (Tick < 60 + Map.StartRoundTime)
				MVP.Add(MVPTypes.EARLY_KILL, DisplayKey);

			lock (ActiveLock) {
				ActiveKeys.Remove(this.KeyCode);
				if (KingOfTheHill) InactiveKeys.Add(this.KeyCode);
				else InactiveKeys.Insert(0, this.KeyCode);
			}
			key.Die();
			tail.Die();
			z = 0;
			DashHideText = false;

			if (isBounty) new Animation(pos, 30, HeadR - 1, HeadR - 5, 5, 0, Color.Red, Color.Red, AnimationTypes.CUBED);

			isBounty = false;
			isDoubleBounty = false;

			if (AnimationsEnabled && ActiveKeys.Count <= 12 && !(Map is BotArena))
				_ = new Animation(pos, 80, 0, W, HeadR, (float)PHI * HeadR, Color.FromArgb(150, this.color), 0);
			if (AnimationsEnabled)
				_ = new Animation(pos, 12, 0, 0, HeadR, HeadR, this.color, 32, AnimationTypes.CUBED);
			else
				window.writeln(DisplayKey + " died: " + Points + " pts");

			

			if (KingOfTheHill) {
				IKey.UpdateAll();
				new Thread(() => {
					int tick = Tick;
					SpinWait.SpinUntil(() => Tick >= tick + 120 || Map.phase != Phases.NONE || state == States.NEWGAME || !ApplicationRunning);
					if (Map.phase != Phases.NONE || state == States.NEWGAME || !ApplicationRunning) return;
						new Coin(IPoint.Center + 3 * IVector.Random(), "3", this.color);

					SpinWait.SpinUntil(() => Tick >= tick + 180 || Map.phase != Phases.NONE || state == States.NEWGAME || !ApplicationRunning);
					if (Map.phase != Phases.NONE || state == States.NEWGAME || !ApplicationRunning) return;
						new Coin(IPoint.Center + 3 * IVector.Random(), "2", this.color);

					SpinWait.SpinUntil(() => Tick >= tick + 240 || Map.phase != Phases.NONE || state == States.NEWGAME || !ApplicationRunning);
					if (Map.phase != Phases.NONE || state == States.NEWGAME || !ApplicationRunning) return;
						new Coin(IPoint.Center + 3 * IVector.Random(), "1", this.color);

					SpinWait.SpinUntil(() => Tick >= tick + 300 || Map.phase != Phases.NONE || state == States.NEWGAME || !ApplicationRunning);
					if (Map.phase != Phases.NONE || state == States.NEWGAME || !ApplicationRunning) return;
					lock (updatinglocker) {
						this.clear();
						this.Revive();
						this.act = Activities.DEFAULT;
						lock (ActiveLock) {
							ActiveKeys.Add(KeyCode);
							InactiveKeys.Remove(KeyCode);
						}
						this.INVINCIBLE = true;
					}
					SpinWait.SpinUntil(() => (Tick >= tick + 360 && !is_about_to_die()) || !ApplicationRunning);
						this.INVINCIBLE = false;

					SpinWait.SpinUntil(() => (Tick >= tick + 372 || Died) || !ApplicationRunning);
						if (Tick <= tick + 372 && Died) MVP.Flash("Spawnkill!");
				}).Start();
			}
			else {
				Map.Sort();
				IKey.UpdateAll();
				if (ActiveKeys.Count == 1 && Map.phase != Phases.ENDROUND) new Thread(Map.EndRound).Start();
			}

            if (OnDie != null) Parallel.Invoke(OnDie);
        }

		private static readonly object lock_reward = new {};
		public byte Reward(byte OrbId, Keys victim) {
			byte temp;
			lock (MVP.RecordsLock) {
				lock (lock_reward) {
					if (killed.Contains(victim) || HEADS[victim].Died)
						return 0;
					else if (!KingOfTheHill)
						killed.Add(victim);

					if (ChaosMode && Leader == victim) Leader = this.KeyCode;

					OnReward?.Invoke();
					temp = this.Points;
					Kills++;
					this.Points += KingOfTheHill? (byte) 1 : Kills;
					if (HEADS[victim].isBounty) this.Points++;
					if (HEADS[victim].isDoubleBounty) this.Points++;
					this.Points += Orb.All[OrbId].KillStreak++;
					key.Add(this.Points - temp);
					key.points = Points;

					if (Orb.All[OrbId].KillStreak > 1) {
						if (KingOfTheHill) MVP.Flash("Collateral!");
						else MVP.Add(MVPTypes.COLLATERAL, DisplayKey, Orb.All[OrbId].KillStreak.ToString());
					}

					if (Died) {
						if (KingOfTheHill) MVP.Flash("GhostKill!");
						else MVP.Add(MVPTypes.GHOSTKILL, DisplayKey);
					}
				}
			}

			if (KingOfTheHill) {
				Map.Sort();
				IKey.UpdateAll();
				bool leader = true;
				int second = 0;

				lock (ActiveLock)
					foreach (Head head in HEADS.Values) {
						if (head.KeyCode == KeyCode) continue;
						if (this.Points < head.Points)
							leader = false;
						if (second < head.Points) second = head.Points;
					}

				if (leader)
					Leader = this.KeyCode;

				if (this.Points >= Map.MaxPoints && Points - second > 1 && Map.phase != Phases.ENDGAME) {
					MVP.Show(MVP.WinMessage(this.DisplayKey));
					new Thread(Map.EndGame).Start();
				}
				else if (Kills >= 5 && !isBounty) {
					MVP.Flash($"{DisplayKey} killstreak");
					this.isBounty = true;
				} else if (this.Points >= Map.MaxPoints - 1 && !isBounty) {
					this.isBounty = true;
					MVP.Flash("Matchpoint!");
				}

				else if (isBounty && Kills >= 5 && this.Points >= Map.MaxPoints - 1 && !isDoubleBounty) {
					MVP.Flash("Double Bounty!");
					this.isDoubleBounty = true;
				}

				
			}
			return (byte) (Points - temp);
		}

		double d;
		IVector n;
		public void Update() {
			if (!Ingame || INACTIVE) return;
			tail.logAdd(pos.Copy());
			tail.Update();
			v.L = speed;
			if (dash_frame < 0) dash_frame++;

			switch (act) {
				case Activities.DEFAULT:
					pos += v;
					break;

				case Activities.STARTROUND:
					pos += v;
					v.A += speed/(72 * SZR);
					break;

				case Activities.DASHING:
					v *= 2d / (Math.Pow(2d * dash_frame / dash_length - 1d, 2d) + 1d); // some complicated resistance formula

					pos += v;
					
					z = HeadR - (float) Math.Cos(dash_frame / (double) dash_length * Math.PI * 2) * HeadR;
					
					if (dash_frame++ == dash_length) {
						act = Activities.DEFAULT;
						dash_frame = -1;
					} else if (dash_frame == dash_length/4) {
						DashHideText = true;
					} else if (dash_frame == dash_length*3/4) {
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

				case Activities.DEAD: return;
			}

			//collisions
			if (pos.X < HeadR) {
				v.X = Math.Abs(v.X);
				pos.X = 2 * HeadR - pos.X;
				NewColor();
			} else if (pos.X > W - HeadR) {
				v.X = -Math.Abs(v.X);
				pos.X = 2 * (W - HeadR) - pos.X;
				NewColor();
			} else if (pos.Y < HeadR) {
				v.Y = Math.Abs(v.Y);
				pos.Y = 2 * HeadR - pos.Y;
				NewColor();
			} else if (pos.Y > W / 2 - HeadR) {
				v.Y = -Math.Abs(v.Y);
				pos.Y = 2 * (W/2 - HeadR) - pos.Y;
				NewColor();
			}

			if (pos.X + pos.Y < C + HeadR * sqrt2 && v * new IVector(-1, -1) >= 0) {
				d = Math.Sqrt(Math.Abs(pos.X + pos.Y - C - HeadR * sqrt2) / sqrt2);
				n = ~new IVector(-1, -1);
				pos -= 2 * d * n;
				v -= 2 * (v * n) * n;
				NewColor();
			} else if (W - pos.X + pos.Y < C + HeadR * sqrt2 && v * new IVector(1, -1) >= 0) {
				d = Math.Sqrt(Math.Abs(W - pos.X + pos.Y - C - HeadR * sqrt2) / sqrt2);
				n = ~new IVector(1, -1);
				pos -= 2 * d * n;
				v -= 2 * (v * n) * n;
				NewColor();
			} else if (W / 2 + pos.X - pos.Y < C + HeadR * sqrt2 && v * new IVector(-1, 1) >= 0) {
				d = Math.Sqrt(Math.Abs(W / 2 + pos.X - pos.Y - C - HeadR * sqrt2) / sqrt2);
				n = ~new IVector(-1, 1);
				pos -= 2 * d * n;
				v -= 2 * (v * n) * n;
				NewColor();
			} else if (W + W / 2f - pos.X - pos.Y < C + HeadR * sqrt2 && v * new IVector(1, 1) >= 0) {
				d = Math.Sqrt(Math.Abs(W + W / 2f - pos.X - pos.Y - C - HeadR * sqrt2) / sqrt2);
				n = ~new IVector(1, 1);
				pos -= 2 * d * n;
				v -= 2 * (v * n) * n;
				NewColor();
			}
		}

		public void Draw(Graphics g) {
			r = HeadR;
			if (Died) return;
			SolidBrush b;
			if (KingOfTheHill && INVINCIBLE) b = new SolidBrush(Color.FromArgb(128, color));
			else b = new SolidBrush(color);

			Bitmap bmp = new Bitmap((int) Math.Ceiling(r * 2+4), (int) Math.Ceiling(r * 2+4));
			Graphics frame = Graphics.FromImage(bmp);
			SizeF sz = g.MeasureString(DisplayKey, new Font(Program.FONT, r-1));

			frame.TranslateTransform(r+1, r+1);
			frame.RotateTransform((float)(this.v.A / Math.PI * 180d + 90d));
			frame.ScaleTransform((r - z) / r, 1);
			frame.FillEllipse(b, -r, -r, r * 2, r * 2);
			if (isBounty) frame.DrawEllipse(new Pen(Color.Red, isDoubleBounty? 4f : 2f), -r, -r, r * 2, r * 2);
			else if (ContrastMode) frame.DrawEllipse(Pens.Black, .5f - r, .5f - r, r * 2 - 1, r * 2 - 1);
			
			if (!DashHideText && !(ChaosMode && Map.phase == Phases.NONE && state == States.INGAME))
				frame.DrawString(DisplayKey, new Font(Program.FONT, r - 1), Brushes.White, -sz.Width / 2, -sz.Height / 2);
			g.DrawImage(bmp, (float) pos.X - r - 1, (float) pos.Y - r - 1);
			
		}

		private bool is_about_to_die() {
			lock (Orb.OrbLock) {
				this.r *= 2f;
				foreach (Orb orb in Orb.All) {
					if (this.Collide(orb) && orb.isDangerTo(KeyCode)) return true;
				}
			}
			this.r /= 2f;
			return false;
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

		public static Color GenerateColor() {
			int a, r, g, b;
			double x = R.NextDouble();

			a = 255;
			r = (int) Math.Round(pow(Math.Cos(Math.PI * (x + 0/3d))) * 255);
			g = (int) Math.Round(pow(Math.Cos(Math.PI * (x + 1/3d))) * 255);
			b = (int) Math.Round(pow(Math.Cos(Math.PI * (x + 2/3d))) * 255);

			return Color.FromArgb(a, r, g, b);
		}

        private static double pow(double x) => x * x;

        public static double FromColor(Color color) {
            return Math.Acos(Math.Sqrt(color.R / 255d)) / Math.PI;
        }

        public static Color FromDouble(double x) {
            int a = 255;
            int r = (int) Math.Round(pow(Math.Cos(Math.PI * (x + 0/3d))) * 255);
            int g = (int) Math.Round(pow(Math.Cos(Math.PI * (x + 1/3d))) * 255);
            int b = (int) Math.Round(pow(Math.Cos(Math.PI * (x + 2/3d))) * 255);

			return Color.FromArgb(a, r, g, b);
        }

		public void NewColor(bool red) => this.color = red? Color.Red : color_creation;

		public void NewColor() {
			if (Gamemode == Gamemodes.CHAOS_RAINBOW)
				this.color = GenerateColor();
		}

		public override bool Equals(object obj) => this.GetHashCode() == obj.GetHashCode();

		public override int GetHashCode() => this.KeyCode.GetHashCode();

		public static bool operator ==(Head h, Head H) => h.KeyCode.GetHashCode() == H.KeyCode.GetHashCode();

		public static bool operator !=(Head h, Head H) => h.KeyCode.GetHashCode() != H.KeyCode.GetHashCode();
	}
}
