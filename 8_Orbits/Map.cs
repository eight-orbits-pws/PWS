using Eight_Orbits.Entities;
using System;
using System.Drawing;
using Eight_Orbits.Properties;
using static Eight_Orbits.Program;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Eight_Orbits {
	class World : Visual {
		//int MaxOrbs = 255;
		int orbSpawn = 2;
		int StartRoundTime = 0;
		byte RoundsPassed = 0;
		BlastSpawn blastSpawn = BlastSpawn.RARE;

		HashSet<Orbit> orbits = new HashSet<Orbit>();
		IPoint tempCenter;

		private int tick = 0;
		public Phases phase = Phases.NONE;
		public int MaxPoints = 0;

		public World() {
			MVP.Winner += EndGame;
			this.orbits = maps.Random;
			this.OnStartGame += SetMap;
		}

		public void SetMap() {
			this.orbits = maps.Random;
		}

		public void Update() {
			if (Blast.All.Count < Settings.Default.MaxBlast) new Blast();

			if (phase != Phases.NONE) {
				
				tick++;
				switch (phase) {
					case Phases.STARTROUND: if (tick == StartRoundTime) {
							foreach (Keys k in Active) HEAD[k].act = Activities.DEFAULT;
							phase = Phases.NONE;
							tick = 0;
						} break;

					case Phases.ENDROUND: if (tick == 300) {
							StartRound();
							tick = 0;
						} break;

					case Phases.ENDGAME: if (tick >= 500) {
							this.StartGame();
							tick = 0;
						} break;

					default: break;
				}
			}
		}

		public void Draw(ref PaintEventArgs e) {
			e.Graphics.DrawString(MaxPoints.ToString(), new Font(FONT, 8, FontStyle.Bold), Brushes.White, 6, 6);
			e.Graphics.FillPolygon(new SolidBrush(window.MapColor), new PointF[8]{
				new PointF(C, 0f), new PointF(0, C),
				new PointF(0, W/2-C), new PointF(C, W/2),
				new PointF(W-C, W/2), new PointF(W, W/2-C),
				new PointF(W, C), new PointF(W-C, 0)
			});

			foreach (Orbit orbit in orbits) orbit.Draw(ref e);
		}

		public void spawnOrb() {
			if (orbSpawn == 0) return;
			byte i = 0;
			do {
				new Orb();
				i++;
			} while (i < orbSpawn);
		}

		public void newOrb() {
			if (orbSpawn < 0) new Orb();
			if (orbSpawn != 0) new Orb();
		}

		public IPoint generateSpawn(float r) {
			IPoint pos = new IPoint();
			pos.X = (float)R.NextDouble() * W;
			pos.Y = (float)R.NextDouble() * W / 2;

			if (OutOfBounds(pos, r)) return generateSpawn(r);
			else return pos;
		}

		public bool OutOfBounds(IPoint pos, float r) {
			bool x = pos.X <= r
					|| pos.Y <= r
					|| pos.X >= W - r
					|| pos.Y >= W / 2 - r
					|| pos.X + pos.Y <= C + r * sqrt2
					|| W - pos.X + pos.Y <= C + OrbR * sqrt2
					|| W / 2 + pos.X - pos.Y <= C + OrbR * sqrt2
					|| W + W / 2f - pos.X - pos.Y <= C + OrbR * sqrt2;
			
			return x;
		}

		public bool InOrbit(IPoint pos) {
			foreach (Orbit orbit in orbits) {
				if (orbit.Inside(pos)) {
					tempCenter = orbit.pos;
					return true;
				}
			}

			return false;
		}

		public IPoint getOrbitCenter() {
			return tempCenter;
		}
		
		public event GameEvent OnClear;
		public void Clear() {
			OnClear?.Invoke();
			//if (Active.Count == 1) HEAD[Active[0]].tail.Die();
			//SpawnOrbs.Clear();
			//AllOrbs.Clear();
			//MapOrbs.Clear();
			//Blasts.Clear();
			//Bullets.Clear();
			Orb.All.Clear();
			Active.AddRange(Dead);
			Dead.Clear();
		}

		public void SetMaxPoints() {
			switch (Active.Count) {
				case 0: MaxPoints = 0;
					break;
				case 1: MaxPoints = 0;
					break;
				case 2: MaxPoints = 3;
					break;
				case 3: MaxPoints = 8;
					break;
				case 4: MaxPoints = 13;
					break;
				case 5: MaxPoints = 21;
					break;
				case 6: MaxPoints = 25;
					break;
				case 7: MaxPoints = 32;
					break;
				case 8: MaxPoints = 36;
					break;
				default: MaxPoints = 5 + (Active.Count - 2) * 5;
					break;
			}
		}

		public event GameEvent OnStartRound;
		public event GameEvent Revive;
		public event GameEvent OnEndRound;
		public event GameEvent OnStartGame;
		public event GameEvent OnEndGame;

		public void StartRound() {
			OnStartRound?.Invoke();
			Clear();
			StartRoundTime = (int) Math.Round(speed * 24 * SZR);
			//if (Active.Count == 1) HEAD[Active[0]].Die();
			MVP.Hide();
			Map.spawnOrb();
			Active.AddRange(Dead);
			Dead.Clear();
			Sort();
			Revive?.Invoke();
			if (HEAD[Active[0]].Points > 0) Leader = Active[0];
			phase = Phases.STARTROUND;
		}

		public void EndRound() {
			OnEndRound?.Invoke();
			if (phase == Phases.ENDGAME) return;
			phase = Phases.ENDROUND;
		}

		public void StartGame() {
			OnStartGame?.Invoke();
			Console.WriteLine(RoundsPassed++);
			StartRound();
		}

		public void EndGame() {
			OnEndGame?.Invoke();
			Console.WriteLine("Game ended");
			phase = Phases.ENDGAME;
		}

		public void Sort() {
			//Assuming all is alive
			List<Keys> SortedKeys = new List<Keys>();
			List<int> SortedPoints = new List<int>();

			SortedKeys.Add(Keys.None);
			SortedPoints.Add(0);

			int pts; byte i;
			foreach (Keys key in Active) {
				pts = HEAD[key].Points;
				i = 0;
				while (true) {
					if (pts > SortedPoints[i] || i == (byte) (SortedPoints.Count - 1)) {
						SortedPoints.Insert(i, pts);
						SortedKeys.Insert(i, key);
						break;
					} else i++;
				}
			}
			SortedKeys.RemoveAt(SortedKeys.Count - 1);
			Active = SortedKeys;
		}

		public void DrawCrown(ref PaintEventArgs e) {
			Circle p = new Circle(HEAD[Leader]);

			p.v.L = HeadR + Resources.crown.Height / 2 + 2;
			p.pos += p.v;

			e.Graphics.TranslateTransform((float) p.pos.X, (float) p.pos.Y);
			e.Graphics.RotateTransform((float) (p.v.A / Math.PI * 180d + 90d));
			e.Graphics.ScaleTransform(SZR, SZR);
			e.Graphics.DrawImage(Resources.crown, -Resources.crown.Width / 2, -Resources.crown.Height / 2);

			e.Graphics.ResetTransform();
		}

		public static class maps {

			public static void Create() {
				Standard.Add(new Orbit(.1f, .2f, 1f / 13));
				Standard.Add(new Orbit(.367f, .2f, 1f / 13));
				Standard.Add(new Orbit(.633f, .2f, 1f / 13));
				Standard.Add(new Orbit(.9f, .2f, 1f / 13));
				Standard.Add(new Orbit(2f / 9, .5f, 1f / 13));
				Standard.Add(new Orbit(.5f, .5f, 1f / 13));
				Standard.Add(new Orbit(7f / 9, .5f, 1f / 13));
				Standard.Add(new Orbit(.1f, .8f, 1f / 13));
				Standard.Add(new Orbit(.367f, .8f, 1f / 13));
				Standard.Add(new Orbit(.633f, .8f, 1f / 13f));
				Standard.Add(new Orbit(.9f, .8f, 1f / 13));

				First.Add(new Orbit(1/12f, 1/6f, 1/21f));
				First.Add(new Orbit(11/12f, 1/6f, 1/21f));
				First.Add(new Orbit(1/12f, 5/6f, 1/21f));
				First.Add(new Orbit(11/12f, 5/6f, 1/21f));
				First.Add(new Orbit(.5f, .5f, 3/16f));
				First.Add(new Orbit(1/6f, .5f, 3/32f));
				First.Add(new Orbit(5/6f, .5f, 3/32f));
				First.Add(new Orbit(1/4f, 1/5f, 1/28f));
				First.Add(new Orbit(3/4f, 1/5f, 1/28f));
				First.Add(new Orbit(1/4f, 4/5f, 1/28f));
				First.Add(new Orbit(3/4f, 4/5f, 1/28f));

				Dense.Add(new Orbit(1/12f, 1/8f, 1/24f));
				Dense.Add(new Orbit(1/12f, 3/8f, 1/24f));
				Dense.Add(new Orbit(1/12f, 5/8f, 1/24f));
				Dense.Add(new Orbit(1/12f, 7/8f, 1/24f));
				Dense.Add(new Orbit(1/6f, 1/4f, 1/24f));
				Dense.Add(new Orbit(1/6f, 1/2f, 1/24f));
				Dense.Add(new Orbit(1/6f, 3/4f, 1/24f));
				Dense.Add(new Orbit(1/4f, 1/8f, 1/24f));
				Dense.Add(new Orbit(1/4f, 3/8f, 1/24f));
				Dense.Add(new Orbit(1/4f, 5/8f, 1/24f));
				Dense.Add(new Orbit(1/4f, 7/8f, 1/24f));
				Dense.Add(new Orbit(1/3f, 1/4f, 1/24f));
				Dense.Add(new Orbit(1/3f, 1/2f, 1/24f));
				Dense.Add(new Orbit(1/3f, 3/4f, 1/24f));
				Dense.Add(new Orbit(5/12f, 1/8f, 1/24f));
				Dense.Add(new Orbit(5/12f, 3/8f, 1/24f));
				Dense.Add(new Orbit(5/12f, 5/8f, 1/24f));
				Dense.Add(new Orbit(5/12f, 7/8f, 1/24f));
				Dense.Add(new Orbit(1/2f, 1/4f, 1/24f));
				Dense.Add(new Orbit(1/2f, 1/2f, 1/24f));
				Dense.Add(new Orbit(1/2f, 3/4f, 1/24f));
				Dense.Add(new Orbit(7/12f, 1/8f, 1/24f));
				Dense.Add(new Orbit(7/12f, 3/8f, 1/24f));
				Dense.Add(new Orbit(7/12f, 5/8f, 1/24f));
				Dense.Add(new Orbit(7/12f, 7/8f, 1/24f));
				Dense.Add(new Orbit(2/3f, 1/4f, 1/24f));
				Dense.Add(new Orbit(2/3f, 1/2f, 1/24f));
				Dense.Add(new Orbit(2/3f, 3/4f, 1/24f));
				Dense.Add(new Orbit(3/4f, 1/8f, 1/24f));
				Dense.Add(new Orbit(3/4f, 3/8f, 1/24f));
				Dense.Add(new Orbit(3/4f, 5/8f, 1/24f));
				Dense.Add(new Orbit(3/4f, 7/8f, 1/24f));
				Dense.Add(new Orbit(5/6f, 1/4f, 1/24f));
				Dense.Add(new Orbit(5/6f, 1/2f, 1/24f));
				Dense.Add(new Orbit(5/6f, 3/4f, 1/24f));
				Dense.Add(new Orbit(11/12f, 1/8f, 1/24f));
				Dense.Add(new Orbit(11/12f, 3/8f, 1/24f));
				Dense.Add(new Orbit(11/12f, 5/8f, 1/24f));
				Dense.Add(new Orbit(11/12f, 7/8f, 1/24f));

				Simple.Add(new Orbit(1/4f, .5f, .2f));
				Simple.Add(new Orbit(3/4f, .5f, .2f));
				Simple.Add(new Orbit(.5f, 1/6f, .05f));
				Simple.Add(new Orbit(.5f, 5/6f, .05f));
				
				Steps.Add(new Orbit(1/7f, .26f, .12f));
				Steps.Add(new Orbit(1/3f, .6f, .1f));
				Steps.Add(new Orbit(.5f, .85f, 1/16f));
				Steps.Add(new Orbit(2/3f, .6f, .1f));
				Steps.Add(new Orbit(6/7f, .26f, .12f));

				Isles.Add(new Orbit(1/12f, 3/5f, 1/12f));
				Isles.Add(new Orbit(3/12f, 1/5f, 1/12f));
				Isles.Add(new Orbit(5/12f, 4/5f, 1/12f));
				Isles.Add(new Orbit(7/12f, 1/5f, 1/12f));
				Isles.Add(new Orbit(9/12f, 4/5f, 1/12f));
				Isles.Add(new Orbit(11/12f, 2/5f, 1/12f));

				Moon.Add(new Orbit(1/4f, 1/3f, .15f));
				Moon.Add(new Orbit(.42f, .87f, 1/30f));
				Moon.Add(new Orbit(.3f, .5f, 1/30f));
				Moon.Add(new Orbit(.72381f, .18924f, 1/32f));
				Moon.Add(new Orbit(.62348f, .54328f, 1/18f));
				Moon.Add(new Orbit(.76483f, .43617f, 1/32f));
				Moon.Add(new Orbit(.32732f, .32711f, 1/26f));
				Moon.Add(new Orbit(.17231f, .79801f, 1/24f));
				Moon.Add(new Orbit(.79782f, .79701f, 1/20f));
				Moon.Add(new Orbit(.87982f, .33241f, 1/22f));
				Moon.Add(new Orbit(.54311f, .12411f, 1/28f));
				Moon.Add(new Orbit(.06251f, .56781f, 1/30f));
				Moon.Add(new Orbit(.52879f, .78232f, 1/34f));
				
				Ring.Add(new Orbit(3/13f, 1/8f, 1/18f));
				Ring.Add(new Orbit(5/13f, 1/8f, 1/18f));
				Ring.Add(new Orbit(7/13f, 1/8f, 1/18f));
				Ring.Add(new Orbit(9/13f, 1/8f, 1/18f));
				Ring.Add(new Orbit(4/13f, 7/8f, 1/18f));
				Ring.Add(new Orbit(6/13f, 7/8f, 1/18f));
				Ring.Add(new Orbit(8/13f, 7/8f, 1/18f));
				Ring.Add(new Orbit(10/13f, 7/8f, 1/18f));
				Ring.Add(new Orbit(1/13f, .41f, 1/18f));
				Ring.Add(new Orbit(12/13f, .59f, 1/18f));
				Ring.Add(new Orbit(1/13f, 1/8f, 1/18f));
				Ring.Add(new Orbit(12/13f, 7/8f, 1/18f));
				Ring.Add(new Orbit(1/9f, 7/9f, .1f));
				Ring.Add(new Orbit(8/9f, 2/9f, .1f));
				
				Wave.Add(new Orbit(1/6f, 4/15f, 1/9f));
				Wave.Add(new Orbit(2/6f, 11/15f, 1/9f));
				Wave.Add(new Orbit(3/6f, 4/15f, 1/9f));
				Wave.Add(new Orbit(4/6f, 11/15f, 1/9f));
				Wave.Add(new Orbit(5/6f, 4/15f, 1/9f));

				Me.Add(new Orbit(1/4f, 1/4f, .12f));
				Me.Add(new Orbit(1/4f, 3/4f, .12f));
				Me.Add(new Orbit(.52f, .155f, .06f));
				Me.Add(new Orbit(.67f, .155f, .06f));
				Me.Add(new Orbit(.82f, .155f, .06f));
				Me.Add(new Orbit(.595f, .5f, .12f));
				Me.Add(new Orbit(.52f, .85f, .06f));
				Me.Add(new Orbit(.875f, .85f, .05f));
			}

			public static HashSet<Orbit> fromMapName(MapNames name) {
				switch (name) {
					case MapNames.STANDARD: return Standard;
					case MapNames.FIRST: return First;
					case MapNames.DENSE: return Dense;
					case MapNames.SIMPLE: return Simple;
					case MapNames.STEPS: return Steps;
					case MapNames.ISLES: return Isles;
					case MapNames.MOON: return Moon;
					case MapNames.RING: return Ring;
					case MapNames.WAVE: return Wave;
					case MapNames.ME: return Me;
					default: return new HashSet<Orbit>();
				}
			}

			public static HashSet<Orbit> Random { get {
					return fromMapName((MapNames) R.Next(MapNames.Length.GetHashCode()));
			} }
			public static HashSet<Orbit> Standard = new HashSet<Orbit>();
			public static HashSet<Orbit> First = new HashSet<Orbit>();
			public static HashSet<Orbit> Dense = new HashSet<Orbit>();
			public static HashSet<Orbit> Simple = new HashSet<Orbit>();
			public static HashSet<Orbit> Steps = new HashSet<Orbit>();
			public static HashSet<Orbit> Isles = new HashSet<Orbit>();
			public static HashSet<Orbit> Moon = new HashSet<Orbit>();
			public static HashSet<Orbit> Ring = new HashSet<Orbit>();
			public static HashSet<Orbit> Wave = new HashSet<Orbit>();
			public static HashSet<Orbit> Me = new HashSet<Orbit>();

		}
	}
}
