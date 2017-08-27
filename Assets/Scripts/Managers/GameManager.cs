using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;



public class GameManager : MonoBehaviour {


  //-------------------------------------------------------------------
  // game manager singleton
  //-------------------------------------------------------------------

  private static GameManager _instance;

  public static GameManager Instance { 
    get {
      if (_instance == null) {
        GameObject obj = new GameObject("GameManager");
        DontDestroyOnLoad(obj);
        obj.AddComponent<GameManager>();
      }
      return _instance;
    }
  }



  //-------------------------------------------------------------------
  // init everything here
  //-------------------------------------------------------------------

  public void Awake() {
    _instance = this;
    _paused = false;
    _inGame = false;
    _gameOver = true;
    _virusScoreMultiplier = 1;
    _isPlayableLevel = false;

    // register scene changed callback:
    // when the scene is considered 'loaded', most of the shit in it isn't
    // so we need to use the 'active' callback instead
    SceneManager.activeSceneChanged += activeSceneChanged;

    resetRobertData();        // 
    resetLevelData();         // should happen on every level load
  }



  //-------------------------------------------------------------------
  // stores the player's data
  //-------------------------------------------------------------------

  private RobertData _robertData = new RobertData();
  public RobertData robertData { get { return _robertData; } }

  private void resetRobertData() {
    _robertData.alive = true;
    _robertData.lives = 3;
    _robertData.score = 0;
    _robertData.bombs = 0;
  }



  //-------------------------------------------------------------------
  // stores the level's data
  //-------------------------------------------------------------------

  private LevelData _levelData = new LevelData();
  public LevelData levelData { get { return _levelData; } }

  private void resetLevelData() {
    _levelData.pelletsEaten = 0;
    _levelData.pelletsTotal = 0;

    _isPlayableLevel = false;
    _levelManager = null;
  }



  //-------------------------------------------------------------------
  // conveniency methods to do stuff later
  //-------------------------------------------------------------------

  public void callLater(UnityAction function, float seconds) {
    StartCoroutine(callLaterInternal(function, seconds));
  }

  public void callLaterRealtime(UnityAction function, float seconds) {
    StartCoroutine(callLaterRealtimeInternal(function, seconds));
  }

  private IEnumerator callLaterInternal(UnityAction function, float seconds) {
    yield return new WaitForSeconds(seconds);
    function();
  }

  private IEnumerator callLaterRealtimeInternal(UnityAction function, float seconds) {
    yield return new WaitForSecondsRealtime(seconds);
    function();
  }



  //-------------------------------------------------------------------
  // robert and ghosts game objects
  //-------------------------------------------------------------------

  private GameObject _robertGO;
  private Dictionary<string, GameObject> _virusesGOs = new Dictionary<string, GameObject>();


  public void registerRobert(GameObject obj) {
    _robertGO = obj;
  }

  public void registerVirus(string ghostName, GameObject obj) {
    _virusesGOs[ghostName] = obj;
  }


  //-------------------------------------------------------------------
  // activate/deactivate colliders
  //-------------------------------------------------------------------

  private void robertVirusesIgnoreCollision(bool ignore) {
    Collider pacmanCollider = _robertGO.GetComponent<Collider>();
    foreach (KeyValuePair<string, GameObject> ghostEntry in _virusesGOs) 
      Physics.IgnoreCollision(ghostEntry.Value.GetComponent<Collider>(), pacmanCollider, ignore);
  }




  //-------------------------------------------------------------------
  // kill robert
  //-------------------------------------------------------------------

  public void killRobert() {
    if (_robertGO == null) return;

    robertVirusesIgnoreCollision(true);

    // needs to: play sfx
    //           play robert dead animation

    _robertData.alive = false;
    _robertData.lives -= 1;

    if (_robertData.lives <= 0) doGameOver();
    else fadeInAndOut(
      delegate {
        // on fade out complete:
        // halt walkers 
        // if in the middle of a game, wait for user input to restart
        _inGame = false;
        if (_isPlayableLevel) _levelManager.waitForInput();

        // send everyone to their spawns (do a hard reset by calling the start method)
        foreach (KeyValuePair<string, GameObject> ghostEntry in _virusesGOs) 
          ghostEntry.Value.GetComponent<WaypointWalker>().Start();
        _robertGO.GetComponent<WaypointWalker>().Start();

        // bring robert back to life
        reviveRobert();
      },
      delegate {
        // do nothing!
      });
  }

  public void reviveRobert() {
    _robertData.alive = true;
    if (_robertGO == null) return;
    robertVirusesIgnoreCollision(false);
  }


  //-------------------------------------------------------------------
  // kill a virus
  //-------------------------------------------------------------------

  private ulong _virusScoreMultiplier;

