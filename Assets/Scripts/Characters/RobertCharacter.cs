using UnityEngine;
using System.Collections;


public enum RobertState {
  Idle, 
  Run,
  JumpUp,
  JumpDown,
  Die
}


public class RobertCharacter : Character {
	

  private RobertState lastState = RobertState.Idle;


  
	protected override void setAnimationState() {
    
    if (gameManager.robertData.alive) {

      if (lastState == RobertState.Die) {
        anim.SetInteger("AnimState", 5);
        lastState = RobertState.Idle;
      } else {
        RobertWalker robert = walker as RobertWalker;
        if (robert.grounded()) {
          if (robert.justLanded()) {
            anim.SetInteger("AnimState", 4);
            lastState = RobertState.Idle;
          } else if (robert.moving()) {
            anim.SetInteger("AnimState", 1);
            lastState = RobertState.Run;
          } else {
            anim.SetInteger("AnimState", 0);
            lastState = RobertState.Idle;
          }
        } else {
          if (robert.jumpingUp()) {
            anim.SetInteger("AnimState", 2);
            lastState = RobertState.JumpUp;
          } else if (robert.jumpingDown()) {
            anim.SetInteger("AnimState", 3);
            lastState = RobertState.JumpDown;
          }
        }
      }
    } else {
      if (lastState == RobertState.Idle) anim.SetInteger("AnimState", 8);
      else if (lastState == RobertState.JumpUp) anim.SetInteger("AnimState", 6);
      else if (lastState == RobertState.JumpDown) anim.SetInteger("AnimState", 7);
      else if (lastState == RobertState.Run) anim.SetInteger("AnimState", 9);
      lastState = RobertState.Die;
			GameObject.FindGameObjectWithTag("Pacman").GetComponent<RobertSounds>().robertDeath();
		
    }

	}


}
