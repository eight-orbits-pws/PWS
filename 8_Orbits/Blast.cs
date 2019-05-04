using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Eight_Orbits.Program;
using Eight_Orbits.Properties;

namespace Eight_Orbits.Entities {
	class Blast : Circle, Visual {
		public static HashSet<Blast> All = new HashSet<Blast>();
		private bool Popped = false;
		private Head head;

		private Animatable PopR;
		private Animatable PopA;

		public Blast() {
			this.r = BlastR;
			this.pos = Map.generateSpawn(this.r);
			this.PopR = new Animatable(r, BlastRange, 22, AnimationTypes.SQRT);
			this.PopA = new Animatable(255, 128, 22);

			All.Add(this);
			OnUpdate += Update;
			window.DrawBlast += Draw;
			Map.OnClear += Remove;
		}

		public void Remove() {
			OnUpdate -= Update;
			window.DrawBlast -= Draw;
			Map.OnClear -= Remove;
			All.Remove(this);
		}

		public void Update() {
			if (Map.phase == Phases.STARTROUND) return;

			if (Popped) {
				if (PopR.Ended()) PopEnd();
			} else {
				foreach (Keys k in Active) {
					Head h = HEAD[k];

					if (h.act != Activities.DASHING && this.Collide(h)) {
						Pop(h);
					}
				}
			}
		}

		public void Pop(Head h) {
			Popped = true;
			Collect(h);
			head = h;
			PopR.Reset();
			PopA.Reset();

			PopR.OnEnd += PopEnd;
		}

		public void Draw(ref PaintEventArgs e) {
			if (Popped) e.Graphics.DrawEllipse(new Pen(Color.FromArgb((int) PopA, Color.White), 5*Scale), (float) pos.X - PopR, (float) pos.Y - PopR, PopR*2, PopR*2);
			else e.Graphics.DrawEllipse(new Pen(Color.White, 5 * Scale), (float) pos.X - r, (float) pos.Y - r, r*2, r*2);
		}

		public void Collect(Head head) {
			HashSet<Orb> toEat = new HashSet<Orb>(Orb.All);

			foreach (Orb orb in toEat) if (head.pos * orb.pos < BlastRange && orb.owner != head.keyCode && !orb.isBullet) head.Eat(orb.ID);
		}

		public void PopEnd() {
			if (!head.Died) Collect(head);
			Remove();
		}
	}
}
