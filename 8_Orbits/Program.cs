using System;
using System.Drawing;
using System.Windows.Forms;
using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using System.Collections.Generic;

namespace Eight_Orbits {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
				
			World.maps.Create();

			Map = new World();
			
			Map.OnEndRound += MVP.Analyze;
			Map.spawnOrb();
			Console.WriteLine((float) (new Animatable(5, 10, 64)) == 5f);
			
			Application.EnableVisualStyles();
			Application.Run(window);
		}

		public static Window window = new Window();

		public static World Map;
		
		//public static HashSet<Orb> SpawnOrbs = new HashSet<Orb>();
		//public static HashSet<Orb> MapOrbs = new HashSet<Orb>();
		//public static HashSet<Orb> Bullets = new HashSet<Orb>();
		//public static HashSet<Orb> AllOrbs = new HashSet<Orb>();

		//public static HashSet<Visual> Blasts = new HashSet<Visual>();
		//public static HashSet<Visual> BlastEnd = new HashSet<Visual>();

		public static Dictionary<Keys, Head> HEAD = new Dictionary<Keys, Head>();
		public static List<Keys> Active = new List<Keys>();
		public static List<Keys> Dead = new List<Keys>();

		public static Random R = new Random();

		public static float HeadR { get { return 32f * Scale; } }
		public static float OrbR { get { return 25f * Scale; } }
		public static float BlastR { get { return 28f * Scale; } }
		public static float BlastRange { get { return 256f * Scale; } }
		public static double speed { get { return 5.25D * Scale; } }
		public static int W;
		public static int H;
		public static float C;
		public static float SZR;
		public static int mBL = 16;
		public static double sqrt2 { get { return Math.Sqrt(2D); } }
		public static double PHI { get { return (Math.Sqrt(5D) + 1D) / 2D; } }
		public static FontFamily FONT = FontFamily.GenericSansSerif;

		public static Keys Leader = Keys.None;

		public static States state = States.NEWGAME;

		public static bool ContrastMode = false;
		public static float Scale { get { return SZR / Settings.Default.Scale; } }

		private static int tick = 0;
		public static int Tick { get { return tick; } }

		public static event GameEvent OnUpdate;

		public static void Update() {
			tick++;
			OnUpdate?.Invoke();
			if (state == States.INGAME) {
				//Update Players
				HashSet<Keys> check = new HashSet<Keys>();
				HashSet<Head> toDie = new HashSet<Head>();
				foreach (Keys a in Active) {
					Head p = HEAD[a];
					if (p.act == Activities.DASHING ||  p.act == Activities.STARTROUND) continue;
						int L = check.Count;
						foreach (Keys b in check) {
							Head P = HEAD[b];
							if (p.pos * P.pos < HeadR * 2)
								Bounce(ref p, ref P);
						}
						check.Add(a);
					

					HashSet<byte> toEat = new HashSet<byte>();

					foreach (Orb orb in Orb.All) if (p.Collide(orb)) {
							if (orb.noOwner()) toEat.Add(orb.ID);
							else if (orb.owner != p.keyCode && orb.state != OrbStates.TRAVELLING) {
								toDie.Add(p);
								HEAD[orb.owner].Reward(orb.ID);
							}
						}

					foreach (byte nom in toEat) p.Eat(nom);
				}

				if (Map.phase == Phases.STARTROUND) return;

				foreach (Head h in toDie) h.Die();

				//Update orbs
				foreach (Visual orb in Orb.All) orb.Update();
				foreach (Visual blast in Blast.All) blast.Update();

				/*if (BlastEnd.Count > 0) {
					foreach (Visual blast in BlastEnd) Blasts.Remove(blast);
					BlastEnd.Clear();
				}*/
			}
		}

		public static void Swap<T>(ref T a, ref T b) {
			T temp = a;
			a = b;
			b = temp;
		}

		private static void Bounce(ref Head p, ref Head P) {
			IVector d = P.pos - p.pos;
			double n = d.A;

			p.v.A -= n;
			P.v.A -= n;

			//swap the X values;
			double temp = p.v.X;
			p.v.X = P.v.X;
			P.v.X = temp;

			p.v.A += n;
			P.v.A += n;

			//now move them apart
			double tc = HeadR * 2 - d.L;
			d.L = 1;

			p.pos -= d * tc / 2d;
			P.pos += d * tc / 2d;

			p.act = P.act = Activities.DEFAULT;
		}
    }
}
