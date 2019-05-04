using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;
using static Eight_Orbits.Entities.Orb;

namespace Eight_Orbits {
	class Tail {
		HashSet<byte> white = new HashSet<byte>();
		List<byte> tail = new List<byte>();
		int tailL = 0;

		List<IPoint> log = new List<IPoint>();
		int logL = 0;

		public Tail() {
			Map.OnClear += Clear;
			OnUpdate += Update;
			window.DrawTail += Draw;
		}
		~Tail() {
			Map.OnClear -= Clear;
			OnUpdate -= Update;
			window.DrawTail -= Draw;
		}

		void Clear() {
			white.Clear();
			tail.Clear();
			tailL = 0;
		}

		public void logAdd(IPoint pos) {
			//if (tailL == 5) throw new Exception();
			trim();
			logL = log.Count;
			log.Insert(0, pos);
		}

		public void Shoot() {
			if (tailL > 0) {
				All[tail[0]].Pew();
				tail.RemoveAt(0);
				tailL--;
			}
		}

		public void Die() {
			foreach (byte orb in white) Orb.All[orb].newOwner();
			foreach (byte orb in tail) Orb.All[orb].newOwner();
			white.Clear();
			tail.Clear();
			tailL = 0;
		}

		public void Add(byte id) {
			white.Add(id);
			Orb.All[id].r = OrbR / 2;
		}

		public void Remove(byte id) {
			if (white.Remove(id)) Orb.All[id].r = OrbR;
			else {
				tail.Remove(id);
				tailL--;
			}
		}

		public int length { get { return tailL; } }

		public void Update() {
			try {
				for (int i = 0; i < tailL; i++)
					All[tail[i]].Move(logIndex(i));

				HashSet<byte> toRemove = new HashSet<byte>();

				foreach (byte id in white) {
					Orb orb = All[id];
					orb.Move(logLast());

					if (orb.pos * logLast() < speed && IsNotGrowing()) {
						orb.state = OrbStates.OWNER;
						toRemove.Add(id);
						tail.Add(id);
						tailL++;
						orb.r = OrbR;
						AnimationControl.Add(new Animation(orb.pos, 5, 0, 0, (float) (OrbR * PHI), OrbR, Color.White, 255));
					}
				}

				foreach (byte id in toRemove)
					white.Remove(id);
			} catch (Exception) {
				Console.WriteLine("Some orbs were stolen!");
			}
		}

		public void Draw(ref PaintEventArgs e) {
			foreach (byte orb in white) All[orb].Draw(ref e);
			tail.Reverse();
			foreach (byte orb in tail) All[orb].Draw(ref e);
			tail.Reverse();

			if (tail.Count > 0) All[tail[0]].DrawKills(ref e);
		}

		IPoint logIndex(int i) { return log[Math.Min((i+1) * mBL, logL)].Copy(); }

		public IPoint logLast() { return log[Math.Min(tailL * mBL, logL)].Copy(); }

		public bool IsNotGrowing() { return tailL * mBL == logL; }
		

		private void trim() {
			int l = log.Count;
			int end = tailL * mBL;
			if (l > end) log.RemoveRange(end, l - end);
		}
	}
}