  public void killVirus(string ghostName) {
    GameObject virusGo = _virusesGOs[ghostName];
    if (virusGo == null) return;

    // kill virus
    VirusAI ai = virusGo.GetComponent<VirusAI>();
    if (ai.state == VirusAIState.Dead) return;

    ai.state = VirusAIState.Dead;

    // deactivate collisions
    Collider ghostCollider = virusGo.GetComponent<Collider>();
    Collider pacmanCollider = _robertGO.GetComponent<Collider>();
    Physics.IgnoreCollision(ghostCollider, pacmanCollider, true);

    // give points to robert
    if (_robertGO.GetComponent<RobertAI>().powerTime <= 0f) _virusScoreMultiplier = 1;
    _robertData.score += Score.Virus * _virusScoreMultiplier;
    _virusScoreMultiplier *= Score.VirusEatenMultiplierFactor;
  }

  public void reviveVirus(string ghostName) {
    GameObject virusGO = _virusesGOs[ghostName];
    if (virusGO == null) return;

    // reactivate collisions
    Collider ghostCollider = virusGO.GetComponent<Collider>();
    Collider pacmanCollider = _robertGO.GetComponent<Collider>();
    Physics.IgnoreCollision(ghostCollider, pacmanCollider, false);

    // reset AI state? 
    virusGO.GetComponent<VirusAI>().state = VirusAIState.Scatter;
  }


  //-------------------------------------------------------------------
  // virus mode table
  //-------------------------------------------------------------------

  // Mode     Level 1     Levels 2-4     Levels 5+
  // ----------------------------------------------
  // Scatter  7           7              5
  // Chase    20          20             20
  // Scatter  7           7              5
  // Chase    20          20             20
  // Scatter  5           5              5
  // Chase    20          1033           1037
  // Scatter  5           1/60           1/60
  // Chase    indefinite  indefinite     indefinite

