using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static Eight_Orbits.Program;
using Eight_Orbits.Properties;
using Eight_Orbits.Entities;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading;
using Neural_Network;
using System.Threading.Tasks;

namespace Eight_Orbits {
	public partial class Window : Form {
		HashSet<Keys> keydown = new HashSet<Keys>();
		public Color MapColor = Color.FromArgb(255, 222, 222, 222);
		public static long time = 0;
		public static long Time = 0;
		public event Action UpdateColors;

		private float scalar = 1;

		Dictionary<Keys, Head> HEADSOnPause = new Dictionary<Keys, Head>();

		public Label OutputTxt;

		public Image background;
		public event Action OnUpdateAnimation;

		public Window() {
			InitializeComponent();
			OutputTxt = (Label) Controls[0];
			OutputTxt.Text = "Hello World!";
			this.MinimizeBox = true;
			this.MaximizeBox = true;
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			Show();

			//OnUpdate += () => OnUpdateAnimation?.Invoke();

			if (AnimationsEnabled) {
				//this.Location = Screen.AllScreens[0].WorkingArea.Location;
				Bounds = Screen.AllScreens[0].WorkingArea;
				W = 1366;//Bounds.Width;
				H = 768;//(int)(W * 9f / 16f);
				C = W / 20f;
				SZR = 1;
				scalar = Width / 1366f;
				BackColor = Color.Black;
				FormBorderStyle = FormBorderStyle.None;
				WindowState = FormWindowState.Maximized;
				DoubleBuffered = true;
				Cursor.Hide();
				
				Paint += DrawPaint;
				OutputTxt.Visible = false;
			} else {
				AutoScroll = true;
				W = 1366;
				H = 768;
				C = W / 20f;
				SZR = 1;
				
			}
			MouseMove += this.Window_MouseMove;
			Resize += on_resize;
			KeyDown += window_keydown;
			KeyUp += window_keyup;
		}

		private void Window_MouseMove(object sender, MouseEventArgs e) {
			if (FullScreen) {
				
			}
		}

		private void on_resize(object sender, EventArgs e) {
			this.scalar = this.DisplayRectangle.Width / 1366f;
		}

		public void Update_Visual(object sender, EventArgs e) {
			Update_Visual();
		}

		public void Update_Visual() {
			OnUpdateAnimation?.Invoke();
			Invalidate();
		}

		bool running = false;
		public void StartAsyncUpdate() {
			if (running) return;
			running = true;

			Thread t = new Thread(async_update_math);
			t.IsBackground = false;
			t.Priority = ThreadPriority.Highest;
			t.Name = "Math_Thread";
			t.IsBackground = false;
			t.Start();
		}

		private void async_update_math() {
			while (ApplicationRunning && !SyncUpdate) {
				Program.Update();
			}

			running = false;
		}
		
		private void window_keyup(object sender, KeyEventArgs e) {
			keydown.Remove(e.KeyCode);
			if (e.KeyCode == Keys.F7) {
				SlowMo = false;
				return;
			} else if (e.KeyCode == Keys.F8) {
				SpeedMo = false;
				return;
			}

			if (Ingame && ActiveKeys.Contains(e.KeyCode)) HEADS[e.KeyCode].key.Release();
		}
		
