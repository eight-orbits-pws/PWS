using Eight_Orbits.Entities;
using Eight_Orbits.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static Eight_Orbits.Program;

namespace Eight_Orbits {
	class Tutorial : World {
		// tutorial exists of stages
		// each stage explains a game element
		int stage = 1;
		int step = 0;
		World world;
		String text = "In each stage,\nthe goal is to grab\nall the white balls";
		Font font = new Font(FontFamily.GenericSansSerif, 24 * SZR, FontStyle.Italic);
		Brush brush = new SolidBrush(Color.FromArgb(255, 32, 32, 32));
		PointF point = new PointF(.5f * W, .26f * W);
		Head bot;
		Head bot1;
		Keys key_player;

		Action trigger_die_restart;

		HashSet<Orbit> Empty = new HashSet<Orbit>();
		HashSet<Orbit> Standard = new HashSet<Orbit>();

		public Tutorial(World world) {
			Orbits = Empty;
			this.world = world;
			key_player = ActiveKeys[0];

			trigger_die_restart = () => { if (phase != Phases.ENDROUND) EndRound(); };
			HEADS[key_player].OnDie += trigger_die_restart;

			Standard.Add(new Orbit(.1f, .2f, 1f / 13));
			Standard.Add(new Orbit(.367f, .2f, 1f / 13));
			Standard.Add(new Orbit(.633f, .2f, 1f / 13));
			Standard.Add(new Orbit(.9f, .2f, 1f / 13));
			Standard.Add(new Orbit(2f / 9, .5f, 1f / 13));
			Standard.Add(new Orbit(.5f, .5f, 1f / 13));
			Standard.Add(new Orbit(7f / 9, .5f, 1f / 13));
			Standard.Add(new Orbit(.1f, .8f, 1f / 13));
			Standard.Add(new Orbit(.633f, .8f, 1f / 13f));
			Standard.Add(new Orbit(.9f, .8f, 1f / 13));
			
			new Thread(() => {
				Thread.CurrentThread.Name = "Tutorial_Manager";
				MVP.SetText("Tutorial");
				MVP.Hide();
				SpinWait.SpinUntil(() => step == 1 || !ApplicationRunning);
				step = 0;
				stage = 2;
				EndRound();

				SpinWait.SpinUntil(() => step == 23 || !ApplicationRunning);
				step = 0;
				stage = 3;
				EndRound();

				SpinWait.SpinUntil(() => step > 1 || !ApplicationRunning);
				step = 0;
				stage = 4;
				EndRound();

				SpinWait.SpinUntil(() => step == 4 || !ApplicationRunning);
				step = 0;
				stage = 5;
				EndRound();

				SpinWait.SpinUntil(() => step == 15 || !ApplicationRunning);
				step = 0;
				stage = 6;
				bot1.OnDie += () => step++;
				EndRound();

				SpinWait.SpinUntil(() => step == 13 || !ApplicationRunning);
				step = 0;
				stage = 7;
				EndGame();
			}).Start();
		}
		//public override event Action OnClear;

		public override void SetMap() {
			Orbits = new HashSet<Orbit>();
		}

		public override void spawnOrb() {
			// not used, see stages
			return;
		}

		public override void newOrb() {
			// inject endround trigger
			step++;
		}

		public override void StartGame() {
			state = States.INGAME;
			Ingame = true;
			StartRound();
		}

		public override void StartRound() {
			lock (updatinglocker) {
				start_round();
				step = 0;
				Clear();
				world.Clear();
				revive();
				world.revive();
				phase = Phases.NONE;
				StartStage();
			}
		}

		public override void EndRound() {
			MVP.SetText($"Stage {stage-1}");
			MVP.Flash();
			phase = Phases.ENDROUND;
			if (ApplicationRunning) new Thread(() => {
				Thread.CurrentThread.Name = "EndRound";
				int endtick = Tick + 180;
				SpinWait.SpinUntil(()=> Tick > endtick || !ApplicationRunning);
				StartRound();
			}).Start();
		}

		public override void EndGame() {
			MVP.SetText("Tutorial completed");
			MVP.Show();
			HEADS[key_player].OnDie -= trigger_die_restart;
			//phase = Phases.ENDGAME;

			if (ApplicationRunning) new Thread(() => {
				Thread.CurrentThread.Name = "EndGame";
				int endtick = Tick + 180;
				SpinWait.SpinUntil(() => Tick > endtick || !ApplicationRunning);

				
				TutorialActive = false;
				start_round();
				Clear();
				world.Clear();

				bot.Remove();
				bot1.Remove();

                Neural_Network.Neat n = new Neural_Network.Neat();
                n.SetupGenZero();
                n.AddKey();

                IKey.UpdateAll();

				Program.Map.StartGame();
			}).Start();
		}

