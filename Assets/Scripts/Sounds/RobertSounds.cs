using UnityEngine;
using System.Collections;


[RequireComponent (typeof (AudioSource))]
public class RobertSounds : MonoBehaviour {

	private AudioSource player;
	public AudioClip chomp;
	public AudioClip death;
	public AudioClip virus;
	public AudioClip win;
	public AudioClip steps;

	private RobertWalker walker;
	private float lastTimeEaten;
	// Use this for initialization
	void Start () {
		lastTimeEaten = 0;
		player = GetComponent<AudioSource> ();
		player.loop = false;
		walker = GetComponent<RobertWalker> ();
	}

	void Update() {
		if (GameManager.Instance.paused) {
			player.Stop ();
		} else if (player.clip == chomp && Time.time - lastTimeEaten > 0.3f) {
			player.loop = false;
		}
		if (!player.isPlaying && GameManager.Instance.robertData.alive) {
			player.clip = steps;
			player.Play ();
		}
		if (player.isPlaying && player.clip == steps && (walker.direction() == Direction.None || walker.jumpingUp() || walker.jumpingDown())) {
			player.Stop ();
		}
	}
	
	// Update is called once per frame
	public void pelletEaten () {
		lastTimeEaten = Time.time;
		if (!player.isPlaying || player.clip == steps) {
			player.clip = chomp;
			player.Play ();
			player.loop = true;
		}
	}

	public void robertDeath () {
		if (player.clip != death) {
			player.clip = death;
			player.Play ();
			player.loop = false;
		}
	}

	public void virusEaten () {
			player.clip = virus;
			player.Play ();
			player.loop = false;
	}

	public void levelDone() {
		if (player.clip != win) {
			player.clip = win;
			player.Play ();
			player.loop = false;
			GameManager.Instance.levelManager.playing = false;
		}
	}
}
