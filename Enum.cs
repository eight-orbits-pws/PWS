using System.Threading.Tasks;

namespace Eight_Orbits.Properties {
	public delegate Task AsyncEvent();
	public delegate void PaintEvent(System.Drawing.Graphics g);
	delegate void KillEvent(Entities.Head killer, Entities.Head victim);

	public enum MapNames {
		STANDARD, FIRST, DENSE, SIMPLE, STEPS, ISLES, MOON, RING, WAVE, ME, Length
	}

	public enum BlastSpawn {
		ONE, RARE, NONE
	}

	public enum States {
		PAUSED, INGAME, NEWGAME
	}

	public enum Activities {
		DEFAULT, STARTROUND, DASHING, ORBITING, DEAD
	}

	public enum OrbStates {
		SPAWN, WHITE, TRAVELLING, BULLET, OWNER
	}

	public enum Phases {
		NONE, STARTGAME, STARTROUND, ENDROUND, ENDGAME
	}

	public enum MVPTypes {
		NONE, POINTS, EARLY_KILL, ASSIST, COLLATERAL, GHOSTKILL, ACE, TWO_PTS, WINNER, ACE_WINNER, COLLATERAL_ACE, COLLATERAL_ACE_WINNER, FLAWLESS
	}

	public enum Gamemodes {
		CLASSIC, CHAOS_RED, CHAOS_RAINBOW, KING_OF_THE_HILL, YEET_MODE, HIDDEN
	}

	public enum AnimationTypes {
		LINEAR, SQRT, SQUARED, CUBED, SIN, COS
	}
}
