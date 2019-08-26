﻿using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;
using static Eight_Orbits.Entities.Orb;

namespace Eight_Orbits {
	class Tail {
		List<byte> white = new List<byte>(256);
		List<byte> tail = new List<byte>(256);

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
			white.Clear();
			tail.Clear();
		}
		
		public void logAdd(IPoint pos) {
			trim();
			lock (list_lock) {
				log.Insert(0, pos);
			}
		}

		public void Shoot() {
			if (tail.Count > 0) {
				All[tail[0]].Pew();
				tail.RemoveAt(0);
			}
		}

		public void Die() {
			//int i;
			lock (OrbLock) {
				foreach (byte i in white) All[i].newOwner();
				for (int i = tail.Count - 1; i >= 0; i--) All[tail[i]].newOwner();
			}

			Clear();
		}

		public void Add(byte id) {
			white.Add(id);
			Orb.All[id].r = OrbR / 2;
		}

		public void Remove(byte id) {
			if (white.Remove(id)) Orb.All[id].r = OrbR;
			else tail.Remove(id);
		}

		public int length { get { return tail.Count; } }
		
		public void Update() {
			lock (OrbLock) for (int i = tail.Count - 1; i >= 0; i--) All[tail[i]].Move(logIndex(i));

			for (int i  = white.Count - 1; i >= 0; i--) {
				byte id = white[i];
				Orb orb = All[id];
				orb.Move(logLast());

				if (orb.pos * logLast() < speed * 3D && IsNotGrowing()) {
					orb.state = OrbStates.OWNER;
					white.Remove(id);
					tail.Add(id);
					orb.r = OrbR;
					new Animation(orb.pos, 5, 0, 0, (float) (OrbR * PHI), OrbR, Color.White, 255);
				}
			}
		}

		public void Draw(Graphics g) {
			int i;
			lock (OrbLock) {
				for (i = white.Count - 1; i >= 0; i--) All[white[i]].Draw(g);
				for (i = tail.Count - 1; i >= 0; i--) All[tail[i]].Draw(g);

				if (tail.Count > 0) All[tail[0]].DrawKills(g);
			}
		}

		IPoint logIndex(int i) { lock (list_lock) return log[Math.Min((i + 1) * mBL, log.Count - 1)].Copy(); }

		public IPoint logLast() { lock (list_lock) return log[Math.Min(tail.Count * mBL, log.Count - 1)].Copy(); }

		public bool IsNotGrowing() { return tail.Count * mBL == log.Count - 1; }
		
		private void trim() {
			int end = tail.Count * mBL;
			lock (list_lock) log = log.GetRange(0, Math.Min(tail.Count * mBL, log.Count));
		}

	}
}