		public override void Draw(Graphics g) {
			base.Draw(g);
			string[] lines = text.Split("\n".ToCharArray());
			List<SizeF> sz_line = new List<SizeF>();
			foreach (string line in lines) sz_line.Add(g.MeasureString(line, font));

			SizeF sz_total = g.MeasureString(text, font);

			float last_y = 2 * sz_line[1].Height - sz_total.Height;

			// Fuck this shit, just assume three lines
			g.DrawString(lines[0], font, brush, point.X - sz_line[0].Width / 2, point.Y - sz_total.Height / 2f, StringFormat.GenericDefault);
			g.DrawString(lines[1], font, brush, point.X - sz_line[1].Width / 2, point.Y - sz_line[1].Height / 2f, StringFormat.GenericDefault);
			g.DrawString(lines[2], font, brush, point.X - sz_line[2].Width / 2, point.Y - last_y / 2f, StringFormat.GenericDefault);
		}

		public void StartStage() {
			switch (stage) {
				case 1: stage1();
					break;
				case 2: Stage2();
					break;
				case 3: Stage3();
					break;
				case 4: Stage4();
					break;
				case 5: Stage5();
					break;
				case 6: Stage6();
					break;
				case 7: return;
			}
		}

		private void stage1() {
			// introduce the goal of each stage in the tutorial
			Head player = HEADS[key_player];
			player.act = Activities.DEFAULT;
			player.pos = new IPoint(W*4/5, W*.19d);
			player.color = Color.Blue;
			player.v = speed * IVector.Left;
			lock (Orb.OrbLock) new Orb(true).pos = new IPoint(W*7/8, W*.19d);
		}

		public void Stage2() {
			text = "Right before hitting an obstacle,\ndash to avoid it\n ";

			Head player = HEADS[key_player];
			player.act = Activities.DEFAULT;
			player.pos = new IPoint(W*.6d, W*.19d);
			player.v = speed * IVector.Left;
			lock (Orb.OrbLock) new Orb(true).pos = new IPoint(W*7/8, W*.19d);
			// introduce the dash with a black wall

			if (!HEADS.Keys.Contains(Keys.F13)) {
				bot = new Head(Keys.F13);
				bot.DisplayKey = "";
				bot.color = Color.Red;
				HEADS.Add(Keys.F13, bot);
				lock (ActiveLock) ActiveKeys.Add(Keys.F13);

				bot1 = new Head(Keys.F14);
				bot1.DisplayKey = "";
				bot1.color = Color.Red;
				HEADS.Add(Keys.F14, bot1);
				lock (ActiveLock) ActiveKeys.Add(Keys.F14);
			}

			bot.act = Activities.DEFAULT;
			bot.pos = new IPoint(W*3/4, W/4);
			bot.v = IVector.Down;

			lock (Orb.OrbLock) for (int i = 1; i < 12; i++)
				new Orb(true).pos = new IPoint(W*3/4, W/2*i/13);

			bot1.act = Activities.DEFAULT;
			bot1.pos = new IPoint(W*1/4, W/4);
			bot1.v = IVector.Up;

			lock (Orb.OrbLock) for (int i = 1; i < 12; i++) 
				new Orb(true).pos = new IPoint(W*1/4, W/2*i/13);
		}

		public void Stage3() {
			// introduce blast
			text = "Blasts will pop when eaten\nUse them to pick up any balls\ninside its range";
			Head player = HEADS[key_player];
			player.act = Activities.DEFAULT;
			player.pos = new IPoint(W*.6d, W*.19d);
			player.v = speed * IVector.Left;
			
			
			bot.act = Activities.DEAD;
			bot1.act = Activities.DEAD;
			bot.pos = bot1.pos = new IPoint(0, -HeadR*2);
			bot.v = bot1.v = IVector.Right;
			
			Blast blast = new Blast();
			blast.pos = new IPoint(W*.8f, W*.19d);
			Blast.All.Add(blast);

			new Thread(()=> {
				Thread.CurrentThread.Name = "Blast_Animation";
				int starttick = Tick;
				new Animation(blast.pos, 30, BlastR, BlastRange / 5f, 5*Scale, 10*Scale, Color.White, Color.Yellow, AnimationTypes.SIN);
				SpinWait.SpinUntil(()=> Tick >= starttick + 30 || !ApplicationRunning);
				new Animation(blast.pos, 10, BlastRange / 5f, BlastR, 10*Scale, Scale, Color.Yellow, Color.Yellow, AnimationTypes.COS);
				SpinWait.SpinUntil(()=> Tick >= starttick + 50 || !ApplicationRunning);
				new Animation(blast.pos, 30, BlastR, BlastRange, 5*Scale, 5*Scale, Color.White, Color.White, AnimationTypes.SQRT);
			}).Start();

			List<IPoint> ps = new List<IPoint>() {
					new IPoint(W*.79d, W*.14d),
					new IPoint(W*.7d, W*.24d),
					new IPoint(W*.94d, W*.22d),
					new IPoint(W*.7d, W*.1d),
					new IPoint(W*.87d, W*.32d),
					new IPoint(W*.82d, W*.4d)
				};

			lock (Orb.OrbLock) foreach (IPoint p in ps) new Orb().pos = p;
		}

