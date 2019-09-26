using System;
using System.Threading;

namespace Eight_Orbits {
	class MyTimer {
		System.Timers.Timer t;
		Thread exec;
		Action executable;

		private int timeout = (int) Math.Pow(2, 14) - 1;
		public volatile bool running = false;
		readonly object execute_lock = new { };

		public MyTimer(double interval, Action action, string name, bool IsBackground, ThreadPriority priority) {
			this.executable = action;

			System.Windows.Forms.Application.ApplicationExit += Application_ApplicationExit;

			exec = new Thread(execute);
			exec.Name = name;
			exec.Priority = priority;
			exec.IsBackground = IsBackground;
			
			t = new System.Timers.Timer(interval);
			t.Elapsed += fire;
			t.Start();
			exec.Start();
		}

		private void Application_ApplicationExit(object sender, EventArgs e) {
			t.Stop();
			running = true;
		}

		private void fire(object sender, EventArgs e) {
			running = true;
		}

		private void execute() {
			while (Program.ApplicationRunning) {
				running = false;
				if (Program.SyncUpdate) SpinWait.SpinUntil(() => running || !Program.ApplicationRunning);
				lock (execute_lock) executable();
			}
		}

		public void Pause() {
			t.Stop();
		}

		public void UnPause() {
			t.Start();
		}
	}
}
