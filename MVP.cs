using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using static Eight_Orbits.Program;

namespace Eight_Orbits {
	class MVP {
		private static readonly List<Stat> records = new List<Stat>();
		public static readonly object RecordsLock = new { };
		public static readonly object FlashLock = new { };
		private static Stat mvp = new Stat();

		private static readonly Animatable Appear = new Animatable(0, 1, 36, AnimationTypes.SIN, true);
		private static readonly Animatable Disappear = new Animatable(1, 0, 36, AnimationTypes.COS, true);
		private static volatile bool Displaying = true;
		private static string DisplayText = "Prepare!";
		private static readonly Color color = Color.FromArgb(96, 32, 32, 32);

		private static bool winner = false;

		public static void SetText(string text) => DisplayText = text;

		public static void Show(string text) {
			SetText(text);
			Show();
		}

		public static void Flash(string text) =>
			new Thread(() => {
				lock (FlashLock) {
					SetText(text);
					flash();
				}
			}).Start();

		public static void Flash() => new Thread(new ThreadStart(flash)).Start();

		private static Action flash = () => {
			lock (FlashLock) {
				int starttick = Tick;
				Thread.CurrentThread.Name = "Flash_MVP";
				Show();
				SpinWait.SpinUntil(() => Tick >= starttick + 60 || !ApplicationRunning);
				Hide();
				SpinWait.SpinUntil(() => Tick >= starttick + 96 || !ApplicationRunning);
			}
		};

		public static void Add(MVPTypes type) => records.Add(new Stat(type));

		public static void Add(MVPTypes type, string hero) {
			if (type == MVPTypes.GHOSTKILL && Displaying == true) {
				new Thread(() => {
					Thread.CurrentThread.Name = "Ghostkill!";
					DisplayText = "";
					int tick = Tick + 1;
					SpinWait.SpinUntil(() => Tick > tick || !ApplicationRunning);

					// okay, so one can win with a ghostkill. help
					// copy paste of Analyze
					Keys leader = Keys.None;
					Keys second = Keys.None;

					foreach (Keys p in InactiveKeys) {
						if (leader == Keys.None || HEADS[leader].Points < HEADS[p].Points) {
							second = leader;
							leader = p;
						} else if (second == Keys.None || HEADS[second].Points < HEADS[p].Points) {
							second = p;
						}
					}

					if (Head.getKeyString(leader) == hero && HEADS[leader].Points > HEADS[second].Points + 1 && HEADS[leader].Points >= Map.MaxPoints) {
						// won with a ghostkill O_o
						DisplayText = $"Ghostkill win by {hero}";
						Map.EndGame();
						Leader = leader;
						Clear();
					} else if (Map.phase == Phases.ENDGAME && HEADS[leader].Points - HEADS[second].Points < 2) {
						// so leader won, but now seconds is in reached because of the ghostkill O_o
						Map.phase = Phases.ENDROUND;
						Map.RoundsPassed--;
						DisplayText = $"{hero} denied the win";
					} else {
						DisplayText = "Ghostkill!";
					}

				}).Start();
			} 
			else records.Add(new Stat(hero, type));
		}

		public static void Add(MVPTypes type, string hero, string special) => records.Add(new Stat(hero, type, special));

