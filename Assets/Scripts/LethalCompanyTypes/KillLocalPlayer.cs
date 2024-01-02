
using UnityEngine;
using Unity.Netcode;

public class KillLocalPlayer : MonoBehaviour
{
	public bool dontSpawnBody;

	public CauseOfDeath causeOfDeath = CauseOfDeath.Gravity;

	public bool justDamage;

	public NetworkBehaviour playersManager;

	public int deathAnimation;

	[Space(5f)]
	public NetworkBehaviour roundManager;

	public Transform spawnEnemyPosition;

	[Space(5f)]
	public int enemySpawnNumber;

	public int playAudioOnDeath = -1;

	public void KillPlayer(NetworkBehaviour playerWhoTriggered)
	{
	}
}
