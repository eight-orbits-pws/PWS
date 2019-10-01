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
				// else update in UpdateThread

				AssistThread = new AssistKill();

				if (AnimationsEnabled) {
					if (SyncUpdate) VisualThread = new MyTimer(60d / 1000d, window.Update_Visual, "Visual_Thread", false, ThreadPriority.Normal);
					else {
						System.Timers.Timer timer = new System.Timers.Timer(60d / 1000d);
						timer.Elapsed += window.Update_Visual;
						timer.Start();
					}
				}
			});
			//Map.OnStartGame += reset_tick;
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
		public static MyTimer NeuralThread;
		public static AssistKill AssistThread;

        public static volatile object updatinglocker = new { };

		/// <summary>
		/// A few settings all together:
		public static bool AnimationsEnabled = true;
		///	Disable to save energy and increase speed
		public static bool SyncUpdate = true;
		public static volatile bool ForcePaused = false;
		/// Disable to calcute as fast as possible.
		/// Don't wait for timestamps
		public static Gamemodes Gamemode = Gamemodes.DEFAULT;
		public static bool ChaosMode => Gamemode == Gamemodes.CHAOS_RAINBOW || Gamemode == Gamemodes.CHAOS_RED;
		/// </summary>
		public static bool ApplicationRunning = true;
		public static bool SlowMo = false;
		public static bool SpeedMo = false;
		private static void stop_running(object sender, EventArgs e) => ApplicationRunning = false;
		
		public static Window window = new Window();

		private static World map;
		public static World Map { get { return TutorialActive? TUTO : map; } set { map = value; } }

		public static bool TutorialActive = false;
		public static Tutorial TUTO;

		public static Dictionary<Keys, Head> HEADS = new Dictionary<Keys, Head>();
		public static volatile List<Keys> ActiveKeys = new List<Keys>();
		public static volatile object ActiveLock = new { };
		public static volatile List<Keys> InactiveKeys = new List<Keys>();

		public static Random R = new Random();
		public static TaskFactory Manager = new TaskFactory();

		public static float HeadR => 32f * Scale;
		public static float OrbR => 25f * Scale;
		public static float BlastR => 28f * Scale;
		public static float BlastRange => 256f * Scale;
		public static double speed => PHI * 4d * Scale;
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
		public static float Scale => scale;

		private static ulong frame = 0;
		private static ulong tick = 1;
		public static int Tick => (int) tick;

		public static event Action OnUpdate;
		public static event Action OnUpdateNNW;

        private static HashSet<Keys> check = new HashSet<Keys>();

		public static void Update() {
			byte iframe = 0;
			do {
				lock (updatinglocker) {
					frame++;
					SpinWait.SpinUntil(() => !(ForcePaused && ApplicationRunning));
					if (SlowMo) {
						if (SpeedMo && frame % 3 != 0)
							return;
						else if (!SpeedMo && frame % 12 != 0)
							return;
					}
					tick++;
					OnUpdate?.Invoke();
					
					if (!SyncUpdate)
						UpdateNeural();

					if (state == States.INGAME) {

						for (int b = Blast.All.Count - 1; b >= 0; b--)
							Blast.All[b].Update();
						if (tick % 6 == 0)
							Blast.Spawn();

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
                                        if (i >= Orb.All.Count)
                                            i = Orb.All.Count - 1;
										orb = Orb.All[i];
										if (p.Collide(orb)) {
											if (orb.NoOwner)
												p.Eat((byte) i);
											else if (orb.owner != p.KeyCode && orb.state != OrbStates.TRAVELLING && !p.INVINCIBLE) {
												new Coin(p.pos, HEADS[orb.owner].Reward((byte)i, p.KeyCode), HEADS[orb.owner].color);
												p.Die();
												AssistThread.AddKill(orb.owner, p.KeyCode);
												if (ChaosMode) TriggerSlowMo(30);
											}
										}
									}
								}
								if (!p.Died && !p.INVINCIBLE) {
									foreach (Keys b in check)
										if (p.pos * HEADS[b].pos < HeadR * 2)
											Bounce(p, HEADS[b]);

									check.Add(ActiveKeys[c]);
								}
							}
						}

						AssistThread.Invoke();
						//if (Map.phase == Phases.STARTROUND) return;
						check.Clear();
					}
				}

				if (iframe++ == 1) return; 
			} while (SpeedMo);
		}

		public static void TriggerSlowMo(int length) {
			if (SlowMo) return;
			new Thread(() => {
				SlowMo = true;
				SpeedMo = true;
				int tick = Tick + length;
				SpinWait.SpinUntil(() => Tick >= tick);
				SlowMo = false;
				SpeedMo = false;
			}).Start();
		}

		public static void TriggerSuperSlowMo() {
			if (SlowMo) return;
			new Thread(() => {
				SlowMo = true;
				int tick = Tick;
				SpinWait.SpinUntil(() => Tick >= tick + 20);
				SlowMo = false;
			}).Start();
		}

		public static void TriggerSpeedMo() {
			new Thread(() => {
				SpeedMo = true;
				int tick = Tick;
				SpinWait.SpinUntil(() => Tick >= tick + 60);
				SpeedMo = false;
			}).Start();
		}

		public static void UpdateNeural() {
			OnUpdateNNW?.Invoke();
		}

		public static Task WaitUntilTick(int endtick) {
			Task task = Task.Run(() => SpinWait.SpinUntil(() => Tick >= endtick));
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

			//normalize
			p.v.A -= normal;
			P.v.A -= normal;

			//swap the X values;
			double swappable = p.v.X;
			p.v.X = P.v.X;
			P.v.X = swappable;

			//denormalize
			p.v.A += normal;
			P.v.A += normal;

			//now move them apart
			double to_correct = HeadR * 2 - distance.L;
			distance.L = 1;

			p.pos -= distance * to_correct / 1.5d;
			P.pos += distance * to_correct / 1.5d;

			// add mvpcheck
			AssistThread.Add(p.KeyCode, P.KeyCode);

			// just act NORMAL
			p.act = P.act = Activities.DEFAULT;
			if (Gamemode == Gamemodes.CHAOS_RAINBOW) {
				Swap(ref p.color, ref P.color);
			}

			if (ChaosMode && Leader == p.KeyCode) {
				Leader = P.KeyCode;
			} else if (ChaosMode && Leader == P.KeyCode) {
				Leader = p.KeyCode;
			}
		}
		public static int BoolToInt(bool v) => v? 1 : 0;
    }
}
