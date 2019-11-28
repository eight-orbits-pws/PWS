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
		readonly List<byte> white = new List<byte>(256);
		readonly List<byte> tail = new List<byte>(256);

		readonly object list_lock = new { };
		readonly List<IPoint> log = new List<IPoint>(); //backlog of positions
		readonly Keys key;

		public Tail(Keys key) {
			this.key = key;
			Map.OnClear += clear;
			window.DrawTail += Draw;
		}
		~Tail() {
			Map.OnClear -= clear;
			OnUpdate -= Update;
			window.DrawTail -= Draw;
		}

		private void clear() {
			lock (OrbLock) white.Clear();
			lock (OrbLock) tail.Clear();
		}
		
		public void logAdd(IPoint pos) {
			trim();
			lock (list_lock) {
				log.Insert(0, pos);
			}
		}

		public void Shoot() {
			lock (OrbLock) {
				if (tail.Count > 0) {
					Orb.All[tail[0]].Pew();
					tail.RemoveAt(0);
				}
			}
		}

		public void Die() {
            lock (OrbLock) {
                foreach (byte id in white) All[id].NewOwner();
                foreach (byte id in tail) All[id].NewOwner();
            }

			clear();
		}

		public void Add(byte id) {
			if (!white.Contains(id)) white.Add(id);
			Orb.All[id].r = OrbR / 2;
		}

		public void Remove(byte id) {
			if (white.Remove(id)) All[id].r = OrbR;
			else tail.Remove(id);
		}

		public int length => tail.Count;

		public void Update() {
			lock (OrbLock) {
				for (int i = tail.Count - 1; i >= 0; i--)
					All[tail[i]].Move(log_index(i));

				for (int i = white.Count - 1; i >= 0; i--) {
					byte id = white[i];
					Orb orb = All[id];
					orb.Move(logLast());

					if (orb.pos * logLast() < speed * 3D && IsNotGrowing()) {
						if (KingOfTheHill && length >= 3) {
							white.Remove(id);
							orb.NewOwner();
							orb.v /= 2d;
						} else if (YeetMode && length > 0) {
							white.Remove(id);
							orb.NewOwner();
							orb.v /= 2d;
						} else {
							white.Remove(id);
							if (orb.Owner == this.key) {
								orb.state = (byte) OrbStates.OWNER;
								if (!tail.Contains(id))
									tail.Add(id);
								orb.r = OrbR;
								new Animation(orb.pos, 5, 0, 0, (float)(OrbR * PHI), OrbR, Color.White, 255);
							}
						}
					}
				}
			}
		}

		public void Draw(Graphics g) {
			lock (OrbLock) {
				foreach (byte id in white) All[id].Draw(g);
				foreach (byte id in tail) All[id].Draw(g);

				if (tail.Count > 0) All[tail[0]].DrawKills(g);
			}
		}

		IPoint log_index(int i) { lock (list_lock) return log[Math.Min((i + 1) * mBL, log.Count - 1)].Copy(); }

		public IPoint logLast() { lock (list_lock) return log[Math.Min(tail.Count * mBL, log.Count - 1)].Copy(); }

		public bool IsNotGrowing() => tail.Count * mBL == log.Count - 1;

		private void trim() {
			int l = log.Count;
			int min = Math.Min(tail.Count * mBL, l);

			lock (list_lock) {
				log.RemoveRange(min, l - min);
			}
		}

	}
}
