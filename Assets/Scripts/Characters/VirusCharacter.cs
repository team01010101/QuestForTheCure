using UnityEngine;
using System.Collections;



public class VirusCharacter : Character {



  private Renderer bodyRenderer;
  private Material chaseMaterial;
  private Material frightenedMaterial;
  private int materialIndex;
  private VirusAI ai;
  private VirusAIState lastState;



  public void Start() {
    GameObject ghostBody = transform.Find(Tags.GhostBody).gameObject;
    bodyRenderer = ghostBody.GetComponent<Renderer>();
    chaseMaterial = bodyRenderer.materials[0];
    frightenedMaterial = bodyRenderer.materials[1];
    materialIndex = 0;
    ai = GetComponent<VirusAI>();
    lastState = VirusAIState.Chase;
  }


  private void setMaterial(int index) {
    materialIndex = index;
    bodyRenderer.enabled = true;
    bodyRenderer.material = materialIndex == 0 ? chaseMaterial : frightenedMaterial;
  }

  protected override void setAnimationState() {
    VirusWalker virus = walker as VirusWalker;
    if (virus.chase()) {
      if (lastState == VirusAIState.Frightened || lastState == VirusAIState.Dead) setMaterial(0);
      lastState = VirusAIState.Chase;
    }
    if (virus.scatter()) {
      if (lastState == VirusAIState.Frightened || lastState == VirusAIState.Dead) setMaterial(0);
      lastState = VirusAIState.Scatter;
    }
    if (virus.frightened()) {
      if (lastState != VirusAIState.Frightened) setMaterial(1);
      else {
        // 5 flashes
        double t = ai.remainingFrightenedTime;
        if (t <= 1.5) setMaterial((int)(t / 0.15) % 2 == 0 ? 1 : 0);
      }
      lastState = VirusAIState.Frightened;
    }
    if (virus.dead()) {
			if (lastState != VirusAIState.Dead) {
				bodyRenderer.enabled = false;
				GameObject.FindGameObjectWithTag ("Pacman").GetComponent<RobertSounds> ().virusEaten ();
			}
			lastState = VirusAIState.Dead;

    }
  }



}
