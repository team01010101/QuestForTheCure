using UnityEngine;
using System.Collections;

public class Clyde : VirusAI {



  // if clyde is farther than eight tiles away, his targeting is identical to blinky’s, 
  // however, as soon as his distance to robert becomes less than eight tiles, 
  // clyde’s target is set to the same tile as his fixed one in scatter mode
  protected override Direction directionChase() {

    float numTiles = 8f;
    float distanceToPacman = (robert.transform.position - transform.position).magnitude;
    if (distanceToPacman < numTiles) return directionScatter();
    return directionToTarget(robert.transform.position);

  }


}
