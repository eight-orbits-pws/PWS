using System;
using System.Threading;
using System.Timers;

namespace Eight_Orbits {
	class MyTimer {
		readonly System.Timers.Timer timer;
		Thread exec;
		readonly String name;
		readonly ThreadPriority priority;
		readonly bool is_background;
		readonly Action executable;

		private volatile bool go = false;
		private volatile bool stop = false;
		readonly object execute_lock = new { };

		public MyTimer(double interval, Action action, string name, bool IsBackground, ThreadPriority priority) {
			this.executable = action;

			System.Windows.Forms.Application.ApplicationExit += new EventHandler(application_applicationexit);

			this.name = name;
			this.priority = priority;
			this.is_background = IsBackground;
			start();
			
			timer = new System.Timers.Timer(interval);
			timer.Elapsed += new ElapsedEventHandler(fire);
			timer.Start();
		}

		private void application_applicationexit(object sender, EventArgs e) {
			timer.Stop();
			go = true;
		}

		private void fire(object sender, EventArgs e) => this.go = true;

		private void execute() {
			while (Program.ApplicationRunning && !stop) {
				go = false;
				SpinWait.SpinUntil(() => go || stop || !Program.ApplicationRunning);
				if (go) lock (execute_lock) executable();
			}
		}

		private void start() {
			stop = false;
			this.exec = new Thread(new ThreadStart(execute));
			exec.Name = name;
			exec.Priority = priority;
			exec.IsBackground = is_background;
			exec.Priority = priority;
			exec.Start();
		}

		public void Pause() {
			this.timer.Stop();
			this.stop = true;
		}
		public void UnPause() {
			start();
			this.timer.Start();
		}
	}
}
