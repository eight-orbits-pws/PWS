using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;
using Eight_Orbits.Properties;
using System.Threading;

namespace Eight_Orbits.Entities {
	class Blast : Circle, Visual {
		public static volatile object BlastLock = new { };
		public static List<Blast> All = new List<Blast>();
		private Head head;
		public bool Popped = false;

		public static void Spawn() {
			if (TutorialActive) return;
			switch (Map.blastSpawn) {
				case BlastSpawn.RARE:
					if (R.NextDouble() < Math.Pow(2, -8)) lock (BlastLock) All.Add(new Blast());
					break;

				case BlastSpawn.ONE:
					if (All.Count < 1) lock (BlastLock) All.Add(new Blast());
					break;
			}
		}

		public static void DrawAll(Graphics g) {
			lock (BlastLock) foreach (Visual b in All) b.Draw(g);
		}

		public Blast() {
			this.r = BlastR;
			this.pos = Map.generateSpawn(this.r);
			//Map.OnClear += Remove;
		}

		public void Remove() {
			lock (BlastLock) All.Remove(this);
		}

		public void Update() {
			if (state != States.INGAME || Map.phase == Phases.STARTROUND || Popped) return;
			lock (ActiveLock) {
				foreach (Keys k in ActiveKeys) {
					Head h = HEADS[k];

					if (h.act != Activities.DASHING && this.Collide(h))
						Pop(h);
				}
			}
		}

		public void Pop(Head h) {
			Popped = true;
			head = h;
			_ = new Animation(pos, 13, BlastR, BlastRange, 5*Scale, 5*Scale, h.color, 200, AnimationTypes.SQRT);
			window.DrawBlast -= Draw;

			//Program.TriggerSlowMo(15);
			int endtick = Tick + 13;
			new Thread(() => {
				Thread.CurrentThread.Name = "BlastPop_Timer";
				SpinWait.SpinUntil(() => Tick >= endtick || !ApplicationRunning || Map.phase != Phases.NONE);
				lock(BlastLock) if (Tick >= endtick) PopEnd();
			}).Start();

			Remove();
		}

		public void Draw(Graphics g) => g.DrawEllipse(new Pen(Color.White, 5 * Scale), (float)this.pos.X - r, (float)this.pos.Y - r, r * 2, r * 2);

		/// do lock OrbLock on call
		public void Collect(Head head) {
			for (int i = Orb.All.Count - 1; i >= 0; i--) {
				Orb orb = Orb.All[i];
				if (pos * orb.pos < BlastRange + OrbR && orb.owner != head.KeyCode && !orb.isBullet)
					head.Eat(orb.ID);
			}
		}

		public void PopEnd() {
			if (!head.Died) {
				lock (Orb.OrbLock) Collect(head);
				if (Map.blastSpawn != BlastSpawn.ONE) {
					Blast blast;
					for (int i = All.Count - 1; i >= 0; i--) {
						blast = All[i];
						if (head.pos * blast.pos <= BlastRange) blast.Pop(head);
					}
				}
			}
		}

	}
}
