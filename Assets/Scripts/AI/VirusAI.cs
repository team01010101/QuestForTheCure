using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public enum VirusAIState {
  Chase,
  Scatter,
  Frightened,
  Dead
}



[RequireComponent (typeof (WaypointWalker))]
public abstract class VirusAI : MonoBehaviour {



  //-----------------------------------------------------------------------------------
  // in scatter mode, ghosts try to reach this node
  //-----------------------------------------------------------------------------------

  public WaypointNode cornerNode;



  //-----------------------------------------------------------------------------------
  // find the virus waypoint walker
  // and robert too
  // and topography
  //-----------------------------------------------------------------------------------

  protected GameManager gameManager;
  protected WaypointWalker walker;
  protected GameObject robert;
  protected PlanetTopography topo;

  protected int modesTableIndex;
  protected List<KeyValuePair<VirusAIState, float>> modesTable;
  protected float frightenedTime;
  protected float liveTime;

  public void Awake() {
    gameManager = GameManager.Instance;
    walker = GetComponent<WaypointWalker>();
    robert = GameObject.FindGameObjectWithTag(Tags.Pacman);
    topo = GameObject.FindGameObjectWithTag(Tags.Planet).GetComponent<PlanetTopography>();

    modesTable = gameManager.virusModesForCurrentLevel();
    resetMode();

    awake();
  }

  // override this method instead of Awake
  public virtual void awake() {}

  // handle AI state 
  public void Update() {
    if (gameManager.paused || !gameManager.inGame) return;

    if (_state == VirusAIState.Frightened) {
      frightenedTime -= Time.deltaTime;
      if (frightenedTime < 0f) {
        frightenedTime = 0f;
        _state = _previousState;
      }
    } else {
      if (_state != VirusAIState.Dead) {
        if (modesTableIndex >= 0 && liveTime >= 0f) {
          liveTime -= Time.deltaTime;
          if (liveTime < 0f) {
            ++modesTableIndex;
            _state = modesTable[modesTableIndex].Key;
            liveTime = modesTable[modesTableIndex].Value;
          }
        }
      }
    }
  }


  //-----------------------------------------------------------------------------------
  // animators can use this to pick a texture
  //-----------------------------------------------------------------------------------

  public void resetMode() {
    frightenedTime = 0f;
    if (modesTable != null && modesTable.Count > 0) {
      modesTableIndex = 0;
      _state = modesTable[modesTableIndex].Key;
      liveTime = modesTable[modesTableIndex].Value;
    } else {
      modesTableIndex = -1;
      _state = VirusAIState.Chase;
      liveTime = -1f;
    }
  }


  //-----------------------------------------------------------------------------------
  // animators can use this to pick a texture
  //-----------------------------------------------------------------------------------

  public double remainingFrightenedTime { get { return frightenedTime; } }


  //-----------------------------------------------------------------------------------
  // whenever ghosts change from chase or scatter to any other mode, 
  // they are forced to reverse direction. note that
  // when the ghosts leave frightened mode, or die, they do not change direction
  //-----------------------------------------------------------------------------------

  public bool forceReverse { get; private set; }



  //-----------------------------------------------------------------------------------
  // call this to make scare virus
  //-----------------------------------------------------------------------------------

  public void scare() {
    if (_state == VirusAIState.Dead) return;
    frightenedTime = gameManager.virusFrightenedTimeForCurrentLevel();
    if (_state != VirusAIState.Frightened) state = VirusAIState.Frightened;
  }


  //-----------------------------------------------------------------------------------
  // virus state determines what a virus will do when they reach an intersection
  //-----------------------------------------------------------------------------------

  private VirusAIState _state;
  private VirusAIState _previousState;
  public VirusAIState state { 
    get { return _state; } 
    set {
      // ghosts are forced to reverse direction by the system anytime the mode changes from: 
      // chase-to-scatter, chase-to-frightened, scatter-to-chase, and scatter-to-frightened 
      // ghosts do not reverse direction when changing back from frightened to chase or scatter modes
      forceReverse = (_state == VirusAIState.Chase && 
          (value == VirusAIState.Scatter || value == VirusAIState.Frightened))
        || (_state == VirusAIState.Scatter && 
          (value == VirusAIState.Chase || value == VirusAIState.Frightened));
      _previousState = _state;
      _state = value;
    }
  }



