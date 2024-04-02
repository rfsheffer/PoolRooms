using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Item", order = 1)]
public class Item : ScriptableObject
{
	public int itemId;

	public string itemName;

	[Space(3f)]
	public List<ItemGroup> spawnPositionTypes = new List<ItemGroup>();

	[Space(3f)]
	public bool twoHanded;

	public bool twoHandedAnimation;

	public bool disableHandsOnWall;

	public bool canBeGrabbedBeforeGameStart;

	[Space(3f)]
	public float weight = 1f;

	public bool itemIsTrigger;

	public bool holdButtonUse;

	public bool itemSpawnsOnGround = true;

	[Space(5f)]
	public bool isConductiveMetal;

	[Header("Scrap-collection")]
	public bool isScrap;

	public int creditsWorth;

	public bool lockedInDemo;

	public int highestSalePercentage = 80;

	[Space(3f)]
	public int maxValue;

	public int minValue;

	public GameObject spawnPrefab;

	[Space(3f)]
	[Header("Battery")]
	public bool requiresBattery = true;

	public float batteryUsage = 15f;

	public bool automaticallySetUsingPower = true;

	[Space(5f)]
	public Sprite itemIcon;

	[Space(5f)]
	[Header("Player animations")]
	public string grabAnim;

	public string useAnim;

	public string pocketAnim;

	public string throwAnim;

	[Space(5f)]
	public float grabAnimationTime;

	[Header("Player SFX")]
	public AudioClip grabSFX;

	public AudioClip dropSFX;

	public AudioClip pocketSFX;

	public AudioClip throwSFX;

	[Header("Netcode")]
	public bool syncGrabFunction = true;

	public bool syncUseFunction = true;

	public bool syncDiscardFunction = true;

	public bool syncInteractLRFunction = true;

	[Header("Save data")]
	public bool saveItemVariable;

	[Header("MISC")]
	public bool isDefensiveWeapon;

	[Space(3f)]
	public string[] toolTips;

	public float verticalOffset;

	public int floorYOffset;

	public bool allowDroppingAheadOfPlayer = true;

	public Vector3 restingRotation = new Vector3(0f, 0f, 90f);

	public Vector3 rotationOffset = Vector3.zero;

	public Vector3 positionOffset = Vector3.zero;

	public bool meshOffset = true;

	public Mesh[] meshVariants;

	public Material[] materialVariants;

	public bool usableInSpecialAnimations;

	public bool canBeInspected = true;
}
