using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static Eight_Orbits.Program;
using Eight_Orbits.Properties;
using Eight_Orbits.Entities;
using System.Collections.Generic;
using System.Timers;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading;

namespace Eight_Orbits {
	public partial class Window : Form {
		//public Graphics cvs;
		public System.Timers.Timer Updater = new System.Timers.Timer(1000D/60D);
		//Timer updateVis = new Timer();

		Rectangle PlayingArea;

		HashSet<Keys> keydown = new HashSet<Keys>();
		public Color MapColor = Color.FromArgb(255, 222, 222, 222);
		public static long time = 0;
		public static long Time = 0;
		public static List<double> fps = new List<double>(64);

		public Thread update;
		

		public Window() {
            InitializeComponent();
			Console.WriteLine(Screen.AllScreens[0].WorkingArea.ToString());
			Show();
			this.Location = Screen.AllScreens[0].WorkingArea.Location;
            //Bounds = Screen.GetWorkingArea(new Point());
			Bounds = Screen.AllScreens[0].WorkingArea;
			W = Bounds.Width;
			H = Bounds.Height;
			C = W / 20f;
			SZR = W / 1366f;
			BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            Cursor.Hide();

			PlayingArea = new Rectangle(0, 0, W, W / 2);
			//update = new Thread(new ThreadStart(Update));
			//Console.WriteLine(1000d/(double)(Updater.Interval));
            Paint += Window_Paint;
			KeyDown += Window_KeyDown;
			KeyUp += Window_KeyUp;

            Updater.Elapsed += Update_Math;
			if (AnimationsEnabled) Updater.Elapsed += Update_Visual;
			Updater.Start();
			//Updater.AutoReset = true;
		}

		private void Update_Visual(object sender, EventArgs e) {
            Invalidate();
		}
		private void Update_Math(object sender, EventArgs e) {
			time = DateTime.Now.Millisecond;
			if (time < Time) fps.Insert(0, 1000d/(1000L + time - Time));
			else fps.Insert(0, 1000d/(time - Time));
			if (fps.Count == 64) fps.RemoveAt(63);
			if (Tick%12==0) Console.WriteLine(fps.Average());
			Time = time;
			Program.Update();
			Map?.Update();
        }

		private void Window_KeyUp(object sender, KeyEventArgs e) {
			keydown.Remove(e.KeyCode);
			try {
				if (state == States.INGAME) HEAD[e.KeyCode].key.Release();
			} catch (KeyNotFoundException) {
				//no problem
			}
		}

		private void Window_KeyDown(object sender, KeyEventArgs e) {
			if (keydown.Contains(e.KeyCode)) {
				return;
			} else {
				keydown.Add(e.KeyCode);
			}

			if (e.KeyCode == Keys.F4) {
				ContrastMode = !ContrastMode;
				return;
			}

			switch (state) {
				case States.NEWGAME:
					//add new player
					if (e.KeyCode == Keys.Enter) {
						state = States.INGAME;
						Map.StartGame();
					} else if (e.KeyCode == Keys.Escape) {
						Active.Clear();
						HEAD.Clear();
						Map.MaxPoints = 0;
					} else if (0 <= e.KeyValue && e.KeyValue < 256) {
						if (Active.Contains(e.KeyCode)) {
							Active.Remove(e.KeyCode);
							HEAD[e.KeyCode].Remove();
							HEAD.Remove(e.KeyCode);
						} else {
							HEAD.Add(e.KeyCode, new Head(e.KeyCode));
							Active.Add(e.KeyCode);
							Map.SetMaxPoints();
						}
					}
					break;
				case States.INGAME:
					if (e.KeyCode == Keys.Escape) {
						state = States.PAUSED;
						Updater.Stop();
					} else if (Active.Contains(e.KeyCode)) {
						HEAD[e.KeyCode].Action();
					} break;
				case States.PAUSED:
					if (e.KeyCode == Keys.Escape) {
						state = States.INGAME;
						Updater.Start();
					} else if (e.KeyCode == Keys.Enter) {
						Map.EndGame();
						state = States.NEWGAME;
						
						Updater.Start();
					}
					break;
			}
		}

        private void Window_Paint(object sender, PaintEventArgs e) {
			//e.Graphics.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);
			e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
			e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
			e.Graphics.InterpolationMode = InterpolationMode.Low;
			e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
			e.Graphics.Clear(Color.Black);

			
			Map.Draw(ref e);

			DrawWhite?.Invoke(ref e);
			DrawBlast?.Invoke(ref e);
			DrawTail?.Invoke(ref e);
			DrawBullet?.Invoke(ref e);
			DrawHead?.Invoke(ref e);
			DrawKeys?.Invoke(ref e);
			AnimationControl.Draw(ref e);
			
			if (Leader != Keys.None && HEAD[Leader].act != Activities.DEAD) Map.DrawCrown(ref e);

			MVP.Draw(ref e);
			e.Graphics.Flush(FlushIntention.Flush);

			
        }

		public event PaintEvent DrawWhite;
		public event PaintEvent DrawBlast;
		public event PaintEvent DrawTail;
		public event PaintEvent DrawBullet;
		public event PaintEvent DrawHead;
		public event PaintEvent DrawKeys;
    }
}
