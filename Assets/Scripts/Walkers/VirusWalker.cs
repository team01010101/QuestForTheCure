using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class VirusWalker : WaypointWalker {



  //----------------------------------------------------------------------------------------------------------
  // default implementation for 'awake' and 'start' state initialization is provided here
  // AI controls the virus's state
  //----------------------------------------------------------------------------------------------------------

  protected VirusAI ai;

  public override void awake() {
    gameManager.registerVirus(gameObject.name, gameObject);
    ai = GetComponent<VirusAI>();
  }

  public override void startState() {
    ai.resetMode();
  }


  //----------------------------------------------------------------------------------------------------------
  // empty input method
  // decisions should be taken only at intersections, ie. in 'updateTurn'
  //----------------------------------------------------------------------------------------------------------

  public void OnCollisionEnter(Collision collision) {
    if (collision.gameObject.tag == Tags.Pacman && gameManager.robertData.alive) {
      if (ai.state != VirusAIState.Dead) {
        // who eats who depends on the virus's state
        if (ai.state == VirusAIState.Frightened) gameManager.killVirus(gameObject.name);
        else gameManager.killRobert();
      }
    }
  }


  //----------------------------------------------------------------------------------------------------------
  // empty input method
  // decisions should be taken only at intersections, ie. in 'updateTurn'
  //----------------------------------------------------------------------------------------------------------

  public override void updateInput() {}



  //----------------------------------------------------------------------------------------------------------
  // animation states (transitions, to be more precise)
  //----------------------------------------------------------------------------------------------------------

  public bool chase() { return ai.state == VirusAIState.Chase; }
  public bool scatter() { return ai.state == VirusAIState.Scatter; }
  public bool frightened() { return ai.state == VirusAIState.Frightened; }
  public bool dead() { return ai.state == VirusAIState.Dead; }



  //------------------------------------------------------------------------
  // update callbacks
  //------------------------------------------------------------------------

  public override void updateDirection() {
    // turn when state update forced a direction change
    // or when the virus reaches an itersection
    if (ai.forceReverse || atNextNode()) processDirection();
  }

  public override void updateMove() {
    // virus moves forward
    float speedMultiplier = walkSpeed * gameManager.virusSpeedMultiplier(ai.state);
    moveAmount = Vector3.forward * speedMultiplier;
  }

  public override void update() {
    // where's the virus facing?
    Debug.DrawLine(transform.position, transform.position + transform.forward.normalized * 4, Color.red);
  }


  //------------------------------------------------------------------------
  // handle turns
  //------------------------------------------------------------------------

	override protected void processDirection() {

    // have the AI tell us where to
    currentNode = nextNode;
    currentDirection = ai.direction();
    if (currentDirection == Direction.Front) nextNode = currentNode.getFront();
    if (currentDirection == Direction.Back) nextNode = currentNode.getBack();
    if (currentDirection == Direction.Left) nextNode = currentNode.getLeft();
    if (currentDirection == Direction.Right) nextNode = currentNode.getRight();

    // update next node plane and rotation
    topo.updatePlane(currentNode.transform.position, nextNode.transform.position, ref nextNodePlane);
    topo.updateRotation(transform, nextNode.transform.position);

    // bring back to life?
    if (ai.state == VirusAIState.Dead && currentNode == spawn) gameManager.reviveVirus(gameObject.name);
  }


}