		public static void Analyze2() {
			bool ace, win, flawless, ghostkill, collateral, two_points;
			ace = win = flawless =  ghostkill = collateral = two_points = false;
			int points_left = 0;

			Keys leader;
			Keys second = Keys.None;
			Keys mvp;

			if (ActiveKeys.Count == 1) leader = ActiveKeys[0];
			else leader = Keys.None;

			foreach (Keys p in InactiveKeys) {
				if (leader == Keys.None || HEADS[leader].Points < HEADS[p].Points) {
					second = leader;
					leader = p;
				} else if (second == Keys.None || HEADS[second].Points < HEADS[p].Points) {
					second = p;
				}
			}

			ace = Ace();
			if (HEADS[leader].Points < Map.MaxPoints) points_left = Map.MaxPoints - HEADS[leader].Points;
			else if (Map.MaxPoints != 0 && HEADS[leader].Points - HEADS[second].Points < 2) two_points = true;
			else if (Flawless()) win = ace = flawless = true;
			else win = true;

			if (win || ace) mvp = leader;

			List<Stat> ghostkills;
			if (records.Exists((Stat stat) => stat.Type == MVPTypes.GHOSTKILL)) {
				ghostkill = true;
				ghostkills = records.FindAll((Stat stat) => stat.Type == MVPTypes.GHOSTKILL);
			} else ghostkills = new List<Stat>(0);

			List<Stat> collats;
			if (records.Exists((Stat stat) => stat.Type == MVPTypes.COLLATERAL)) {
				collateral = true;
				collats = records.FindAll((Stat stat) => stat.Type == MVPTypes.COLLATERAL);
			} else collats = new List<Stat>(0);

			///mvp = ace || win? leader : ghostkill? ghostkills.Count == 1? ghostkills[0].Hero : Keys.None :
		}
		public static void Analyze() {
			//get points
			Keys leader;
			Keys second = Keys.None;

			if (ActiveKeys.Count == 1)
				leader = ActiveKeys[0];
			else
				leader = Keys.None;

			foreach (Keys p in InactiveKeys) {
				if (leader == Keys.None || HEADS[leader].Points < HEADS[p].Points) {
					second = leader;
					leader = p;
				} else if (second == Keys.None || HEADS[second].Points < HEADS[p].Points) {
					second = p;
				}
			}

			if (Ace()) Add(MVPTypes.ACE);
			if (HEADS[leader].Points < Map.MaxPoints)
				Add(MVPTypes.POINTS, HEADS[leader].DisplayKey, HEADS[leader].Points.ToString());
			else if (Map.MaxPoints != 0 && HEADS[leader].Points - HEADS[second].Points < 2)
				Add(MVPTypes.TWO_PTS);
			else if (Flawless()) {
				winner = true;
				Add(MVPTypes.FLAWLESS);
			} else {
				winner = true;
				Add(MVPTypes.WINNER, HEADS[leader].DisplayKey);
			}

			Stat record;
			for (int i = 0; i < records.Count; i++) {
				lock (RecordsLock) {
					record = records[i];
					//COLLAT -> ACE -> WIN
					if (record.Type == MVPTypes.ACE && mvp.Type == MVPTypes.COLLATERAL && mvp.Special.Equals((InactiveKeys.Count - 1))) {
						mvp = new Stat(MVPTypes.COLLATERAL_ACE);
						TriggerSuperSlowMo();
					} else if (record.Type == MVPTypes.WINNER && mvp.Type == MVPTypes.ACE) {
						mvp = new Stat(MVPTypes.ACE_WINNER);
						TriggerSuperSlowMo();
					} else if (record.Type == MVPTypes.WINNER && mvp.Type == MVPTypes.COLLATERAL_ACE) {
						mvp = new Stat(MVPTypes.COLLATERAL_ACE_WINNER);
						TriggerSuperSlowMo();
					} else if (record.Type.GetHashCode() > mvp.Type.GetHashCode())
						mvp = record;
					else if (record.Type == MVPTypes.COLLATERAL && mvp.Type == MVPTypes.COLLATERAL && (record.Special[0] > mvp.Special[0]))
						mvp = record;
				}
			}

			set_message();
			Show();

			if (winner) {
				Map.EndGame();
				Leader = leader;
			}

			Clear();

			//HEADS[leader].IsLeader = true;
		}

		public static bool Flawless() {
			foreach (Keys key in InactiveKeys) if (HEADS[key].Points > 0) return false;

			return true;
		}

		public static bool Ace() {
			byte c = (byte) InactiveKeys.Count;
			if (c < 3) return false;
			else if (ActiveKeys.Count > 0) return HEADS[ActiveKeys[0]].Kills == c;
			else foreach (Keys key in InactiveKeys) if (!(HEADS[key].Kills == 0 || HEADS[key].Kills == c - 1)) return false;

			return true;
		}

		public static void Clear() {
			records.Clear();
			mvp = new Stat();
			winner = false;
		}

