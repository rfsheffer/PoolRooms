using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using System.ComponentModel;

namespace PoolRooms
{
    public class PoolRoomsWaterBehaviour : MonoBehaviour
    {
        public static string BehaviorsVer = "0.1.4";

        public AudioSource SplashSound = null;
        public AudioSource WaterMovementSound = null;
        public ParticleSystem SplashParticles = null;
        public Vector3 LastPosition = Vector3.zero;
        public float NextSplashTime = 0.0f;

        public List<AudioClip> SplashSounds = new List<AudioClip>();

        void Awake()
        {
            NextSplashTime = Time.unscaledTime;
            SplashSound.volume = 0.0f;
        }

        public AudioClip GetRandomSplashSound()
        {
            return SplashSounds[UnityEngine.Random.Range(0, SplashSounds.Count)];
        }
    }

    // A simple replication of the QuicksandTrigger in Lethal Company which also works in the facility
    public class PoolRoomsWaterTrigger : MonoBehaviour
    {
        public bool isWater;

        public int audioClipIndex;

        [Space(5f)]
        public bool sinkingLocalPlayer;

        public float movementHinderance = 1.6f;

        public float sinkingSpeedMultiplier = 0.15f;

        public Transform WaterSurface = null;

        public GameObject WaterBehaviourPrefab = null;

        private List<Transform> EnteredThingTransforms = new List<Transform>();

        private List<GameObject> WaterMovementsWeCreated = new List<GameObject>();

        private static string PoolRoomsWaterMovementTag = "PoolRoomsWaterMovement";

        private void OnDestroy()
        {
            foreach(GameObject g in WaterMovementsWeCreated)
            {
                if(g != null)
                {
                    Destroy(g);
                }
            }
            WaterMovementsWeCreated.Clear();
        }

        public static PoolRoomsWaterBehaviour FindGameObjectChildWaterBehaviour(GameObject parent)
        {
            Transform t = parent.transform;

            for (int i = 0; i < t.childCount; ++i)
            {
                var behavior = t.GetChild(i).gameObject.GetComponent<PoolRoomsWaterBehaviour>();
                if (behavior)
                {
                    return behavior;
                }
            }
            return null;
        }

        private static float FInterpTo(float Current, float Target, float DeltaTime, float InterpSpeed)
        {
            // If no interp speed, jump to target value
            if (InterpSpeed <= 0.0f)
            {
                return Target;
            }

            // Distance to reach
            float Dist = Target - Current;

            // If distance is too small, just set the desired location
            if ((Dist * Dist) < 0.00000001f)
            {
                return Target;
            }

            // Delta Move, Clamp so we do not over shoot.
            float DeltaMove = Dist * Mathf.Clamp(DeltaTime * InterpSpeed, 0.0f, 1.0f);

            return Current + DeltaMove;
        }

