using System;
using System.Threading;

namespace Eight_Orbits {
	class MyTimer {
		readonly System.Timers.Timer timer;
		readonly Thread exec;
		readonly Action executable;

		public volatile bool running = false;
		readonly object execute_lock = new { };

		public MyTimer(double interval, Action action, string name, bool IsBackground, ThreadPriority priority) {
			this.executable = action;

			System.Windows.Forms.Application.ApplicationExit += application_applicationexit;

			exec = new Thread(execute);
			exec.Name = name;
			exec.Priority = priority;
			exec.IsBackground = IsBackground;
			
			timer = new System.Timers.Timer(interval);
			timer.Elapsed += fire;
			timer.Start();
			exec.Start();
		}

		private void application_applicationexit(object sender, EventArgs e) {
			timer.Stop();
			running = true;
		}

		private void fire(object sender, EventArgs e) => this.running = true;

		private void execute() {
			while (Program.ApplicationRunning) {
				running = false;
				if (Program.SyncUpdate) SpinWait.SpinUntil(() => running || !Program.ApplicationRunning);
				lock (execute_lock) executable();
			}
		}

		public void Pause() => this.timer.Stop();
		public void UnPause() => this.timer.Start();
	}
}
