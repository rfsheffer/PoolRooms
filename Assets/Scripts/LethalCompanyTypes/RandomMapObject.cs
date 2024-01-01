using System.Collections.Generic;
using UnityEngine;

public class RandomMapObject : MonoBehaviour
{
	public List<GameObject> spawnablePrefabs = new List<GameObject>();

	public bool randomizePosition = true;

	public float spawnRange = 10f;
}