		private static void set_message() {
			//convert mvp to string
			switch (mvp.Type) {
				case MVPTypes.POINTS:
					DisplayText = mvp.Hero + " on top, " + (Map.MaxPoints - short.Parse(mvp.Special)) + " pts left";
					return;

				case MVPTypes.COLLATERAL:
					if (mvp.Special == "2") DisplayText = mvp.Hero + " collateral!";
					else DisplayText = mvp.Hero + " " + mvp.Special + "-collateral!";
					break;

				case MVPTypes.GHOSTKILL:
					DisplayText = mvp.Hero + " ghostkill!";
					break;

				case MVPTypes.ASSIST: // deprecated
					DisplayText = mvp.Hero + " assist-kill on " + mvp.Special;
					break;

				case MVPTypes.TWO_PTS:
					DisplayText = "Get a 2-point lead";
					break;

				case MVPTypes.ACE:
					DisplayText = "Ace!";
					break;

				case MVPTypes.COLLATERAL_ACE:
					DisplayText = "Collateral ace!";
					break;

				case MVPTypes.ACE_WINNER:
					DisplayText = "Victory royale!";
					break;

				case MVPTypes.COLLATERAL_ACE_WINNER:
					DisplayText = "Collateral ace victory royale!";
					break;

				case MVPTypes.WINNER:
					DisplayText = WinMessage(mvp.Hero);
					break;

				case MVPTypes.FLAWLESS:
					DisplayText = "Flawless victory!";
					break;

				case MVPTypes.EARLY_KILL:
					DisplayText = mvp.Hero + " died <1s";
					break;

				default:
					DisplayText = mvp.Type.ToString();
					return;
			}
		}

		//public static event Action Winner;

		public static void Show() {
			if (AnimationsEnabled) {
				if (!Displaying) {
					lock (Program.window.draw_lock) {
						Appear.Reset();
						Displaying = true;
					}
				}
			} else window.writeln("> " + DisplayText);
		}

		public static void Hide() {
			if (AnimationsEnabled) {
				if (Displaying) {
					lock (Program.window.draw_lock) {
						Disappear.Reset();
						Displaying = false;
					}
				}
			}
		}

		public static void Draw(Graphics g) {
			GraphicsState gstate = g.Save();
			g.TranslateTransform(W / 2, W / 4f - (TutorialActive? W/7:0));
			float r = Displaying? (float) Appear:(float) Disappear;

			if (r != 0) {
				g.ScaleTransform(1, r);

				Font font = new Font(FONT, 56);
				SizeF sz = g.MeasureString(DisplayText, font);

				g.FillRectangle(new SolidBrush(color), -W / 2, -50, W, 100);
				g.DrawString(DisplayText, font, Program.ContrastMode? Brushes.White : Brushes.Black, -sz.Width / 2, -sz.Height / 2);
			}
			g.Restore(gstate);
		}

		public static string WinMessage(string hero) {
			List<string> msg = new List<string>() {
				$"{hero}!",
				$"{hero} win!",
				$"{hero} has won!",
				$"{hero} victory!",
				$"Victory to {hero}!",
				$"{hero} defeated y'all!",
				$"{hero} seems OP",
				$"Lucky {hero}",
				$"{hero} was top fragging",
				$"Congrats to {hero}!",
				$"Y'all got slain by {hero}"
			};
			
			return msg[R.Next(msg.Count)];
		}
	}

	
	class Stat {
		public string Hero { get; }
		public MVPTypes Type { get; }
		public string Special { get; }

		public Stat() {
			this.Hero = "";
			this.Type = MVPTypes.NONE;
			this.Special = "";
		}

		public Stat(MVPTypes type) {
			this.Hero = "";
			this.Type = type;
			this.Special = "";
		}

		public Stat(string hero, MVPTypes type) {
			this.Hero = hero;
			this.Type = type;
			this.Special = "";
		}

		public Stat(string hero, MVPTypes type, string special) {
			this.Hero = hero;
			this.Type = type;
			this.Special = special;
		}
	}
}
