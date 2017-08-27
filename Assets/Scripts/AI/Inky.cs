using UnityEngine;
using System.Collections;

public class Inky : VirusAI {



  //------------------------------------------------------------------------------------------------
  // we need to be able to access robert's direction and blinky's position
  //------------------------------------------------------------------------------------------------

  WaypointWalker robertWalker;
  private GameObject blinky;


  public override void awake() {
    robertWalker = robert.GetComponent<WaypointWalker>();
    blinky = Object.FindObjectOfType<Blinky>().gameObject;
  }



  //------------------------------------------------------------------------------------------------
  // inky actually uses both robert’s position/facing as well as blinky’s (the red virus’s). 
  // to locate inky’s target, we first start by selecting 
  // the position two tiles in front of robert in his current direction of travel
  // from there, imagine drawing a vector from blinky’s position to this tile, 
  // and then doubling the length of the vector. 
  // The tile that this new, extended vector ends on will be inky’s actual target.
  //------------------------------------------------------------------------------------------------

  protected override Direction directionChase() {

    float numTiles = 2f;
    Transform pacmanTransform = robert.transform;
    Vector3 pacmanTarget = pacmanTransform.position;
    Direction pacmanDirection = robertWalker.direction();
    if (pacmanDirection == Direction.Front) {
      pacmanTarget += pacmanTransform.forward.normalized * numTiles;
      pacmanTarget -= pacmanTransform.right.normalized * numTiles;
    }
    if (pacmanDirection == Direction.Back) pacmanTarget -= pacmanTransform.forward.normalized * numTiles;
    if (pacmanDirection == Direction.Left) pacmanTarget -= pacmanTransform.right.normalized * numTiles;
    if (pacmanDirection == Direction.Right) pacmanTarget += pacmanTransform.right.normalized * numTiles;

    Vector3 blinkyPosition = blinky.transform.position;
    Vector3 target = blinkyPosition + (pacmanTarget - blinkyPosition) * 2f;
    return directionToTarget(target);
  }


}
