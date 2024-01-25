using System;
using Unity.Netcode;
using UnityEngine;

public class AnimatedObjectTrigger : NetworkBehaviour
{
	public Animator triggerAnimator;

	public Animator triggerAnimatorB;

	public bool isBool = true;

	public string animationString;

	public bool boolValue;

	public bool setInitialState;

	public bool initialBoolState;

	[Space(5f)]
	public AudioSource thisAudioSource;

	public AudioClip[] boolFalseAudios;

	public AudioClip[] boolTrueAudios;

	public AudioClip[] secondaryAudios;

	[Space(4f)]
	public AudioClip playWhileTrue;

	public bool resetAudioWhenFalse;

	public bool makeAudibleNoise;

	public float noiseLoudness = 0.7f;

	[Space(3f)]
	public ParticleSystem playParticle;

	[Space(4f)]
	private NetworkBehaviour playersManager;

	private bool localPlayerTriggered;

	public BooleanEvent onTriggerBool;

	[Space(5f)]
	public bool playAudiosInSequence;

	private int timesTriggered;

	public bool triggerByChance;

	public float chancePercent = 5f;

	private bool hasInitializedRandomSeed;

	public System.Random triggerRandom;

	private float audioTime;

	public void TriggerAnimation(NetworkBehaviour playerWhoTriggered)
	{

	}
}