		private void window_keydown(object sender, KeyEventArgs e) {

			if (keydown.Contains(e.KeyCode) || e.KeyCode == Keys.NumLock || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu)
				return;
			else keydown.Add(e.KeyCode);

			Keys ekey = e.KeyCode;
			if (e.KeyCode == Keys.Delete)
				ekey = Keys.Decimal;
			else if (e.KeyCode == Keys.Insert)
				ekey = Keys.NumPad0;
			else if (e.KeyCode == Keys.Clear)
				ekey = Keys.NumPad5;

			else if (e.KeyCode == Keys.F2) {
				ContrastMode = !ContrastMode;
				return;
			} else if (e.KeyCode == Keys.F3) {
				if (state == States.NEWGAME && Neat.All.Count < 22) {
					Neat n = new Neat();
					n.SetupGenZero();
					n.AddKey();

					IKey.UpdateAll();
					Map.SetMaxPoints();
				}
				return;
			} else if (e.KeyCode == Keys.F1) {
				// debug shortcut
				return;
			} else if (e.KeyCode == Keys.F4 && !e.Alt) {
				if (state == States.NEWGAME) {
					if (Neat.All.Count > 0) {
						Neat toRemove = Neat.All.Last();
						toRemove.Remove();
						IKey.UpdateAll();
					}
					Map.SetMaxPoints();
				}
				return;
			} else if (e.KeyCode == Keys.F9 && e.Alt) {
				if (ActiveKeys.Count == 0 || InactiveKeys.Count == 0)
					return;

				HEADS[InactiveKeys[0]].Reward(0, ActiveKeys[0]);
				HEADS[ActiveKeys[0]].Die();
				return;
			} else if (e.KeyCode == Keys.F10) {
				if (SyncUpdate) {
					SyncUpdate = false;
					UpdateThread.Pause();
					NeuralThread.Pause();
					StartAsyncUpdate();
				} else {
					SyncUpdate = true;
					UpdateThread.UnPause();
					NeuralThread.UnPause();
				}
				return;
			} else if (e.KeyCode == Keys.F7) {
				SlowMo = true;
				return;
			} else if (e.KeyCode == Keys.F8) {
				SpeedMo = true;
				return;
			} else if (e.KeyCode == Keys.F11) {
				FullScreen = !FullScreen;
				if (FullScreen) {
					Width = Screen.PrimaryScreen.Bounds.Width;
					FormBorderStyle = FormBorderStyle.None;
					WindowState = FormWindowState.Maximized;
					Cursor.Hide();
				} else {
					Width = 1024;
					Height = 600;
					FormBorderStyle = FormBorderStyle.Sizable;
					WindowState = FormWindowState.Normal;
					Cursor.Show();
				}
				return;
			}

			else if (e.KeyCode == Keys.F12 && e.Alt && state == States.NEWGAME && !TutorialActive) {
				if (ActiveKeys.Count != 1) return;
				TUTO = new Tutorial(Map);
				TutorialActive = true;
				Map.StartGame();
				return;
				// trigger tutorial
			}

            else if (e.KeyCode == Keys.F6)
            {
                if (ActiveKeys.Count != 0) return;

                BotArena arena = new BotArena(6, BotArena.Type.MAX_POINTS); /// <   -	-	-	-	-	-	-	-	-	-	-	-	-	-	-	-

                Map = arena;
                arena.AddBots(false);
                Map.StartGame();

                Ingame = true;
                state = States.INGAME;

                return;
                // trigger bot arena
            }
			
			else if (e.KeyCode == Keys.F5) {
				switch (Gamemode) {
					case Gamemodes.DEFAULT:
						Gamemode = Gamemodes.CHAOS_RED;
						break;
					case Gamemodes.CHAOS_RED:
						Gamemode = Gamemodes.CHAOS_RAINBOW;
						break;
					case Gamemodes.CHAOS_RAINBOW:
						Gamemode = Gamemodes.DEFAULT;
						break;

				}
				foreach (Head head in HEADS.Values)
					head.NewColor(Gamemode == Gamemodes.CHAOS_RED);
				UpdateColors?.Invoke();
				return;
			}

			switch (state) {
				case States.NEWGAME:
					//add new player
					if (e.KeyCode == Keys.Enter && ActiveKeys.Count > 0) { // (re)start game
						if (e.Shift) Map.ResumeGame();
						else Map.StartGame();
						ForcePaused = false;
						Ingame = true;
						state = States.INGAME;
					} else if (e.KeyCode == Keys.Escape) { // clear all keys
						HashSet<Neat> NeatCopy = new HashSet<Neat>(Neat.All);
						foreach (Neat neat in NeatCopy) neat.Remove();
						HashSet<Head> HeadCopy = new HashSet<Head>(HEADS.Values);
						foreach (Head head in HeadCopy) head.Remove();
						Map.MaxPoints = 0;
						Leader = Keys.None;
					} else if (0 <= e.KeyValue && e.KeyValue < 256) { //add key
						lock (ActiveLock) {
							if (ActiveKeys.Contains(ekey)) {
								HEADS[ekey].Remove();
							} else {
								if (HEADSOnPause.ContainsKey(ekey)) {
									HEADS.Add(ekey, new Head(HEADSOnPause[ekey]));
								} else
									HEADS.Add(ekey, new Head(ekey));
								ActiveKeys.Add(ekey);
								Map.SetMaxPoints();
							}
						}
						IKey.UpdateAll();
					}
					break;
				case States.INGAME:
					if (e.KeyCode == Keys.Escape) { //pause game
						state = States.PAUSED;
                        if (SyncUpdate)
					        Program.UpdateThread.Pause();
						else ForcePaused = true;
						running = false;
					} else if (ActiveKeys.Contains(ekey)) { //default action
						HEADS[ekey].Action();
					} break;
				case States.PAUSED:
					if (e.KeyCode == Keys.Escape) { // unpause
						state = States.INGAME;
						Ingame = true;
						if (SyncUpdate) UpdateThread.UnPause();
						ForcePaused = false;
						running = true;
					} else if (e.KeyCode == Keys.Enter) { // let players join
						HEADSOnPause = new Dictionary<Keys, Head>(HEADS);
						Map.EndGame();
						Map.Clear();
						Map.phase = Phases.NONE;
						state = States.NEWGAME;
						lock (ActiveLock) foreach (Keys key in ActiveKeys) HEADS[key].v = IVector.Up;
						Ingame = false;
						if (SyncUpdate) UpdateThread.UnPause();
					}
					break;
			}
		}

