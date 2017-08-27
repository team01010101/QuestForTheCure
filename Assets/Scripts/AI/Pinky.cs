using UnityEngine;
using System.Collections;

public class Pinky : VirusAI {



  //------------------------------------------------------------------------------------------------
  // we need to be able to access robert's direction
  //------------------------------------------------------------------------------------------------

  WaypointWalker robertWalker;

  public override void awake() {
    robertWalker = robert.GetComponent<WaypointWalker>();
  }



  //------------------------------------------------------------------------------------------------
  // pinky’s target tile in chase mode is determined by looking at robert’s current position and orientation, 
  // and selecting the location four tiles straight ahead of robert. 
  // when robert is facing upwards, an overflow error in the game’s code causes pinky’s target tile 
  // to actually be set as four tiles ahead of robert and four tiles to the left of him (LOL)
  //------------------------------------------------------------------------------------------------

  protected override Direction directionChase() {

    float numTiles = 4f;
    Transform pacmanTransform = robert.transform;
    Vector3 target = pacmanTransform.position;
    Direction pacmanDirection = robertWalker.direction();
    if (pacmanDirection == Direction.Front) {
      target += pacmanTransform.forward.normalized * numTiles;
      target -= pacmanTransform.right.normalized * numTiles;
    }
    if (pacmanDirection == Direction.Back) target -= pacmanTransform.forward.normalized * numTiles;
    if (pacmanDirection == Direction.Left) target -= pacmanTransform.right.normalized * numTiles;
    if (pacmanDirection == Direction.Right) target += pacmanTransform.right.normalized * numTiles;

    return directionToTarget(target);
  }


}
