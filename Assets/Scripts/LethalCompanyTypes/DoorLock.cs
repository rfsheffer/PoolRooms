
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(InteractTrigger))]
public class DoorLock : NetworkBehaviour
{
	private InteractTrigger doorTrigger;

	public float maxTimeLeft = 60f;

	public float lockPickTimeLeft = 60f;

	public bool isLocked;

	public bool isPickingLock;

	[Space(5f)]
	public DoorLock twinDoor;

	public Transform lockPickerPosition;

	public Transform lockPickerPosition2;

	private float enemyDoorMeter;

	private bool isDoorOpened;

	private NavMeshObstacle navMeshObstacle;

	public AudioClip pickingLockSFX;

	public AudioClip unlockSFX;

	public AudioSource doorLockSFX;

	private bool displayedLockTip;

	private bool localPlayerPickingLock;

	private int playersPickingDoor;

	private float playerPickingLockProgress;

	[Space(3f)]
	public float defaultTimeToHold = 0.3f;

	private bool hauntedDoor;

	private float doorHauntInterval;


	public void OnHoldInteract()
	{

	}

	public void LockDoor(float timeToLockPick = 30f)
	{

	}

	public void UnlockDoor()
	{

	}

	public void UnlockDoorSyncWithServer()
	{

	}

	public void OpenOrCloseDoor(NetworkBehaviour playerWhoTriggered)
	{

	}

	public void SetDoorAsOpen(bool isOpen)
	{

	}

	public void OpenDoorAsEnemy()
	{

	}


	public void TryPickingLock()
	{

	}

	public void StopPickingLock()
	{

	}
}