  public List<KeyValuePair<VirusAIState, float>> virusModesForCurrentLevel() {
    if (_currentScene == Tags.Scene01) {
      List<KeyValuePair<VirusAIState, float>> ghostModesLevel1 = new List<KeyValuePair<VirusAIState, float>>();
      ghostModesLevel1.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 7f));
      ghostModesLevel1.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, 20f));
      ghostModesLevel1.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 7f));
      ghostModesLevel1.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, 20f));
      ghostModesLevel1.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 5f));
      ghostModesLevel1.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, 20f));
      ghostModesLevel1.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 5f));
      ghostModesLevel1.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, -1f));
      return ghostModesLevel1;
    } else if (_currentScene == Tags.Scene02) {
      List<KeyValuePair<VirusAIState, float>> ghostModesLevel2To4 = new List<KeyValuePair<VirusAIState, float>>();
      ghostModesLevel2To4.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 7f));
      ghostModesLevel2To4.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, 20f));
      ghostModesLevel2To4.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 7f));
      ghostModesLevel2To4.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, 20f));
      ghostModesLevel2To4.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 5f));
      ghostModesLevel2To4.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, 1033f));
      ghostModesLevel2To4.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 1f / 60f));
      ghostModesLevel2To4.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, -1f));
      return ghostModesLevel2To4;
    } else if (_currentScene == Tags.Scene04 || _currentScene == Tags.Scene05) {
      List<KeyValuePair<VirusAIState, float>> ghostModesLevel5Plus = new List<KeyValuePair<VirusAIState, float>>();
      ghostModesLevel5Plus.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 5f));
      ghostModesLevel5Plus.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, 20f));
      ghostModesLevel5Plus.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 5f));
      ghostModesLevel5Plus.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, 20f));
      ghostModesLevel5Plus.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 5f));
      ghostModesLevel5Plus.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, 1037f));
      ghostModesLevel5Plus.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Scatter, 1f / 60f));
      ghostModesLevel5Plus.Add(new KeyValuePair<VirusAIState, float>(VirusAIState.Chase, -1f));
      return ghostModesLevel5Plus;
    }
    return null;
  }

  public float virusFrightenedTimeForCurrentLevel() {
    if (_currentScene == Tags.Scene01) return 5f;
    else if (_currentScene == Tags.Scene02) return 5f;
    else if (_currentScene == Tags.Scene04 || _currentScene == Tags.Scene05) return 3f;
    return 5f;
  }


  //-------------------------------------------------------------------
  // character speed multipliers
  //-------------------------------------------------------------------

  public float robertSpeedMultiplier(float powerTime) {
    float multiplier = 1f;
    if (_currentScene == Tags.Scene01) multiplier = .8f;
    else if (_currentScene == Tags.Scene02) multiplier = .9f;
    else if (_currentScene == Tags.Scene04 || _currentScene == Tags.Scene05) multiplier = 1f;
    if (powerTime > 0f) multiplier = Mathf.Clamp01(multiplier + .5f); 
    return multiplier;
  }

  public float virusSpeedMultiplier(VirusAIState virusState) {
    if (virusState != VirusAIState.Dead) {
      if (_currentScene == Tags.Scene01) return virusState == VirusAIState.Frightened ? .4f : .75f;
      if (_currentScene == Tags.Scene02) return virusState == VirusAIState.Frightened ? .45f : .85f;
      if (_currentScene == Tags.Scene04 || _currentScene == Tags.Scene05) return virusState == VirusAIState.Frightened ? .5f : .95f;
    }
    return 1f;
  }



  //-------------------------------------------------------------------
  // if the scene is a playable level, we can access its level manager
  //-------------------------------------------------------------------

  private bool _isPlayableLevel;
  private LevelManager _levelManager;

  public LevelManager levelManager { get { return _levelManager; } }

  public void registerPlayableLevel(LevelManager levelManager) {
    _isPlayableLevel = true;
    _levelManager = levelManager;
  }


  //-------------------------------------------------------------------
  // can we play?
  //-------------------------------------------------------------------

  private bool _inGame;
  public bool inGame { 
    get { return _inGame; } 
    set { _inGame = value; }
  }
    

  //-------------------------------------------------------------------
  // is this game over? (ie. did robert lose all his lives?)
  //-------------------------------------------------------------------

  private bool _gameOver;

  public bool gameOver {
    get { return _gameOver; }
  }


  //-------------------------------------------------------------------
  // call these to finish the game
  //-------------------------------------------------------------------

  public void doGameComplete() {
    transitionToScene(Tags.GameCompleteScene);
    // after game complete scene, must check for hi score!
  }

  public void doGameOver() {
    _gameOver = true;
    if (HiScoreManager.Instance.isHiScore(_robertData.score)) transitionToScene(Tags.HiScoreScene);
    else transitionToScene(Tags.GameOverScene);
  }



  //-------------------------------------------------------------------
  // call this to start a game
  //-------------------------------------------------------------------

  private void initGame() {
    resetRobertData();        // 
    resetLevelData();         // should happen on every level load
    _gameOver = false;
  }

  public void doStartGame() {
    initGame();
    transitionToScene(Tags.Scene01);
  }

  public void doContinueGame() {
    initGame();
    transitionToScene(_lastPlayedLevel);
  }


  //-------------------------------------------------------------------
  // power pellet mode activation/deactivation
  //-------------------------------------------------------------------

  public void powerPelletEaten() {
    _robertGO.GetComponent<RobertAI>().powerTime = virusFrightenedTimeForCurrentLevel();

    // scaredy ghosts!
    foreach (KeyValuePair<string, GameObject> ghostEntry in _virusesGOs) 
      ghostEntry.Value.GetComponent<VirusAI>().scare();
  }    


  //-------------------------------------------------------------------
  // fade in-out conveniency method
  //-------------------------------------------------------------------

  private void fadeInAndOut(UnityAction onFadeOut, UnityAction onFadeIn) {
    GameObject obj = new GameObject();
    obj.AddComponent<Fader>();
    Fader fader = obj.GetComponent<Fader>();
    fader.onFadeOut = onFadeOut;
    fader.onFadeIn = onFadeIn;
    fader.start = true;
  }


  //-------------------------------------------------------------------
  // fade in/out between scenes
  //-------------------------------------------------------------------

  private string _lastPlayedLevel;

  private string _currentScene;
  public string currentScene { get { return _currentScene; } }


  public void transitionToScene(string sceneName) {
    GameObject pacmanGO = GameObject.FindGameObjectWithTag("Pacman");
    if (pacmanGO) pacmanGO.GetComponent<RobertSounds>().levelDone();

    _inGame = false;
    if (_isPlayableLevel) {
      // halt walkers
      foreach (KeyValuePair<string, GameObject> ghostEntry in _virusesGOs) 
        ghostEntry.Value.GetComponent<WaypointWalker>().halt();
      _robertGO.GetComponent<WaypointWalker>().halt();
    }

    fadeInAndOut(
      delegate {

        // reset leveldata
        resetLevelData();

        // load another scene
        if (_currentScene != null && _currentScene.StartsWith(Tags.LevelPrefix)) _lastPlayedLevel = _currentScene;
        _currentScene = sceneName;
        SceneManager.LoadScene(_currentScene);

      }, 
      delegate {

      });
  }

  // we need to manually set a timeout for characters to start moving in the scene
  private void activeSceneChanged(Scene old, Scene justActivated) {
    //bool newInGame = _currentScene != Tags.MenuScene;
    //if (newInGame) callLater(delegate {
        // we can play as long as we aren't in the main menu
    //_inGame = newInGame;
    //}, 2f);
    //else _inGame = newInGame;

  }



  //-------------------------------------------------------------------
  // pause by setting timeScale
  //-------------------------------------------------------------------

  private bool _paused;
  public bool paused {
    get { return _paused; }
    set {
      _paused = value;
      if (_paused) Time.timeScale = 0f;
      else Time.timeScale = 1f;
    }
  }



}
