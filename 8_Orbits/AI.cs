using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using Neural_Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Eight_Orbits.Program;

namespace Eight_Orbits {
	class AI {

		readonly Keys key;

		volatile bool pressed = false;
		volatile byte counter = 0;

		Head HEAD => HEADS[key];

		public AI(Keys key) {
			this.key = key;

			Program.OnUpdate += update;
		}

		private void action() {
			HEAD.Action();
			pressed = true;
			counter = 0;
		}

		private void remove() {
			Program.OnUpdateNNW -= update;
		}

		Ray ray = new Ray();
		readonly HashSet<double> distances = new HashSet<double>();
		readonly double diagonal = Math.Sqrt(W * W + H * H);
		readonly double m = HeadR + speed * 3;
		
		private void update() {
			if (pressed)
				if (counter++ >= 5) {
					pressed = false;
					HEAD.key.Release();
				}

			if (state != States.INGAME || HEAD.Died || HEAD.Dashing || HEAD.act == Activities.STARTROUND) return;
			// distance Orb or Blast in straight line
			// any Head in danger zone
			// dangerous Orb in danger zone
			// nearest dangerous Orb
			// distance to orbit side
			// in orbit or not

			ray.Set(HEAD.pos, HEAD.v);

			bool in_orbit = Map.InOrbit(HEAD.pos);


			distances.Add(diagonal);
			foreach (Circle orbit in Map.Orbits) if (ray.Hit(orbit)) distances.Add(ray.Distance(orbit));
			double d_orbit = distances.Min();
			distances.Clear();
			
			ray.laser.L = 160;
			if (cast_to_all(ray)) return;

			ray.laser.L = m*2;
			ray.laser.A -= Math.PI / 6;
			if (cast_to_all(ray)) return;

			ray.laser.A += Math.PI / 3;
			if (cast_to_all(ray)) return;

			double n_edible = diagonal;
			ray.laser.A = HEAD.v.A;
			ray.laser.L = diagonal;
			lock (Orb.OrbLock) {
				foreach (Orb orb in Orb.All) { if (orb.isWhite) { n_edible = Math.Min(n_edible, ray.AutoDistance(orb)); } }
			}
			lock (Blast.BlastLock) foreach (Circle blast in Blast.All) n_edible = Math.Min(n_edible, ray.AutoDistance(blast));

			if (HEAD.Orbiting) {
				if (n_edible < diagonal) {
					action();
					return;
				}
			} else {
				if (in_orbit && d_orbit < HeadR  && n_edible == diagonal) {
					action();
					return;
				}
			}
		}

		private bool cast_to_all(Ray ray) {
			double l = ray.laser.L;
			double nearest_danger = l;
			double nearest_edible = l;
			double d = 0;

			lock (Blast.BlastLock) {
				foreach (Circle blast in Blast.All) {
					nearest_edible = Math.Min(nearest_edible, ray.AutoDistance(blast));
				}
			}

			lock (ActiveLock) {
				foreach (Keys k in ActiveKeys) {
					if (k != key) nearest_danger = Math.Min(nearest_danger, ray.AutoDistance(HEADS[k]));
				}
			}
			lock (Orb.OrbLock) {
				for (int i = Orb.All.Count - 1; i >= 0; i--) {
					d = ray.AutoDistance(Orb.All[i]);

					if (Orb.All[i].isWhite) {
						nearest_edible = Math.Min(nearest_edible, d);
					} else if (!Orb.All[i].isWhite && Orb.All[i].isDangerTo(key)) {
						nearest_danger = Math.Min(nearest_danger, d);

						if (Orb.All[i].pos * HEAD.pos <= m + OrbR && (Map.InOrbit(HEAD.pos) != (HEAD.act == Activities.DEFAULT))) {
							if (SyncUpdate) new Thread(action).Start();
							else action();
							return true;
						}
					}
				}
			}

			//ray.laser.L = Math.Min(nearest_danger, nearest_edible);
			//System.Drawing.Graphics g = window.CreateGraphics();
			//g.ScaleTransform(window.scalar, window.scalar);
			//g.DrawLine(nearest_danger < nearest_edible? System.Drawing.Pens.Red : nearest_danger == nearest_edible? System.Drawing.Pens.Blue : System.Drawing.Pens.Green, (System.Drawing.PointF) ray.gun, (System.Drawing.PointF) (ray.gun + ray.laser));

			if (nearest_danger < l && nearest_danger <= nearest_edible) {
				if (nearest_danger == nearest_edible) {
					ray.laser.L += 0; // debugger trigger point
				}
				action();
				return true;
			}

			return false;
		}
	}
}
