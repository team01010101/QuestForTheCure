using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Blinky : VirusAI {
  
  protected override Direction directionChase() {
    return directionToTarget(robert.transform.position);
  }

}
