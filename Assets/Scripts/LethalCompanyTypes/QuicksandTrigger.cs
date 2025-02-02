using UnityEngine;

public class QuicksandTrigger : MonoBehaviour
{
	public bool isWater;

	public bool isInsideWater;

	public int audioClipIndex;

	[Space(5f)]
	public bool sinkingLocalPlayer;

	public float movementHinderance = 1.6f;

	public float sinkingSpeedMultiplier = 0.15f;
}
