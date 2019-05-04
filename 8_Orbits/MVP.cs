using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Eight_Orbits.Program;

namespace Eight_Orbits {
	public class MVP {
		private static List<Stat> records = new List<Stat>();
		private static Stat mvp = new Stat();

		private static Animatable Appear = new Animatable(0, 1, 60);
		private static Animatable Disappear = new Animatable(1, 0, 60);
		private static bool Displaying = true;
		private static string DisplayText = "Prepare!";
		private static Color color = Color.FromArgb(64, 32, 32, 32);

		private static bool winner = false;
		//public static bool Winner { get { return winner; } }

		public static void Add(MVPTypes type) {
			records.Add(new Stat(type));
		}

		public static void Add(MVPTypes type, string hero) {
			records.Add(new Stat(hero, type));
		}

		public static void Add(MVPTypes type, string hero, string special) {
			records.Add(new Stat(hero, type, special));
		}

		public static void Analyze() {
			//get points
			Keys leader;
			Keys second = Keys.None;
			if (Active.Count == 1) {
				leader = Active[0];
			} else
				leader = Dead[0];

			foreach (Keys p in Dead) {
				if (HEAD[leader].Points < HEAD[p].Points) {
					second = leader;
					leader = p;
				} else if (second == Keys.None || HEAD[second].Points < HEAD[p].Points) {
					second = p;
				}
			}

			if (Ace()) Add(MVPTypes.ACE);
			if (HEAD[leader].Points < Map.MaxPoints)
				Add(MVPTypes.POINTS, HEAD[leader].DisplayKey, HEAD[leader].Points.ToString());
			else if (HEAD[leader].Points - HEAD[second].Points < 2)
				Add(MVPTypes.TWO_PTS);
			else if (Flawless()) { winner = true; Add(MVPTypes.FLAWLESS); } 
			else { winner = true; Add(MVPTypes.WINNER, HEAD[leader].DisplayKey); }

			foreach (Stat record in records) {
				if (record.Type.GetHashCode() > mvp.Type.GetHashCode()) mvp = record;
				else if (record.Type == MVPTypes.COLLATERAL && mvp.Type == MVPTypes.COLLATERAL && (record.Special[0] > mvp.Special[0])) mvp = record;
				//COLLAT -> ACE -> WIN
				if (record.Type == MVPTypes.ACE && mvp.Type == MVPTypes.COLLATERAL && mvp.Special.Equals((Active.Count - 1))) mvp = new Stat(MVPTypes.COLLATERAL_ACE);
				else if (record.Type == MVPTypes.WINNER && mvp.Type == MVPTypes.ACE) mvp = new Stat(MVPTypes.ACE_WINNER);
				else if (record.Type == MVPTypes.WINNER && mvp.Type == MVPTypes.COLLATERAL_ACE) mvp = new Stat(MVPTypes.COLLATERAL_ACE_WINNER);
			}

			SetMessage();
			Show();

			if (winner) {
				Winner?.Invoke();
				Leader = leader;
			}

			Clear();
		}

		public static bool Flawless() {
			foreach (Keys key in Dead) if (HEAD[key].Points > 0) return false;

			return true;
		}

		public static bool Ace() {
			foreach (Keys key in Dead) if (HEAD[key].Kills > 0) return false;
			return true;
		}

		public static void Clear() {
			records.Clear();
			mvp = new Stat();
			winner = false;
		}

		private static void SetMessage() {
			//convert mvp to string
			switch (mvp.Type) {
				case MVPTypes.POINTS:
					DisplayText = mvp.Hero + " on top, " + (Map.MaxPoints - short.Parse(mvp.Special)) + " pts left";
					return;

				case MVPTypes.COLLATERAL:
					if (mvp.Special=="2") DisplayText = mvp.Hero + " collateral!";
					else DisplayText = mvp.Hero + " " + mvp.Special + "-collateral!";
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
					DisplayText = mvp.Hero + " won with ace!";
					break;

				case MVPTypes.COLLATERAL_ACE_WINNER:
					DisplayText = "Collateral ace! Win deserved.";
					break;

				case MVPTypes.WINNER:
					DisplayText = mvp.Hero + " won!";
					break;

				case MVPTypes.FLAWLESS:
					DisplayText = "Flawless!";
					break;

				default:
					DisplayText = "?";
					return;
			}
		}
		
		public static event GameEvent Winner;

		public static void Show() {
			if (AnimationsEnabled) {
				Displaying = true;
				Appear.Reset();
			} else Console.WriteLine("> " + DisplayText);
		}

		public static void Hide() {
			if (AnimationsEnabled) {
				Displaying = false;
				Disappear.Reset();
			}
		}

		public static void Draw(ref PaintEventArgs e) {
			e.Graphics.TranslateTransform(W/2, W / 4f);
			float r;
			if (Displaying) r = (float) Appear; else r = (float) Disappear;
			if (r != 0) {
				e.Graphics.ScaleTransform(1, r);

				Font font = new Font(FONT, 56);
				SizeF sz = e.Graphics.MeasureString(DisplayText, font);

				e.Graphics.FillRectangle(new SolidBrush(color), -W / 2, -50, W, 100);
				e.Graphics.DrawString(DisplayText, font, Brushes.Black, -sz.Width / 2, -sz.Height/2);
			}
			e.Graphics.ResetTransform();
		}
	}

	public class Stat {
		private string hero;
		private MVPTypes type;
		private string special;

		public string Hero { get { return hero; } }
		public MVPTypes Type { get { return type; } }
		public string Special { get { return special; } }

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
}