		public volatile object draw_lock = new { };
		private volatile bool drawing = true;
		
		public void DrawPaint(object sender, PaintEventArgs e) {
			if (!drawing) return;
			Graphics g = e.Graphics;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.Low;
			g.TextRenderingHint = TextRenderingHint.AntiAlias;

			g.ScaleTransform(scalar, scalar);
				//lock (draw_lock) {

			Map?.Draw(g);
			//if (draw_log != null) g.DrawImageUnscaled(draw_log, W/2, H/2);

			DrawWhite?.Invoke(g);
			Blast.DrawAll(g);
			DrawTail?.Invoke(g);
			DrawBullet?.Invoke(g);
			DrawHead?.Invoke(g);

			DrawKeys?.Invoke(g);
			DrawAnimation?.Invoke(g);

			if (ActiveKeys.Contains(Leader))
				Map.DrawCrown(g);

			MVP.Draw(g);

			//draw_log = new BufferedGraphics(). Bitmap.FromHbitmap(g.GetHdc());
			//draw_log.MakeTransparent(MapColor);
		}

		public void Clear() {
			DrawWhite = DrawBlast = DrawBullet = DrawAnimation = null;
		}

		public event PaintEvent DrawWhite;
		public event PaintEvent DrawBlast;
		public event PaintEvent DrawTail;
		public event PaintEvent DrawBullet;
		public event PaintEvent DrawHead;
		public event PaintEvent DrawKeys;
		public event PaintEvent DrawAnimation;

		delegate void debugtext(string txt);
		
		volatile object writelock = new { };
		public void writeln() {
			writeln("");
		}

		public void writeln<T>(T obj) {
			writeln(obj.ToString());
		}

		public void writeln(string txt) {
			if (OutputTxt.InvokeRequired) {
				Invoke(new debugtext(writeln), txt);
			} else {
				OutputTxt.Text += "\n" + txt;
			}
		}

		public void write<T>(T obj) {
			write(obj.ToString());
		}

		public void write(string txt) {
			if (OutputTxt.InvokeRequired) {
				Invoke(new debugtext(write), txt);
			} else {
				OutputTxt.Text += txt;
			}
		}
	}
}
