﻿using UnityEngine;
using System.Collections;



public class SphereCamera : GameCamera {



  public float cameraHeight = 20f;

	

	void Update() {
    // set camera position
    Vector3 pacmanUp = robert.transform.position.normalized;
    Vector3 cameraPos = pacmanUp * cameraHeight;
    transform.position = cameraPos;
    // have it face down
    transform.LookAt(Vector3.zero);
	}



}
