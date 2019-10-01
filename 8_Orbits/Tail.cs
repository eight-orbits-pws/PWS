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
		List<Orb> white = new List<Orb>(256);
		List<Orb> tail = new List<Orb>(256);

		volatile object list_lock = new { };
		volatile List<IPoint> log = new List<IPoint>(); //backlog of posistions

		public Tail() {
			Map.OnClear += Clear;
			window.DrawTail += Draw;
		}
		~Tail() {
			Map.OnClear -= Clear;
			OnUpdate -= Update;
			window.DrawTail -= Draw;
		}

		void Clear() {
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
			if (tail.Count > 0) {
				tail[0].Pew();
				tail.RemoveAt(0);
			}
		}

		public void Die() {
            //int i;
            if (!(Map is BotArena) || ((BotArena)Map).type != BotArena.Type.CONTINUEOUS)
            {
                lock (OrbLock)
                {
                    foreach (Orb i in white) i.NewOwner();
                    for (int i = tail.Count - 1; i >= 0; i--) tail[i].NewOwner();
                }
            }
            else
            {

                lock (OrbLock)
                {
                    foreach (Orb i in white) i.Remove();
                    for (int i = tail.Count - 1; i >= 0; i--) tail[i].Remove();
                }

            }

			Clear();
		}

		public void Add(byte id) {
			white.Add(All[id]);
			Orb.All[id].r = OrbR / 2;
		}

		public void Remove(byte id) {
			if (white.Remove(All[id])) Orb.All[id].r = OrbR;
			else tail.Remove(All[id]);
		}

		public int length => tail.Count;

		public void Update() {
            lock (OrbLock) for (int i = tail.Count - 1; i >= 0; i--)
                {
                    if (i >= tail.Count)
                        i = tail.Count - 1;
                    tail[i].Move(log_index(i));
                }

			for (int i  = white.Count - 1; i >= 0; i--) {
				Orb orb = white[i];
				orb.Move(logLast());

				if (orb.pos * logLast() < speed * 3D && IsNotGrowing()) {
					orb.state = OrbStates.OWNER;
					white.Remove(orb);
					tail.Add(orb);
					orb.r = OrbR;
					new Animation(orb.pos, 5, 0, 0, (float) (OrbR * PHI), OrbR, Color.White, 255);
				}
			}
		}

		public void Draw(Graphics g) {
			int i;
			lock (OrbLock) {
				for (i = white.Count - 1; i >= 0; i--) white[i].Draw(g);
				for (i = tail.Count - 1; i >= 0; i--) tail[i].Draw(g);

				if (tail.Count > 0) tail[0].DrawKills(g);
			}
		}

		IPoint log_index(int i) { lock (list_lock) return log[Math.Min((i + 1) * mBL, log.Count - 1)].Copy(); }

		public IPoint logLast() { lock (list_lock) return log[Math.Min
			(tail.Count * mBL, log.Count - 1)].Copy(); }

		public bool IsNotGrowing() => tail.Count * mBL == log.Count - 1;

		private void trim() {
			int end = tail.Count * mBL;
			lock (list_lock) log = log.GetRange(0, Math.Min(tail.Count * mBL, log.Count));
		}

	}
}
