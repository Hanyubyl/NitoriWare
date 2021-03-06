﻿using UnityEngine;
using System.Collections;

public class GhostFoodYuyuko : MonoBehaviour
{

	public int chewsNeeded;

	public bool canEatMultipleFoods;
	public float chewTime, motorSpeedMult;

	public Transform[] foods;

	public Transform face, body;
	public HingeJoint2D joint;
	public ParticleSystem particles, burpParticles;
	public Animator animator;
	public Vibrate headVibrate;

	public Sprite hungryFace, chewingFace1, chewingFace2, happyFace;

	private Vector3 lastMousePosition;

	public float initialScale, victoryScale, victoryMoveSpeedMult;
	private float distanceToCenter;
	

	private SpriteRenderer faceRenderer;
	private int chewsLeft;

	public AudioSource audioSource;
	public AudioClip chewClip, victoryClip, burpClip;

	public State state;
	public enum State
	{
		Hungry,
		Chewing,
		Full
	}

	void Awake()
	{
		faceRenderer = face.GetComponent<SpriteRenderer>();
	}

	void Start()
	{
		reset();
	}


	bool isInCenter(Vector3 position)
	{
		return Mathf.Abs(position.x) < 2.5f && Mathf.Abs(position.y) < 1.5f;
	}

	public void reset()
	{
		Cursor.visible = false;
		lastMousePosition = getMousePosition();
		updateMovement();
		state = State.Hungry;

		for (int i = 0; i < foods.Length; foods[i++].gameObject.SetActive(true))
		{
			do 
			{
				foods[i].transform.position = new Vector3(Random.Range(-5f, 5f), Random.Range(-4f, 4f), transform.position.z);
			}
			while (isInCenter(foods[i].transform.position) || (foods[i].transform.position - transform.position).magnitude < 4f);
			
		}
		setFacingRight(transform.position.x < 0f);
		body.transform.rotation = transform.position.x < 0f ? Quaternion.Euler(0f, 0f, -0.5f * Mathf.Rad2Deg) : Quaternion.Euler(0f, 0f, 0.5f * Mathf.Rad2Deg);
		animator.SetTime(1f);
		animator.enabled = false;

		headVibrate.vibrateOn = false;
		setScale(initialScale);
		face.transform.parent.localScale = Vector3.one;
		CancelInvoke();
		chewsLeft = 0;

		particles.Stop();
		particles.SetParticles(new ParticleSystem.Particle[0], 0);
		burpParticles.Stop();
		burpParticles.SetParticles(new ParticleSystem.Particle[0], 0);

		audioSource.Stop();
		
	}
	
	bool isFoodLeft()
	{
		for (int i = 0; i < foods.Length; i++)
		{
			if (foods[i].gameObject.activeInHierarchy)
				return true;
		}
		return false;
	}
	
	void Update ()
	{
		if (state != State.Full)
			updateMovement();

		updateFace();
		audioSource.panStereo = state == State.Full ? 0f : AudioHelper.getAudioPan(Camera.main, transform.position) * .8f ;
	}

	void checkForVictory()
	{
		if (!isFoodLeft())
			victory();
		else
		{

			burpParticles.Stop();
			burpParticles.Play();
			audioSource.PlayOneShot(burpClip);

			animator.Rebind();
			animator.enabled = false;
			state = State.Hungry;
			face.transform.parent.localScale = Vector3.one;
		}
	}

	void victory()
	{
		MicrogameController.instance.setVictory(true, true);
		state = State.Full;
		animator.enabled = false;
		face.transform.parent.localScale = Vector3.one;
		joint.useMotor = false;
		headVibrate.vibrateOn = true;
		distanceToCenter = ((Vector2)transform.position).magnitude;

		audioSource.PlayOneShot(victoryClip);
		audioSource.pitch = Time.timeScale;
	}

	void spitParticles()
	{
		particles.Stop();
		particles.Play();
	}


