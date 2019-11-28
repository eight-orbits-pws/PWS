using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;
using Eight_Orbits.Properties;
using System.Threading;

namespace Eight_Orbits.Entities {
	class Blast : Circle, Visual {
		public static readonly object BlastLock = new { };
		public static readonly List<Blast> All = new List<Blast>();
		private Head head;
		public volatile bool Popped = false;
		private readonly int spawn_tick;

		public static void Spawn() {
			if (TutorialActive) return;
			switch (Map.blastSpawn) {
				case BlastSpawn.RARE:
					if (R.NextDouble() < Math.Pow(2, -8)) lock (BlastLock) All.Add(new Blast());
					break;

				case BlastSpawn.ONE:
					if (All.Count < 1) lock (BlastLock) All.Add(new Blast());
					break;

				case BlastSpawn.NONE:
					return;
			}
		}

		public static void DrawAll(Graphics g) {
			lock (BlastLock) foreach (Visual b in All) b.Draw(g);
		}

		public Blast() {
			this.r = BlastR;
			this.pos = Map.generateSpawn(this.r);
			this.spawn_tick = Tick;
			new Animation(this.pos, 15, 0, BlastR, 5, 5, Color.FromArgb(48, 48, 48), Color.White, AnimationTypes.SIN);
		}

		public void Remove() {
			lock (BlastLock) All.Remove(this);
		}

		public void Update() {
			if (state != States.INGAME || Map.phase == Phases.STARTROUND || Tick < spawn_tick + 15 || Popped) return;
			lock (ActiveLock) {
				Head h;
				foreach (Keys k in ActiveKeys) {
					h = HEADS[k];

					if (!h.Dashing && !h.Died && this.Collide(h)) {
						this.Pop(h);
						break;
					}
				}
			}
		}

		public void Pop(Head h) {
			Popped = true;
			this.head = h;
			_ = new Animation(this.pos, 13, BlastR, BlastRange, 5*Scale, 5*Scale, h.color, 200, AnimationTypes.SQRT);
			window.DrawBlast -= Draw;

			//Program.TriggerSlowMo(15);
			new Thread(() => {
				int tick = Tick;
				Thread.CurrentThread.Name = "BlastPop_Timer";
				SpinWait.SpinUntil(() => Tick >= tick + 13 || !ApplicationRunning || Map.phase != Phases.NONE);
				lock (BlastLock) { if (Tick >= tick + 13) PopEnd(); }
			}).Start();

			Remove();
		}

		public void Draw(Graphics g) => g.DrawEllipse(new Pen(Color.White, 5 * Scale), (float)this.pos.X - r, (float)this.pos.Y - r, r * 2, r * 2);

		/// do lock OrbLock on call
		Orb orb;
		public void Collect(Head head) {
			lock (Orb.OrbLock) {
				for (int i = Orb.All.Count - 1; i >= 0; i--) {
					orb = Orb.All[i];
					if (this.pos * orb.pos < BlastRange + OrbR && orb.Owner != head.KeyCode && !orb.isBullet)
						head.Eat(orb.ID);
				}
			}
		}

		public void PopEnd() {
			if (!head.Died) {
				this.Collect(head);

				if (Map.blastSpawn != BlastSpawn.ONE) {
					Blast blast;
					for (int i = All.Count - 1; i >= 0; i--) {
						blast = All[i];
						if (this.pos * blast.pos <= BlastRange) blast.Pop(head);
					}
				}
			}
		}

	}
}