  //-----------------------------------------------------------------------------------
  // subclasses must implement these methods to provide different behaviours
  //-----------------------------------------------------------------------------------

  protected abstract Direction directionChase();

  protected virtual Direction directionScatter() {
    return directionToTarget(cornerNode.transform.position);
  }

  protected virtual Direction directionFrightened() {
    // ghosts use a pseudo-random number generator (PRNG) 
    // to pick a way to turn at each intersection when frightened
    System.Random rnd = new System.Random();
    Direction direction = (Direction)rnd.Next(4);
    if (validDirection(direction)) return direction;

    // if a wall blocks the chosen direction, 
    // the virus then attempts the remaining directions in this order: 
    // up, left, down, and right, until a passable direction is found
    WaypointNode node = walker.getCurrentNode();
    if (node.getFront() != null) return Direction.Front;
    if (node.getLeft() != null) return Direction.Left;
    if (node.getBack() != null) return Direction.Back;
    if (node.getRight() != null) return Direction.Right;
    return Direction.None;
  }

  protected virtual Direction directionDead() {
    return directionToTarget(walker.spawn.transform.position);
  }



  //-----------------------------------------------------------------------------------
  // virus walkers should invoke this method to get new directions
  //-----------------------------------------------------------------------------------

  public Direction direction() {

    // force direction change is the easy one
    if (forceReverse) {
      forceReverse = false;
      return walker.direction().getOpposite();
    }

    // otherwise let the state determine where to go
    if (state == VirusAIState.Chase) return directionChase();
    if (state == VirusAIState.Scatter) return directionScatter();
    if (state == VirusAIState.Frightened) return directionFrightened();
    if (state == VirusAIState.Dead) return directionDead();

    // should never happen!
    return Direction.None;

  }



  //-----------------------------------------------------------------------------------
  // which directions are allowed now?
  //-----------------------------------------------------------------------------------

  protected List<Direction> allowedDirections() {
    
    // if two or more potential choices are an equal distance from the target, 
    // the decision between them is made in the order of up > left > down (> right)
    List<Direction> directions = new List<Direction>();
    Direction direction = walker.direction();
    WaypointNode node = walker.getCurrentNode();
    if (node.getFront() != null && !direction.isOpposite(Direction.Front)) directions.Add(Direction.Front);
    if (node.getLeft() != null && !direction.isOpposite(Direction.Left)) directions.Add(Direction.Left);
    if (node.getBack() != null && !direction.isOpposite(Direction.Back)) directions.Add(Direction.Back);
    if (node.getRight() != null && !direction.isOpposite(Direction.Right)) directions.Add(Direction.Right);
    return directions;

  }


  //-----------------------------------------------------------------------------------
  // find out if we the virus can move in the specified direction
  //-----------------------------------------------------------------------------------

  protected bool validDirection(Direction direction) {
    WaypointNode node = walker.getCurrentNode();
    if (direction == Direction.Front) return node.getFront() != null;
    if (direction == Direction.Back) return node.getBack() != null;
    if (direction == Direction.Left) return node.getLeft() != null;
    if (direction == Direction.Right) return node.getRight() != null;
    return false;
  }


  //-----------------------------------------------------------------------------------
  // when a decision about which direction to turn is necessary, 
  // the choice is made based on which tile adjoining the intersection 
  // will put the virus nearest to its target tile, measured in a straight line
  //-----------------------------------------------------------------------------------

  protected Direction directionToTarget(Vector3 target) {

    WaypointNode node = walker.getCurrentNode();
    float bestDistance = float.MaxValue;
    Direction bestDirection = Direction.None;

    List<Direction> directions = allowedDirections();
    foreach (Direction direction in directions) {
      
      Vector3 nextNodePosition = node.getAdjacent(direction).transform.position;
      Vector3 directionVector = (nextNodePosition - node.transform.position).normalized;
      Vector3 origin = node.transform.position + directionVector;
      float distance = topo.calculateDistance(origin, target);
      if (bestDistance > distance) {
        bestDistance = distance;
        bestDirection = direction;
      }

    }
    return bestDirection;
  }


}