		public void Stage4() {
			// introduce the orbits
			// use Maps.STANDARD from now on
			text = "When you're inside an orbit,\npress to go in orbit\npress again to go straight again";
			point = new PointF(.367f * W, .4f * W);
			Orbits = Standard;

			Head player = HEADS[key_player];
			player.act = Activities.DEFAULT;
			player.pos = new IPoint(W*.45d, W*.45d);
			player.v = IVector.Up;
			
			bot.act = Activities.DEAD;
			bot1.act = Activities.DEAD;
			bot.pos = bot1.pos = new IPoint(0, -HeadR * 2);
			bot.v = bot1.v = IVector.Right;

			Blast blast = new Blast();
			blast.pos = new IPoint(W*.15d, W*.18d);
			Blast.All.Add(blast);

			List<IPoint> ps = new List<IPoint>() {
					new IPoint(W*.07d, W*.07d),
					new IPoint(W*.93d, W*.43d),
					new IPoint(W*.07d, W*.43d),
					new IPoint(W*.93d, W*.07d)
				};

			lock (Orb.OrbLock) foreach (IPoint p in ps) new Orb().pos = p;
		}

		public void Stage5() {
			text = "Remember,\nyou can only dash\noutside of orbits";
			// combine orbits and the black wall
			Head player = HEADS[key_player];
			player.act = Activities.DEFAULT;
			player.pos = new IPoint(W*.45d, W*.45d);
			player.v = IVector.Up;

			List<IPoint> ps = new List<IPoint>() {
					new IPoint(W*.07d, W*.07d),
					new IPoint(W*.93d, W*.43d),
					new IPoint(W*.07d, W*.43d),
					new IPoint(W*.93d, W*.07d)
				};

			lock (Orb.OrbLock) foreach (IPoint p in ps) new Orb().pos = p;

			bot.act = Activities.DEFAULT;
			bot.INVINCIBLE = true;
			bot.pos = new IPoint(W*.633f, W/16);
			bot.v = IVector.Down;

			bot1.act = Activities.DEAD;
			bot1.pos = new IPoint(0, -HeadR * 2);
			bot1.v = IVector.Right;

			lock (Orb.OrbLock) for (int i = 1; i < 12; i++)
				new Orb(true).pos = new IPoint(W*.633d, W/2*i/13);
		}

		public void Stage6() {
			// introduce shooting - pick up a ball, force the dash, shoot the target - no orbits this time
			Orbits = Empty;
			Orbits.Add(new Orbit(.8f, .38f, .05f));
			text = "And finally:\nShoot to kill\nyour opponents";
			point = new PointF(W/2, W/4);

			Head player = HEADS[key_player];
			player.act = Activities.DEFAULT;
			player.pos = new IPoint(W*.7d, W*.19d);
			player.v = speed * IVector.Left;

			bot.act = Activities.DEFAULT;
			bot.pos = new IPoint(W*1/4, W/4);
			bot.v = IVector.Up;
			bot.INVINCIBLE = true;

			lock (Orb.OrbLock) for (int i = 1; i < 12; i++) 
				new Orb(true).pos = new IPoint(W*1/4, W/2*i/13);

			bot1.color = Color.Green;
			bot1.DisplayKey = "×";
			bot1.pos = new IPoint(.8d*W, .215d*W);
			bot1.act = Activities.ORBITING;
			bot1.orbitCenter = new IPoint(W*.8f, W*.19f);
			
			new Orb(true).pos = new IPoint(W*.1f, W*.19f);
		}

	}
}
