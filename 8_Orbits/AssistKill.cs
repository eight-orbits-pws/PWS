using Eight_Orbits.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Eight_Orbits {
	class AssistKill {

		private readonly List<Assist> data = new List<Assist>();
		private readonly List<Assist> analyze = new List<Assist>();
		private readonly List<Assist> remove = new List<Assist>();
		private readonly Thread thread;
		private bool wait = false;
		private static readonly object assist_lock = new { };

		public AssistKill() {
			this.thread = new Thread(main);
			this.thread.Name = "Assist_Thread";
			this.thread.Start();
		}

		public void Invoke() => this.wait = true;

		public void Add(Keys h0, Keys h1) { lock (assist_lock) data.Add(new Assist(h0, h1, Program.Tick)); }
		public void AddKill(Keys killer, Keys victim) { lock (assist_lock) analyze.Add(new Assist(killer, victim, Program.Tick)); }

		private void main() {
			while (Program.ApplicationRunning) {
				SpinWait.SpinUntil(() => this.wait || !Program.ApplicationRunning);
				this.wait = false;
				if (Program.ActiveKeys.Count <= 12) this.execute();
			}
		}

		private void execute() {
			// delete outdated assists
			lock (assist_lock) {
				for (int i = 0; i < data.Count; i++) {
					Assist assist = data[i];
					if (assist.Tick + 13 <= Program.Tick)
						remove.Add(assist);
				}

				for (int i = 0; i < remove.Count; i++) {
					Assist assist = remove[i];
					data.Remove(assist);
				}

				remove.Clear();

				for (int i = 0; i < analyze.Count; i++) {
					Assist kill = analyze[i];
					for (int j = 0; j < data.Count; j++) {
						Assist assist = data[j];
						if (Assist.Confirm(assist, kill))
							MVP.Add(Properties.MVPTypes.ASSIST,
									Head.getKeyString(assist.H0 == kill.H1 ? assist.H1 : assist.H0), Head.getKeyString(kill.H1)
								);
					}
				}

				analyze.Clear();
			}
		}
	}

	class Assist {
		
		public Keys H0 => h0;
		private Keys h0;

		public Keys H1 => h1;
		private Keys h1;

		public int Tick => tick;
		private int tick;

		public Assist(Keys h0, Keys h1, int tick) {
			this.h0 = h0;
			this.h1 = h1;
			this.tick = tick;
		}

		public static bool Confirm(Assist bounce, Assist kill) => (kill.h1 == bounce.h0 && kill.h0 != bounce.h1) || (kill.h1 == bounce.h1 && kill.h0 != bounce.h0);
	}
}
