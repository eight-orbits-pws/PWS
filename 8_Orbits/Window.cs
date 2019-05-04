using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static Eight_Orbits.Program;
using Eight_Orbits.Properties;
using Eight_Orbits.Entities;
using System.Collections.Generic;

namespace Eight_Orbits {
	public partial class Window : Form {
        //public Graphics cvs;
        public Timer Updater = new Timer();
		//Timer updateVis = new Timer();

		Rectangle PlayingArea;

		HashSet<Keys> keydown = new HashSet<Keys>();
		public Color MapColor = Color.FromArgb(255, 222, 222, 222);

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
			//Program.Map.SetOrbits();

			PlayingArea = new Rectangle(0, 0, W, W / 2);

            Paint += Window_Paint;
			KeyDown += Window_KeyDown;
			KeyUp += Window_KeyUp;

            Updater.Tick += Update_Math;
			Updater.Tick += Update_Visual;
            Updater.Interval = 10;
			Updater.Start();
		}

		private void Update_Visual(object sender, EventArgs e) {
            Invalidate();
            Update();
		}
		private void Update_Math(object sender, EventArgs e) {
			Program.Update();
			Map.Update();
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

							GC.Collect();
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
			e.Graphics.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);
			e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			e.Graphics.Clear(Color.Black);

			
			Map.Draw(ref e);

			DrawWhite?.Invoke(ref e);
			DrawBlast?.Invoke(ref e);
			DrawTail?.Invoke(ref e);
			DrawBullet?.Invoke(ref e);
			DrawHead?.Invoke(ref e);
			DrawKeys?.Invoke(ref e);
			//foreach (Visual orb in SpawnOrbs) orb.Draw(ref e);
			//foreach (Visual orb in MapOrbs) orb.Draw(ref e);
			//foreach (Visual blast in Blasts) blast.Draw(ref e);
			//foreach (Keys key in Dead) HEAD[key].key.Draw(ref e);
			AnimationControl.Draw(ref e);
			
			if (Leader != Keys.None && HEAD[Leader].act != Activities.DEAD) Map.DrawCrown(ref e);
			//foreach (Keys key in Active) HEAD[key].Draw(ref e);

			MVP.Draw(ref e);
        }

		public event PaintEvent DrawWhite;
		public event PaintEvent DrawBlast;
		public event PaintEvent DrawTail;
		public event PaintEvent DrawBullet;
		public event PaintEvent DrawHead;
		public event PaintEvent DrawKeys;
    }
}
