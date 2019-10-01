﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using Neural_Network;

namespace Eight_Orbits
{

    class BotArena : World
    {

        List<Neat> bots = new List<Neat>();

        public enum Type
        {
            CONTINUEOUS, // When a bot dies it is replaced by a child of its killer
            MUTATIONS, // After each game all bots get mutated
            MAX_POINTS // Play an entire game, the bots with the least points get replaced by children of the bots with the most points
        }

        public Type type;

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

                Program.HEADS[bot.Key].color = Entities.Head.generate_color();
            }

			if (type == Type.CONTINUEOUS) {
				/// in plaats van de orbs laten verdwijnen:
				MaxOrbs = 6;
				
				/// kan ook zo: foreach (Head head in Program.HEADS.Values)
				foreach (KeyValuePair<Keys, Entities.Head> head in Program.HEADS)
					head.Value.OnDie += OnDie;
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

            child.FromParent(parent);

            // Print child
            Console.WriteLine("---------------------------");
            Console.WriteLine(a);
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

                    int mark = (bots.Count + 1) / 2;
                    for (int i = mark; i < bots.Count; i++)
                        bots[i].FromParent(bots[i - mark]);

                    break;
            }
        }

        private void PrintBots()
        {
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
