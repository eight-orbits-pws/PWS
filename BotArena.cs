using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Neural_Network;

using System.IO;

namespace Eight_Orbits
{

    class BotArena : World
    {

        public List<Neat> bots = new List<Neat>();

        public enum Type
        {
            CONTINUEOUS, // When a bot dies it is replaced by a child of its killer
            MUTATIONS, // After each game all bots get mutated
            MAX_POINTS // Play an entire game, the bots with the least points get replaced by children of the bots with the most points
        }

        public Type type;

        public long generation = 0;

        public BotArena(Type type)
        {
            this.type = type;
        }

        public BotArena(int bots, Type type)
        {
            for (int i = 0; i < bots; i++)
            {
                Neat bot = new Neat();
                bot.SetupGenZero();
                this.bots.Add(bot);
            }

            this.type = type;
        }

        public byte[] GetGeneration()
        {
            return BitConverter.GetBytes(generation);
        }

        public void SetGeneration(List<byte> list)
        {
            generation = BitConverter.ToInt32(Neat.remove(list, 4), 0);
        }

        public void AddBots(bool singleRound)
        {
            foreach (Neat bot in bots)
            {
                bot.AddKey();
                IKey.UpdateAll();

                if (singleRound)
                    MaxPoints = 0;
                else
                    SetMaxPoints();

                Program.HEADS[bot.Key].color = bot.color.GetValueOrDefault(Entities.Head.GenerateColor());
            }

			if (type == Type.CONTINUEOUS) {
				/// in plaats van de orbs laten verdwijnen:
				MaxOrbs = 6;
				
				/// kan ook zo: 
				foreach (Entities.Head head in Program.HEADS.Values)
					head.OnDie += OnDie;
			}
        }

        public void OnDie()
        {
            lock (Program.ActiveLock)
            {
                for (int i = Program.InactiveKeys.Count - 1; i >= 0; i--)
                {
                    Keys key = Program.InactiveKeys[i];
                    Keys killer = 0;

                    foreach (KeyValuePair<Keys, Entities.Head> pair in Program.HEADS)
                    {
                        if (pair.Value.killed.Remove(key))
                        {
                            killer = pair.Key;
                            break;
                        }
                    }
                    if (killer == 0)
                        return;

                    FromParent(key, killer);

                    Entities.Head head = Program.HEADS[key];

                    head.Reset();
                    head.clear();

                    Program.InactiveKeys.Remove(key);
                    Program.ActiveKeys.Add(key);

                    head.Revive();
                    head.v.A = Program.R.NextDouble() * Math.PI * 2;
                    head.act = Properties.Activities.DASHING;
                }
            }
        }

        public void FromParent(Keys a, Keys b)
        {
            Neat child = null;
            Neat parent = null;
            
            foreach (Neat n in bots)
            {
                if (n.Key == a)
                    child = n;
                if (n.Key == b)
                    parent = n;
                if (child != null && parent != null)
                    break;
            }

            child.FromParents(parent, null);

            // Print child
            //Console.WriteLine("---------------------------");
            //Console.WriteLine(a);
            foreach (Gene gene in child.Genes)
                Console.WriteLine(gene);
        }

        public override void StartGame()
        {
            PrintBots();
            base.StartGame();
        }

        public override void EndGame()
        {
            base.EndGame();

            foreach (Neat bot in bots)
                bot.ResetNeurons();

            switch (type)
            {
                case Type.MUTATIONS:
                    foreach (Neat bot in bots)
                        bot.Mutate();
                    break;
                case Type.MAX_POINTS:
                    bots.Sort((a, b) => Program.HEADS[b.Key].Points.CompareTo(Program.HEADS[a.Key].Points));

                    //int mark = (bots.Count + 1) / 2;
                    //for (int i = mark; i < bots.Count; i++)
                    //    bots[i].FromParent(bots[i - mark]);

					for (int i = 2; i < bots.Count; i++)
                        if (Program.R.NextDouble() > (double) Program.HEADS[bots[i].Key].Points / MaxPoints) bots[i].FromParents(bots[i % 2], bots[- ((i % 2) - 1)]); // 0 or 1
                    break;
            }

            generation += 1;
            if (true)
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(GetGeneration());
                foreach (Neat bot in bots)
                    bytes.AddRange(Neat.compile(bot));

                Directory.CreateDirectory("backup");
				if (File.Exists($"backup/gen{generation-25}.bot")) File.Delete($"backup/gen{generation-25}.bot");
                File.WriteAllBytes($"backup/gen{generation}.bot", bytes.ToArray());
            }
        }

        private void PrintBots()
        {
			return;
            foreach (Neat bot in bots)
            {
                Console.WriteLine("---------------------------");
                Console.WriteLine(bot.Key);
                foreach (Gene gene in bot.Genes)
                    Console.WriteLine(gene);
            }
        }

    }

}
