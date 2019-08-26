using System;
using System.Drawing;
using System.Windows.Forms;
using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Eight_Orbits {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		
		[STAThread]
		static void Main() {
			World.maps.Create();

			Map = new World();
			Parallel.Invoke(() => {
				if (SyncUpdate) UpdateThread = new MyTimer(120d / 1000d, Update, "Update_Thread", false, ThreadPriority.AboveNormal);
				else window.StartAsyncUpdate();

				if (SyncUpdate) NeuralThread = new MyTimer(120d / 1000d, UpdateNeural, "Neural_Thread", false, ThreadPriority.Normal);
				//else window.StartAsyncUpdate();

				if (AnimationsEnabled) {
					if (SyncUpdate) VisualThread = new MyTimer(61d / 1000d, window.Update_Visual, "Visual_Thread", false, ThreadPriority.Normal);
					else {
						System.Timers.Timer timer = new System.Timers.Timer(60d / 1000d);
						timer.Elapsed += window.Update_Visual;
						timer.Start();
					}
				}
			});
			Map.OnStartGame += reset_tick;
			Map.OnClear += window.Clear;
			Map.spawnOrb();

			if (AnimationsEnabled) {
				Application.EnableVisualStyles();
				Application.VisualStyleState = System.Windows.Forms.VisualStyles.VisualStyleState.ClientAreaEnabled;
			} else {
				Console.SetError(System.IO.TextWriter.Null);
				//System.IO.
			}
			Application.ApplicationExit += stop_running;
			Application.Run(window);
		}

		public static MyTimer UpdateThread;
		public static MyTimer VisualThread;

		public static volatile object updatinglocker = new { };

		/// <summary>
		/// A few settings all together:
		public static bool AnimationsEnabled = true;
		///	Disable to save energy and increase speed
		public static bool SyncUpdate = true;
		/// Disable to calcute as fast as possible.
		/// Don't wait for timestamps
		public static bool KingOfTheHill = false;
		/// </summary>
		public static bool ApplicationRunning = true;
		private static void stop_running(object sender, EventArgs e) { ApplicationRunning = false; }
		
		public static Window window = new Window();

		public static World Map;

		public static Dictionary<Keys, Head> HEADS = new Dictionary<Keys, Head>();
		public static volatile List<Keys> ActiveKeys = new List<Keys>();
		public static volatile object ActiveLock = new { };
		public static volatile List<Keys> InactiveKeys = new List<Keys>();

		public static Random R = new Random();
		public static TaskFactory Manager = new TaskFactory();

		public static float HeadR { get { return 32f * Scale; } }
		public static float OrbR { get { return 25f * Scale; } }
		public static float BlastR { get { return 28f * Scale; } }
		public static float BlastRange { get { return 256f * Scale; } }
		public static double speed { get { return PHI * 4d * Scale; } }
		public static int W;
		public static int H;
		public static float C;
		public static float SZR;
		public static int mBL = 14;
		public static double sqrt2 = Math.Sqrt(2D);
		public static double PHI = (Math.Sqrt(5D) + 1D) / 2D;
		public static FontFamily FONT = FontFamily.GenericSansSerif;
		public static double StartRotation = 1d; 

		public static Keys Leader = Keys.None;

		public static States state = States.NEWGAME;
		public static bool Ingame = false;

		public static bool ContrastMode = Settings.Default.ContrastMode;
		private static float scale = SZR / Settings.Default.Scale;
		public static float Scale { get { return scale; } }

		private static ulong tick = 1;
		public static int Tick { get { return (int) tick; } }

		public static event Action OnUpdate;
		public static event Action OnUpdateNNW;

		private static HashSet<Keys> check = new HashSet<Keys>();

		public static void Update() {
			lock (updatinglocker) {
				tick++;
				OnUpdate?.Invoke();
				if (!SyncUpdate) UpdateNeural();

				for (int i = Blast.All.Count - 1; i >= 0; i--) Blast.All[i].Update();

				if (tick % 6 == 0) Blast.Spawn();

				if (state == States.INGAME) {
					//Update Players
					int i, c; //indexers
					Orb orb;
					lock (ActiveLock) {
						for (c = ActiveKeys.Count - 1; c >= 0; c--) {
							Head p = HEADS[ActiveKeys[c]];
							if (p.act == Activities.DASHING || p.act == Activities.STARTROUND)
								continue;


							lock (Orb.OrbLock) {
								for (i = Orb.All.Count - 1; i >= 0; i--) {
									orb = Orb.All[i];
									if (p.Collide(orb)) {
										if (orb.noOwner())
											p.Eat(orb.ID);
										else if (orb.owner != p.keyCode && orb.state != OrbStates.TRAVELLING) {
											new Coin(p.pos, HEADS[orb.owner].Reward(orb.ID, p.keyCode), HEADS[orb.owner].color);
											p.Die();

											if (KingOfTheHill && p.IsLeader) {
												Leader = orb.owner;
												MVP.Add(MVPTypes.WINNER, Head.getKeyString(orb.owner));
											}
										}
									}
								}
							}
							if (!p.Died) {
								foreach (Keys b in check)
									if (p.pos * HEADS[b].pos < HeadR * 2)
										Bounce(p, HEADS[b]);

								check.Add(ActiveKeys[c]);
							}
						}
					}

					if (Map.phase == Phases.STARTROUND) return;
					check.Clear();
					//toDie.Clear();
				}
			}
		}

		public static void UpdateNeural() {
			OnUpdateNNW?.Invoke();
		}

		public static Task WaitUntilTick(int endtick) {
			Task task = Task.Run(() => {
					SpinWait.SpinUntil(() => {
						return Tick >= endtick;
					});
				});
			return task;
		}

		public static void Nothing() { }

		public static void Swap<T>(ref T a, ref T b) {
			T temp = a;
			a = b;
			b = temp;
		}

		private static void Bounce(Head p, Head P) {
			//new Assist(p, P);
			IVector distance = P.pos - p.pos;
			double normal = distance.A;

			p.v.A -= normal;
			P.v.A -= normal;

			//swap the X values;
			double swappable = p.v.X;
			p.v.X = P.v.X;
			P.v.X = swappable;

			p.v.A += normal;
			P.v.A += normal;

			//now move them apart
			double to_correct = HeadR * 2 - distance.L;
			distance.L = 1;

			p.pos -= distance * to_correct / 2d;
			P.pos += distance * to_correct / 2d;

			p.act = P.act = Activities.DEFAULT;
		}

		private class timeout {
			private int starttick;
			private int duration;
			private Action e;
			private bool ended;

			public timeout(int ticks, Action e) {
				this.starttick = Tick;
				this.duration = ticks;
				this.e = e;
				this.ended = false;
				Program.OnUpdate += update;
				Map.OnClear += remove;
			}

			private void update() {
				if (Tick - starttick >= duration) end();
			}
			
			private void end() {
				if (ended) return;
				else ended = true;
				e?.Invoke();
				OnUpdate -= update;
				remove();
			}

			private void remove() {
				OnUpdate -= update;
				Map.OnClear -= remove;
			}
		}

		public static int BoolToInt(bool b) {
			if (b) return 1;
			else return 0;
		}

		private static void reset_tick() { }// tick = 0; }
    }
}
