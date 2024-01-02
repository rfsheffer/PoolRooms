using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class InteractTrigger : NetworkBehaviour
{
	[Header("Aesthetics")]
	public Sprite hoverIcon;

	public string hoverTip;

	[Space(5f)]
	public Sprite disabledHoverIcon;

	public string disabledHoverTip;

	[Header("Interaction")]
	public bool interactable = true;

	public bool oneHandedItemAllowed = true;

	public bool twoHandedItemAllowed;

	[Space(5f)]
	public bool holdInteraction;

	public float timeToHold = 0.5f;

	public float timeToHoldSpeedMultiplier = 1f;

	public string holdTip;

	public bool isBeingHeldByPlayer;

	public UnityEvent<float> holdingInteractEvent;

	private float timeHeld;

	private bool isHoldingThisFrame;

	[Space(5f)]
	public bool touchTrigger;

	public bool triggerOnce;

	private bool hasTriggered;

	[Header("Misc")]
	public bool interactCooldown = true;

	public float cooldownTime = 1f;

	[HideInInspector]
	public float currentCooldownValue;

	public bool disableTriggerMesh = true;

	[Space(5f)]
	public bool RandomChanceTrigger;

	public int randomChancePercentage;

	[Header("Events")]
	public UnityEvent<NetworkBehaviour> onInteract;

	public UnityEvent<NetworkBehaviour> onInteractEarly;

	public UnityEvent<NetworkBehaviour> onStopInteract;

	public UnityEvent<NetworkBehaviour> onCancelAnimation;

	[Header("Special Animation")]
	public bool specialCharacterAnimation;

	public bool stopAnimationManually;

	public string stopAnimationString = "SA_stopAnimation";

	public bool hidePlayerItem;

	public bool isPlayingSpecialAnimation;

	public float animationWaitTime = 2f;

	public string animationString;

	[Space(5f)]
	public bool lockPlayerPosition;

	public Transform playerPositionNode;

	private Transform lockedPlayer;

	private bool usedByOtherClient;

	private NetworkBehaviour playersManager;

	private float updateInterval = 1f;

	[Header("Ladders")]
	public bool isLadder;

	public Transform topOfLadderPosition;

	public bool useRaycastToGetTopPosition;

	public Transform bottomOfLadderPosition;

	public Transform ladderHorizontalPosition;

	[Space(5f)]
	public Transform ladderPlayerPositionNode;

	public bool usingLadder;

	private bool atBottomOfLadder;

	private Vector3 moveVelocity;

	private NetworkBehaviour playerScriptInSpecialAnimation;

	private Coroutine useLadderCoroutine;

	private int playerUsingId;
}
