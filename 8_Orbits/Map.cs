using Eight_Orbits.Entities;
using System;
using System.Drawing;
using Eight_Orbits.Properties;
using static Eight_Orbits.Program;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace Eight_Orbits {
	class World {
		public int MaxOrbs = 256;
		int orbSpawn = Settings.Default.OrbSpawn;
		public int StartRoundTime = 0;
		int EndRoundTime = 180;
		int EndGameTime = 300;

		public byte RoundsPassed = 0;
		public BlastSpawn blastSpawn = BlastSpawn.ONE;//(Settings.Default.BlastSpawn == "rare"? BlastSpawn.RARE : BlastSpawn.ONE);

		private HashSet<Orbit> orbits = new HashSet<Orbit>();
		public HashSet<Orbit> Orbits => orbits;
		IPoint tempCenter;

		private int tick = 0;
		public Phases phase = Phases.NONE;
		public int MaxPoints = 0;
		
		public event Action OnClear;
		public event Action OnClearRemove;
		public event Action OnStartRound;
		public event Action OnRevive;
		//public event Action OnEndRound;
		public event Action OnStartGame;
		public event Action OnEndGame;

		public World() {
			//MVP.Winner += EndGame;
			this.orbits = maps.Standard;
			OnUpdate += Update;

			if (!AnimationsEnabled) EndRoundTime = EndGameTime = 10;
		}
		
		public void SetMap() {
			//foreach (Orbit orbit in orbits) orbit.Remove();
			HashSet<Orbit> temp;
			do {
				temp = maps.Random;
			} while (this.orbits == temp && RoundsPassed > 0);

			this.orbits = temp;
		}

		public void Update() {
			if (phase != Phases.NONE) {
				tick++;
				switch (phase) {
					case Phases.STARTROUND: if (tick == StartRoundTime) {
							foreach (Keys k in ActiveKeys) HEADS[k].act = Activities.DEFAULT;
							phase = Phases.NONE;
							tick = 0;
						} break;

					case Phases.ENDROUND: if (tick == EndRoundTime) {
							StartRound();
							tick = 0;
						} break;

					case Phases.ENDGAME: if (tick == EndGameTime) {
							//SetMap();
							this.StartGame();
							tick = 0;
						} break;

					default: break;
				}
			} else tick = 0;
		}

		public void Draw(Graphics g) {
			g.DrawString(MaxPoints.ToString(), new Font(FONT, 8, FontStyle.Bold), Brushes.White, 6, 6);
			g.FillPolygon(new SolidBrush(window.MapColor), new PointF[8]{
				new PointF(C, 0f), new PointF(0, C),
				new PointF(0, W/2-C), new PointF(C, W/2),
				new PointF(W-C, W/2), new PointF(W, W/2-C),
				new PointF(W, C), new PointF(W-C, 0)
			});

			foreach (Orbit orbit in orbits)
				orbit.Draw(g);
		}

		public void spawnOrb() {
			if (Orb.All.Count >= this.MaxOrbs) return;

			byte i = 0;
			do {
				new Orb(true);
				i++;
			} while (i < orbSpawn);
		}

		public void newOrb() {
			if (orbSpawn < 0) new Orb(false);
			if (orbSpawn != 0) new Orb(false);
		}

		public IPoint generateSpawnStartRound(float r) {
			IPoint pos = generateSpawn(r);

			if (Math.Abs(IPoint.Center * pos - 72*2) <= HeadR * 2) return generateSpawnStartRound(r);
			else return pos;
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
		
		public void Clear() {
			lock (window.draw_lock) {
				OnClearRemove?.Invoke();
				OnClearRemove = null;

				OnClear?.Invoke();
				
				lock (Orb.OrbLock) Orb.All.Clear();
				lock (Blast.BlastLock) Blast.All.Clear();
				lock (ActiveLock) ActiveKeys.AddRange(InactiveKeys);
				InactiveKeys.Clear();
			}
		}

		public void SetMaxPoints() {
			switch (ActiveKeys.Count) {
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
				case 9: MaxPoints = 42;
					break;
				case 10: MaxPoints = 47;
					break;
				case 11: MaxPoints = 51;
					break;
				case 12: MaxPoints = 55;
					break;
				default: MaxPoints = Math.Min(5 + (ActiveKeys.Count - 2) * 5, 300);
					break;
			}
		}
		
		public void StartRound() {
			OnStartRound?.Invoke();
			Clear();
			StartRoundTime = (int) Math.Round(Math.PI/speed*72*3 * SZR);
			StartRotation = 2D*Math.PI*R.NextDouble();
			MVP.Hide();
			Map.spawnOrb();
			Sort();
			OnRevive?.Invoke();
			if (HEADS[ActiveKeys[0]].Points > 0) Leader = ActiveKeys[0];
			phase = Phases.STARTROUND;
		}

		public void EndRound() {
			Thread.CurrentThread.Name = "EndRound_Thread";
			MVP.Analyze();
			//OnEndRound();
			if (phase != Phases.ENDGAME) phase = Phases.ENDROUND;
		}

		public void StartGame() {
			SetMap();
			OnStartGame?.Invoke();
			window.writeln("Round: " + RoundsPassed++);
			StartRound();
		}

		public void EndGame() {
			OnEndGame?.Invoke();
			window.writeln(">> Game ended");
			phase = Phases.ENDGAME;
		}

		public void Sort() {
			//Assuming all is alive
			List<Keys> SortedKeys = new List<Keys>();
			List<int> SortedPoints = new List<int>();

			SortedKeys.Add(Keys.None);
			SortedPoints.Add(0);

			int pts; byte i;
			lock (ActiveLock) {
				foreach (Keys key in ActiveKeys) {
					pts = HEADS[key].Points;
					i = 0;
					while (true) {
						if (pts > SortedPoints[i] || i == (byte)(SortedPoints.Count - 1)) {
							SortedPoints.Insert(i, pts);
							SortedKeys.Insert(i, key);
							break;
						} else i++;
					}
				}
				SortedKeys.RemoveAt(SortedKeys.Count - 1);
				ActiveKeys = SortedKeys;
			}
			int l = ActiveKeys.Count;
			for (sbyte j = (sbyte) (l-1); j >= 0; j--) HEADS[ActiveKeys[j]].index = (byte) j;
			for (sbyte j = (sbyte) (InactiveKeys.Count-1); j >= 0; j--) HEADS[InactiveKeys[j]].index = (byte) (j+l);
			
			IKey.UpdateAll();
		}

		public void DrawCrown(Graphics g) {
			Circle p = new Circle(HEADS[Leader]);

			p.v.L = HeadR + Resources.crown.Height / 2 + 2;
			p.pos += p.v;

			g.TranslateTransform((float) p.pos.X, (float) p.pos.Y);
			g.RotateTransform((float) (p.v.A / Math.PI * 180d + 90d));
			g.ScaleTransform(SZR, SZR);
			g.DrawImage(Resources.crown, -Resources.crown.Width / 2, -Resources.crown.Height / 2);

			g.ResetTransform();
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
			
				//Slash.Add(new Orbit(2.5f/28f, 11.5f/14f, 1/15f));
				//Slash.Add(new Orbit(6/28f, 10/14f, 1/15f));
				//Slash.Add(new Orbit(6/28f, 10/14f, 1/15f));
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
			//public static HashSet<Orbit> Slash = new HashSet<Orbit>();

		}
	}
}
