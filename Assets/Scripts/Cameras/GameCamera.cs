using UnityEngine;
using System.Collections;



public class GameCamera : MonoBehaviour {


	
  protected GameObject robert;
  protected Canvas canvas;



  public void Awake() {
    robert = GameObject.FindGameObjectWithTag(Tags.Pacman);

    canvas = Instantiate(Resources.Load("Prefabs/GameCanvas")) as Canvas;
    if (canvas != null) canvas.worldCamera = Camera.main;
  }



}
