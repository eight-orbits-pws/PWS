using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Eight_Orbits.Program;

namespace Eight_Orbits {
	class MVP {
		private static volatile List<Stat> records = new List<Stat>();
		public static volatile object RecordsLock = new { };
		private static Stat mvp = new Stat();

		private static readonly Animatable Appear = new Animatable(0, 1, 36, AnimationTypes.SIN);
		private static readonly Animatable Disappear = new Animatable(1, 0, 36, AnimationTypes.COS);
		private static bool Displaying = true;
		private static string DisplayText = "Prepare!";
		private static readonly Color color = Color.FromArgb(96, 32, 32, 32);

		private static bool winner = false;

		public static void Add(MVPTypes type) => records.Add(new Stat(type));

		public static void Add(MVPTypes type, string hero) {
			if (type == MVPTypes.GHOSTKILL && Displaying == true) {
				new Thread(() => {
					DisplayText = "";
					int tick = Tick + 1;
					SpinWait.SpinUntil(() => Tick > tick);

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
						//HEADS[leader].IsLeader = true;
						//} else if (Head.getKeyString(second) == hero && HEADS[leader].Points - HEADS[second].Points < 2 && HEADS[leader].Points >= Map.MaxPoints) {
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
			else if (HEADS[leader].Points - HEADS[second].Points < 2)
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
					if (record.Type == MVPTypes.ACE && mvp.Type == MVPTypes.COLLATERAL && mvp.Special.Equals((InactiveKeys.Count - 1)))
						mvp = new Stat(MVPTypes.COLLATERAL_ACE);
					else if (record.Type == MVPTypes.WINNER && mvp.Type == MVPTypes.ACE)
						mvp = new Stat(MVPTypes.ACE_WINNER);
					else if (record.Type == MVPTypes.WINNER && mvp.Type == MVPTypes.COLLATERAL_ACE)
						mvp = new Stat(MVPTypes.COLLATERAL_ACE_WINNER);
					else if (record.Type.GetHashCode() > mvp.Type.GetHashCode())
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
			if (InactiveKeys.Count < 3) return false;
			foreach (Keys key in InactiveKeys) if (HEADS[key].Kills > 0) return false;
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
					DisplayText = win_message(mvp.Hero);
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
				Displaying = true;
				Appear.Reset();
			} else window.writeln("> " + DisplayText);
		}

		public static void Hide() {
			if (AnimationsEnabled) {
				Displaying = false;
				Disappear.Reset();
			}
		}

		public static void Draw(Graphics g) {
			g.TranslateTransform(W / 2, W / 4f);
			float r = Displaying? (float) Appear:(float) Disappear;

			if (r != 0) {
				g.ScaleTransform(1, r);

				Font font = new Font(FONT, 56);
				SizeF sz = g.MeasureString(DisplayText, font);

				g.FillRectangle(new SolidBrush(color), -W / 2, -50, W, 100);
				g.DrawString(DisplayText, font, Program.ContrastMode? Brushes.White : Brushes.Black, -sz.Width / 2, -sz.Height / 2);
			}
			g.ResetTransform();
		}

		private static string win_message(string hero) {
			List<string> msg = new List<string>() {
				$"{hero}!",
				$"{hero} won!",
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
		private string hero;
		private MVPTypes type;
		private string special;

		public string Hero => hero;
		public MVPTypes Type => type;
		public string Special => special;

		public Stat() {
			this.hero = "";
			this.type = MVPTypes.NONE;
			this.special = "";
		}

		public Stat(MVPTypes type) {
			this.hero = "";
			this.type = type;
			this.special = "";
		}

		public Stat(string hero, MVPTypes type) {
			this.hero = hero;
			this.type = type;
			this.special = "";
		}

		public Stat(string hero, MVPTypes type, string special) {
			this.hero = hero;
			this.type = type;
			this.special = special;
		}
	}

	/// Deprecated because of lag
	class Assist {
		private static HashSet<Keys> bounced = new HashSet<Keys>();
		Head head;
		Head HEAD;

		public Assist(Head head, Head HEAD) {
			if (SyncUpdate && ActiveKeys.Count <= 12 && !bounced.Contains(head.KeyCode) && !bounced.Contains(HEAD.KeyCode)) {
				bounced.Add(head.KeyCode);
				bounced.Add(head.KeyCode);

				this.head = head;
				this.HEAD = HEAD;
				OnKill += kill_confirmed;
				//new Thread(analyze).Start();
			}
		}

		private async void analyze() {
			Thread.CurrentThread.Name = "Assist_Thread";
			if (Thread.CurrentThread.IsBackground) await WaitUntilTick(Tick + 13);

			OnKill -= kill_confirmed;
			bounced.Remove(this.head.KeyCode);
			bounced.Remove(this.HEAD.KeyCode);
		}

		private void kill_confirmed(Head killer, Head victim) {
			if (head != killer && head != victim && HEAD != killer && HEAD != victim) return;
			if ((head == killer && HEAD == victim) || (HEAD == killer && head == victim)) return;
			lock (MVP.RecordsLock) {
				if (head == victim) MVP.Add(MVPTypes.ASSIST, HEAD.DisplayKey, head.DisplayKey);
				else if (HEAD == victim) MVP.Add(MVPTypes.ASSIST, head.DisplayKey, HEAD.DisplayKey);
			}
		}

		private void remove() {
			OnKill -= kill_confirmed;
		}
	}
}
