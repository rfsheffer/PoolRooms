using System;
using Unity.Netcode;
using UnityEngine;

public abstract class GrabbableObject : NetworkBehaviour
{
	public bool grabbable;

	public bool isHeld;

	public bool isHeldByEnemy;

	public bool deactivated;

	[Space(3f)]
	public Transform parentObject;

	public Vector3 targetFloorPosition;

	public Vector3 startFallingPosition;

	public int floorYRot;

	public float fallTime;

	public bool hasHitGround;

	[Space(5f)]
	public int scrapValue;

	public bool itemUsedUp;

	public NetworkBehaviour playerHeldBy;

	public bool isPocketed;

	public bool isBeingUsed;

	public bool isInElevator;

	public bool isInShipRoom;

	public bool isInFactory = true;

	[Space(10f)]
	public float useCooldown;

	public float currentUseCooldown;

	[Space(10f)]
	public Item itemProperties;

	public Battery insertedBattery;

	public string customGrabTooltip;

	[HideInInspector]
	public Rigidbody propBody;

	[HideInInspector]
	public Collider[] propColliders;

	[HideInInspector]
	public Vector3 originalScale;

	public bool wasOwnerLastFrame;

	public MeshRenderer mainObjectRenderer;

	private int isSendingItemRPC;

	public bool scrapPersistedThroughRounds;

	public bool heldByPlayerOnServer;

	[HideInInspector]
	public Transform radarIcon;

	public bool reachedFloorTarget;

	[Space(3f)]
	public bool grabbableToEnemies = true;

	private bool hasBeenHeld;
}