        private void Update()
        {
            foreach(Transform enteredThing in EnteredThingTransforms)
            {
                PoolRoomsWaterBehaviour poolRoomsWaterBehaviour = FindGameObjectChildWaterBehaviour(enteredThing.gameObject);
                if (poolRoomsWaterBehaviour != null)
                {
                    // Move the water movement onto the waters surface
                    poolRoomsWaterBehaviour.transform.position = new Vector3(enteredThing.position.x, WaterSurface.position.y, enteredThing.position.z);

                    // How deep is the player
                    float depthInWater = WaterSurface.position.y - enteredThing.position.y;
                    bool isDeep = Mathf.Abs(depthInWater) > 1.1f;

                    // Player moving this frame?
                    float playerMoveSpeed = (poolRoomsWaterBehaviour.LastPosition - enteredThing.position).magnitude;
                    bool moving = playerMoveSpeed > (0.04f * Time.deltaTime);
                    poolRoomsWaterBehaviour.LastPosition = enteredThing.position;

                    // Interp audio in and out from movement
                    float curVolume = poolRoomsWaterBehaviour.WaterMovementSound.volume;
                    curVolume = FInterpTo(curVolume, moving ? (isDeep ? 0.3f : 1.0f) : 0.0f, Time.deltaTime, 2.0f);
                    poolRoomsWaterBehaviour.WaterMovementSound.volume = curVolume;

                    //print($"Setting water volume to: {curVolume}");
                    //print($"IsDeep: {isDeep}");
                    //print($"Player Move Speed: {playerMoveSpeed}");

                    // Splash particles from time to time
                    if (!moving)
                    {
                        poolRoomsWaterBehaviour.NextSplashTime = Time.unscaledTime + 0.5f;
                    }
                    else
                    {
                        if(poolRoomsWaterBehaviour.NextSplashTime <= Time.unscaledTime)
                        {
                            poolRoomsWaterBehaviour.NextSplashTime = Time.unscaledTime + (playerMoveSpeed > 0.1f ? 0.3f : 0.5f);
                            poolRoomsWaterBehaviour.SplashParticles.Play();
                            poolRoomsWaterBehaviour.SplashSound.clip = poolRoomsWaterBehaviour.GetRandomSplashSound();
                            poolRoomsWaterBehaviour.SplashSound.volume = playerMoveSpeed > 0.1f ? 0.3f : 0.1f;
                            poolRoomsWaterBehaviour.SplashSound.Play();
                        }
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isWater && other.gameObject.CompareTag("Player"))
            {
                PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
                if(component != null) 
                {
                    EnteredThingTransforms.Add(component.transform);
                    PoolRoomsWaterBehaviour poolRoomsWaterBehaviour = FindGameObjectChildWaterBehaviour(component.gameObject);
                    if (poolRoomsWaterBehaviour == null)
                    {
                        print("Creating new WaterBehaviourPrefab");

                        GameObject behaviorGO = Instantiate(WaterBehaviourPrefab, new Vector3(0, 0, 0), Quaternion.Euler(-90.0f, 0.0f, 0.0f));

                        behaviorGO.transform.parent = component.transform;
                        behaviorGO.transform.localPosition = Vector3.zero;
                        behaviorGO.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);

                        poolRoomsWaterBehaviour = behaviorGO.GetComponent<PoolRoomsWaterBehaviour>();
                    }

                    poolRoomsWaterBehaviour.WaterMovementSound.Play();
                    poolRoomsWaterBehaviour.WaterMovementSound.volume = 0;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            OnExit(other);

            if (isWater && other.gameObject.CompareTag("Player"))
            {
                PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
                if (component != null)
                {
                    EnteredThingTransforms.Remove(component.transform);
                    PoolRoomsWaterBehaviour poolRoomsWaterBehaviour = FindGameObjectChildWaterBehaviour(component.gameObject);
                    if (poolRoomsWaterBehaviour != null)
                    {
                        poolRoomsWaterBehaviour.WaterMovementSound.Stop();
                    }
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (isWater)
            {
                if (!other.gameObject.CompareTag("Player"))
                {
                    return;
                }

                // If this isn't the local player and this remote players underwaterCollider isn't set to this, set it and return.
                PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
                if (component != GameNetworkManager.Instance.localPlayerController && component != null && component.underwaterCollider != this)
                {
                    component.underwaterCollider = gameObject.GetComponent<Collider>();
                    print($"Setting underwater collider to {component.underwaterCollider.name}");
                    return;
                }
            }
            if (!isWater && !other.gameObject.CompareTag("Player"))
            {
                return;
            }
            PlayerControllerB component2 = other.gameObject.GetComponent<PlayerControllerB>();
            if (component2 != GameNetworkManager.Instance.localPlayerController)
            {
                print("Player is not local player controller!");
                return;
            }
            if (isWater && !component2.isUnderwater)
            {
                component2.underwaterCollider = gameObject.GetComponent<Collider>();
                component2.isUnderwater = true;
                print($"Setting underwater collider to {component2.underwaterCollider.name}");
            }
            component2.statusEffectAudioIndex = audioClipIndex;
            if (component2.isSinking)
            {
                return;
            }
            if (sinkingLocalPlayer)
            {
                if (!component2.CheckConditionsForSinkingInQuicksand())
                {
                    StopSinkingLocalPlayer(component2);
                }
            }
            else if (component2.CheckConditionsForSinkingInQuicksand())
            {
                Debug.Log("Set local player to sinking!");
                sinkingLocalPlayer = true;
                component2.sourcesCausingSinking++;
                component2.isMovementHindered++;
                component2.hinderedMultiplier *= movementHinderance;
                if (isWater)
                {
                    component2.sinkingSpeedMultiplier = 0f;
                }
                else
                {
                    component2.sinkingSpeedMultiplier = sinkingSpeedMultiplier;
                }
            }
        }

        public void OnExit(Collider other)
        {
            if (!sinkingLocalPlayer)
            {
                if (isWater && other.CompareTag("Player") && !(other.gameObject.GetComponent<PlayerControllerB>() == GameNetworkManager.Instance.localPlayerController))
                {
                    other.gameObject.GetComponent<PlayerControllerB>().isUnderwater = false;
                }
            }
            else if (other.CompareTag("Player"))
            {
                PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
                if (!(component != GameNetworkManager.Instance.localPlayerController))
                {
                    StopSinkingLocalPlayer(component);
                }
            }
        }

        public void StopSinkingLocalPlayer(PlayerControllerB playerScript)
        {
            if (sinkingLocalPlayer)
            {
                sinkingLocalPlayer = false;
                playerScript.sourcesCausingSinking = Mathf.Clamp(playerScript.sourcesCausingSinking - 1, 0, 100);
                playerScript.isMovementHindered = Mathf.Clamp(playerScript.isMovementHindered - 1, 0, 100);
                playerScript.hinderedMultiplier = Mathf.Clamp(playerScript.hinderedMultiplier / movementHinderance, 1f, 100f);
                if (playerScript.isMovementHindered == 0 && isWater)
                {
                    playerScript.isUnderwater = false;
                }
            }
        }
    }
}
