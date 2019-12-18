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
using System.IO;

namespace Eight_Orbits {
	public partial class Window : Form {
		readonly HashSet<Keys> keydown = new HashSet<Keys>();
		public Color MapColor = Color.FromArgb(255, 222, 222, 222);
		public static long time = 0;
		public static long Time = 0;
		public event Action UpdateColors;

		public float scalar = 1;

		readonly Dictionary<Keys, Head> HEADSOnPause = new Dictionary<Keys, Head>();

		public Label OutputTxt;
		public event Action OnUpdateAnimation;
		public event Action ClearAI;

		private static bool single_instance_created = false;

		public Window() {
			if (single_instance_created) return;
			else single_instance_created = true;

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
				
				Paint += new PaintEventHandler(DrawPaint);
				OutputTxt.Visible = false;
			} else {
				AutoScroll = true;
				W = 1366;
				H = 768;
				C = W / 20f;
				SZR = 1;
				
			}
			MouseMove += new MouseEventHandler(this.Window_MouseMove);
			Resize += new EventHandler(on_resize);
			KeyDown += new KeyEventHandler(window_keydown);
			KeyUp += new KeyEventHandler(window_keyup);
		}

		private void Window_MouseMove(object sender, MouseEventArgs e) {
			if (FullScreen) {
				
			}
		}

		private void on_resize(object sender, EventArgs e) {
			this.scalar = this.DisplayRectangle.Width / 1366f;
		}
		
		public void ApplicationPause(object sender, EventArgs e) => ApplicationPause();
		public void ApplicationPause() {
			state = States.PAUSED;
			if (SyncUpdate) {
				UpdateThread.Stop();
				NeuralThread.Stop();
			}

			running = false;

			VisualThread.Stop();
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
			while (ApplicationRunning && !SyncUpdate && running) {
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
				if (state == States.NEWGAME && Neat.All.Count < 24) {
					Neat n = new Neat();
					n.SetupGenZero();
					n.AddKey();

					IKey.UpdateAll();
					Map.SetMaxPoints();
				}
				return;
			} else if (e.KeyCode == Keys.F1) {
				if (state != States.NEWGAME) return;
				if (ActiveKeys.Count > 0) new AI(ActiveKeys[ActiveKeys.Count - 1]);
				MVP.Flash($"AI added to {HEADS[ActiveKeys[ActiveKeys.Count - 1]].DisplayKey}");
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
					UpdateThread.Stop();
					NeuralThread.Stop();
					StartAsyncUpdate();
				} else {
					SyncUpdate = true;
					UpdateThread.Start();
					NeuralThread.Start();
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
			
			else if (e.KeyCode == Keys.F12) {
                if (!e.Alt) {
                    if (map is BotArena)
                    {
                        List<byte> bytes = new List<byte>();
                        bytes.AddRange(((BotArena)map).GetGeneration());
                        foreach (Neat bot in ((BotArena) map).bots)
                            bytes.AddRange(Neat.compile(bot));

                        SaveFileDialog dialog = new SaveFileDialog();
                        dialog.Filter = "Bot files (*.bot)|*.bot|All files (*.*)|*.*";
                        dialog.RestoreDirectory = true;

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            File.WriteAllBytes(dialog.FileName, bytes.ToArray());
                        }
                    }
                    else if (ActiveKeys.Count == 0)
                    {
                        OpenFileDialog dialog = new OpenFileDialog();
                        dialog.Filter = "Bot files (*.bot)|*.bot|All files (*.*)|*.*";
                        dialog.RestoreDirectory = true;

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            List<byte> bytes = new List<byte>(File.ReadAllBytes(dialog.FileName));

                            BotArena arena = new BotArena(BotArena.Type.MAX_POINTS);
                            arena.SetGeneration(bytes);
                            map = arena;

                            while (bytes.Count > 0)
                            {
                                arena.bots.Add(Neat.decompile(bytes));
                            }

                            arena.AddBots(false);
                            Map.StartGame();

                            Ingame = true;
                            state = States.INGAME;
                        }
                    }
                } else if (state == States.NEWGAME && !TutorialActive) {
					if (ActiveKeys.Count != 1)
						return;
					TUTO = new Tutorial(Map);
					TutorialActive = true;
					Map.StartGame();
					if (SyncUpdate) NeuralThread.Start();
				}
				return;
				// trigger tutorial
			} else if (e.KeyCode == Keys.F6) {
				if (ActiveKeys.Count != 0)
					return;

				BotArena arena = new BotArena(7, BotArena.Type.MAX_POINTS); /// <   -	-	-	-	-	-	-	-	-	-	-	-	-	-	-	-

				map = arena;
				arena.AddBots(false);
				Map.StartGame();

				Ingame = true;
				state = States.INGAME;

				return;
				// trigger bot arena
			} else if (e.KeyCode == Keys.F5) {
				if (state != States.NEWGAME) return;
				switch (Gamemode) {
					case Gamemodes.CLASSIC:
						MVP.SetText("Red chaos");
						Gamemode = Gamemodes.CHAOS_RED;
						foreach (Head head in HEADS.Values)
							head.NewColor(true);
						break;
					case Gamemodes.CHAOS_RED:
						MVP.SetText("Rainbow chaos");
						Gamemode = Gamemodes.CHAOS_RAINBOW;
						foreach (Head head in HEADS.Values)
							head.NewColor();
						break;
					case Gamemodes.CHAOS_RAINBOW:
						MVP.SetText("King of the hill");
						foreach (Head head in HEADS.Values)
							head.NewColor(false);
						Map.MaxOrbs = 7;
						Gamemode = Gamemodes.KING_OF_THE_HILL;
						break;

					case Gamemodes.KING_OF_THE_HILL:
						MVP.SetText("Yeet mode");
						Map.MaxOrbs = 8;
						Gamemode = Gamemodes.YEET_MODE;
						break;

					case Gamemodes.YEET_MODE:
						MVP.SetText("Classic");
						Map.MaxOrbs = 255;
						Gamemode = Gamemodes.CLASSIC;
						break;
				}

				UpdateColors?.Invoke();
				return;
			}

			switch (state) {
				case States.NEWGAME:
					//add new player
					if (e.KeyCode == Keys.Enter && ActiveKeys.Count > 0) { // (re)start game
						if (e.Shift) Map.ResumeGame();
						else Map.StartGame();
						if (SyncUpdate) NeuralThread.Start();
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
						ApplicationPause();
					} else if (ActiveKeys.Contains(ekey)) { //default action
						HEADS[ekey].Action();
					} break;
				case States.PAUSED:
					if (e.KeyCode == Keys.Escape) { // unpause
						state = States.INGAME;
						Ingame = true;
						if (SyncUpdate) {
							UpdateThread.Start();
							NeuralThread.Start();
						} else StartAsyncUpdate();
						VisualThread.Start();
						//ForcePaused = false;
						
					} else if (e.KeyCode == Keys.Enter) { // let players join
						HEADSOnPause.Clear();
						foreach (KeyValuePair<Keys, Head> player in HEADS) HEADSOnPause.Add(player.Key, player.Value);
						Map.EndGame();
						Map.Clear();
						Map.phase = Phases.NONE;
						state = States.NEWGAME;
						MVP.Show("Prepare!");
						lock (ActiveLock) foreach (Keys key in ActiveKeys) HEADS[key].v = IVector.Up;
						Ingame = false;
						VisualThread.Start();
						if (SyncUpdate) UpdateThread.Start();
						else StartAsyncUpdate();
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
			
			try {
				Map?.Draw(g);

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
			} catch (Exception) { }
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
