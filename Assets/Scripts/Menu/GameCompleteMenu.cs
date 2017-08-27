using UnityEngine;
using System.Collections;

public class GameCompleteMenu : MonoBehaviour {


  private bool buttonPressable = true;
	

  public void advance() {
    if (buttonPressable) {
      buttonPressable = false;
      GameManager gameManager = GameManager.Instance;
      gameManager.transitionToScene(
        HiScoreManager.Instance.isHiScore(gameManager.robertData.score) ?
        Tags.HiScoreScene :
        Tags.MenuScene
      );
    }
  }


}
