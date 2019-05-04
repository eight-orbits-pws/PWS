namespace Eight_Orbits.Properties {
	public delegate void GameEvent();
	public delegate void PaintEvent(ref System.Windows.Forms.PaintEventArgs e);

	public enum MapNames {
		STANDARD, FIRST, DENSE, SIMPLE, STEPS, ISLES, MOON, RING, WAVE, ME, Length
	}

	public enum BlastSpawn {
		ONE, RARE,
	}

	public enum States {
		PAUSED, INGAME, NEWGAME,
	}

	public enum Activities {
		DEFAULT, STARTROUND, DASHING, ORBITING, DEAD,
	}

	public enum OrbStates {
		SPAWN, WHITE, TRAVELLING, BULLET, OWNER,
	}

	public enum Phases {
		NONE, STARTGAME, STARTROUND, ENDROUND, ENDGAME
	}

	public enum MVPTypes {
		NONE, POINTS, EARLY_KILL, ASSIST, GHOSTKILL, COLLATERAL, ACE, TWO_PTS, WINNER, ACE_WINNER, COLLATERAL_ACE, COLLATERAL_ACE_WINNER, FLAWLESS
	}

	public enum AnimationTypes {
		LINEAR, SQRT, SQUARED, CUBED, SIN, COS
	}

	public enum AnimationState { //not implemented
		PLAY, PAUSE, REVERSE 
	}
}