	void updateFace()
	{
		switch(state)
		{
			case(State.Hungry):
					faceRenderer.sprite = hungryFace;
					break;
			case (State.Chewing):
					faceRenderer.sprite = chewingFace1;
					if (chewsLeft > 0 && Input.GetMouseButtonDown(0) && animator.GetTime() >= chewTime)
					{
						animator.enabled = true;
						//Debug.Log(animator.GetTime());
						//animator.SetTime(0f);
						animator.Rebind();
						//Debug.Log(animator.GetTime());
						chewsLeft--;
						audioSource.PlayOneShot(chewClip);
						audioSource.pitch = Time.timeScale * Random.Range(.9f, 1f);
						if (chewsLeft == 0)
						{
							Invoke("checkForVictory", chewTime);
						}
						else
						{
							Invoke("spitParticles", 1f / 12f);
						}
					}
					break;
			case (State.Full):
					faceRenderer.sprite = happyFace;

				if ((Vector2)transform.position != Vector2.zero)
				{
					float diff = victoryMoveSpeedMult * distanceToCenter * Time.deltaTime;
					Vector2 toCenter = -1f * (Vector2)transform.position;
					transform.position = toCenter.magnitude <= diff ? new Vector3(0f, 0f, transform.position.z) : transform.position + (Vector3)MathHelper.resizeVector2D(toCenter, diff);
					setScale(Mathf.Lerp(victoryScale, initialScale, toCenter.magnitude / distanceToCenter));
					
				}
					break;
			default:
					break;
		}
	}

	void setScale(float scale)
	{
		transform.localScale = new Vector3(scale, scale, transform.localScale.z);
	}



	void updateMovement()
	{
		transform.position = getMousePosition();

		//going nowhere
		if (MathHelper.Approximately(lastMousePosition.x, transform.position.x, .0001f))
		{
			setFacingRight(isFacingRight());
		}
		//going left
		else if (lastMousePosition.x > transform.position.x)
		{
			setFacingRight(false);
		}
		//going right
		else
		{
			setFacingRight(true);
		}

		float speed = (transform.position.x - lastMousePosition.x) / Time.deltaTime;


		if (!MathHelper.Approximately(speed, 0f, .01f))
		{
			joint.useMotor = true;
			JointMotor2D motor = joint.motor;
			motor.motorSpeed = speed * motorSpeedMult;
			joint.motor = motor;
		}
		else
		{
			joint.useMotor = false;
		}


		lastMousePosition = getMousePosition();
	}

	bool isFacingRight()
	{
		return body.transform.localScale.x < 0f;
	}

	void setFacingRight(bool facingRight)
	{
		Vector3 scale = new Vector3(facingRight ? -1f : 1f, 1f, 1f);
		body.transform.localScale = scale;
		//Debug.Log(chewsLeft);
		face.transform.localScale = scale * Mathf.Lerp(1f, 1.3f, (float)chewsLeft / 9f);

		//burp.transform.localRotation = Quaternion.EulerAngles(0f, facingRight ? 0f : 180f, 0f);
		//burp.transform.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);

		burpParticles.startRotation = facingRight ? 0f : Mathf.PI;
		//Debug.Log((float)chewsLeft / 6f);
		//Debug.Log(face.transform.localScale);
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		collide(other);
	}

	void OnTriggerStay2D(Collider2D other)
	{
		collide(other);
	}

	void collide(Collider2D other)
	{
		if ((state == State.Hungry || canEatMultipleFoods) && other.name == "Food")
		{
			state = State.Chewing;
			other.gameObject.SetActive(false);
			particles.Play();
			chewsLeft += chewsNeeded;
			CancelInvoke();
			animator.enabled = true;
			animator.SetTime(1f);
		}
	}

	Vector3 getMousePosition()
	{
		Vector3 mousePosition = CameraHelper.getCursorPosition();
		mousePosition.z = transform.position.z;
		return mousePosition;
	}


}
