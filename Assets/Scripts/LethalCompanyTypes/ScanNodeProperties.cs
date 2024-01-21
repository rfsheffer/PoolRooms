using UnityEngine;

public class ScanNodeProperties : MonoBehaviour
{
	public int maxRange = 7;

	public int minRange = 5;

	public bool requiresLineOfSight = true;

	[Space(5f)]
	public string headerText;

	public string subText;

	public int scrapValue;

	[Space(5f)]
	public int creatureScanID = -1;

	[Space(3f)]
	public int nodeType;
}
