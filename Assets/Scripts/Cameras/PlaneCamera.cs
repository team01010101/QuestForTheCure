using UnityEngine;
using System.Collections;



public class PlaneCamera : GameCamera {



  public float cameraHeight = 15f;



  void Update() {

    // find camera position
    Vector3 pacmanPosition = robert.transform.position;
    Vector3 cameraPos = pacmanPosition;
    cameraPos.y = cameraHeight;
    transform.position = cameraPos;

    // have it face down
    transform.LookAt(pacmanPosition);
  }



}
