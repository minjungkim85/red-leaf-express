﻿using UnityEngine;
using System.Collections;

public class TrainEngine : MonoBehaviour {
	public Rigidbody2D rigidbody;
	float t = 0;
	bool countUp = true;
	public bool isEngine = false;
	public Vector2 impactDir;
	float magnitude = 0;
	public AnimationCurve acceleration;
	public float damage;
	public float maxSpeed = 200;
	public float accelerationRate = 2000;
	float holdTime = 0;
	public GameObject antigrav;
	public float energy = 100f;
	float maxEnergy = 100f;
	public float antiGravCost = 25;
	public float rechargeRate = 1f;
	public SpriteRenderer brakeSprite;
	public AudioClip[] clips;
	AudioSource audioSource;
	public AudioSource trainEngineAudioSource;
	// Use this for initialization
	void Start () {
		
		audioSource = GetComponent<AudioSource> ();
		rigidbody = GetComponent<Rigidbody2D> ();
	}
	
	void Update(){
		if (isEngine == false)
			return;
		if (accelerate) {
			holdTime += Time.deltaTime;

		} else {
			holdTime -= Time.deltaTime;
		}
		holdTime = Mathf.Clamp01 (holdTime);
		if (antigrav && energy >= antiGravCost && Input.GetMouseButtonDown (0)) {
			Vector3 pos = Input.mousePosition;
			if (isEngine){
				int id = Random.Range (4,8);
				audioSource.PlayOneShot(clips[id]);
			}
			pos.y = -4.9f;
			pos.x += 25f * rigidbody.velocity.x;
			pos = Camera.main.ScreenToWorldPoint(pos);
			pos.z = 0;
			Instantiate (antigrav, pos, Quaternion.identity);
			ApplyExplosiveForce(pos);
		}
		energy += Time.deltaTime * rechargeRate;
		energy = Mathf.Min (maxEnergy, energy);


	}
	bool accelerate = false;
	void Accelerate(bool active){
//		if(active)
		Debug.Log ("Setting accelerate to " + active);
		accelerate = active;
	}
	bool brake = false;
	void Brake(bool active){
		if (isEngine){
			int id = 8;
			audioSource.PlayOneShot(clips[id]);
		}
		Debug.Log ("Setting brake to " + active);
		brake = active;
	}
	public void BrakeWarning(){

		brakeSprite.enabled = true;
	}
	void FixedUpdate(){
//		if(countUp)	t += Time.deltaTime;
//		else t -= Time.deltaTime;
//		rigidbody.AddForce (Vector2.Lerp(Vector2.up * 974, Vector2.up * 980, t));
//		if (countUp && t >= 1) countUp = false;
//		else if(countUp == false && t <= 0) countUp = true;

		if(isEngine && accelerate) rigidbody.AddForce(Vector2.right * acceleration.Evaluate(holdTime) * accelerationRate);
		if(rigidbody.velocity.x > maxSpeed) rigidbody.velocity = new Vector2(maxSpeed, rigidbody.velocity.y); 
		if (isEngine && brake) rigidbody.drag += Time.deltaTime;
		else rigidbody.drag -= Time.deltaTime;
		if(rigidbody.drag < 0) rigidbody.drag =0;
		if(rigidbody.drag > 10) rigidbody.drag =10;
		if(isEngine) CheckFront ();
		if (isEngine && rigidbody.velocity.x > 5 && trainEngineAudioSource.isPlaying == false)
			trainEngineAudioSource.Play ();
		else if(isEngine && trainEngineAudioSource.isPlaying == true)
			trainEngineAudioSource.Stop ();
	}

	public bool collided = false;
	void OnCollisionEnter2D(Collision2D collide){
		if(collide.collider.attachedRigidbody){
			collided = true; 
			
			collide.collider.attachedRigidbody.AddForce (impactDir * 4, ForceMode2D.Impulse);
			collide.collider.attachedRigidbody.AddTorque(5, ForceMode2D.Impulse);
			if(collide.collider.GetComponent<Obstacle>() != null){
				float accel = acceleration.Evaluate(holdTime) - acceleration.Evaluate(holdTime - Time.deltaTime);
				Obstacle obstacle = collide.collider.GetComponent<Obstacle>();
				if(obstacle.dealFlatDamage) this.damage += obstacle.damageAmount;
				else this.damage += Mathf.Max(0.5f,obstacle.damageAmount * accel * 0.75f);
				collide.collider.SendMessage("Destruct",true, SendMessageOptions.DontRequireReceiver);
				
				// argh
				if (isEngine){
					int id = Random.Range (0,4);
					audioSource.PlayOneShot(clips[id]);
				}
			}
				
		}
			

	}

	void ApplyExplosiveForce(Vector2 point){

		energy -= antiGravCost;
		Collider2D[] obstacles = Physics2D.OverlapCircleAll(point, 5, 1<<LayerMask.NameToLayer("Obstacle"));
		foreach (Collider2D c in obstacles) {
			c.SendMessage("Lift", SendMessageOptions.DontRequireReceiver);
		}
	}

	public bool collisionIncoming = false;
	public float collisionCheckDist = 40;
	void CheckFront(){
		RaycastHit2D rh = Physics2D.Raycast (transform.position - new Vector3(0,2,0), Vector2.right, Mathf.Max (10, rigidbody.velocity.x * 1.25f), 1 << LayerMask.NameToLayer ("Obstacle"));
		collisionIncoming = rh.collider != null;
		if(rh.rigidbody)rh.rigidbody.isKinematic = false;
	}
}
