﻿using System;
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
        public static string BehaviorsVer = "6";

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

    // Splashing FX and such
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
            List<Transform> thingsToRemove = new List<Transform>();
            foreach (Transform enteredThing in EnteredThingTransforms)
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
                    if(playerMoveSpeed > 5.0f)
                    {
                        // The player moved a very far distance in a short period of time, probably teleported by something. Stop being in water!
                        thingsToRemove.Add(enteredThing);
                        continue;
                    }
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
                            poolRoomsWaterBehaviour.SplashSound.volume = playerMoveSpeed > 0.1f ? 0.15f : 0.05f;
                            poolRoomsWaterBehaviour.SplashSound.Play();
                        }
                    }
                }
            }
            // Remove things which were teleported
            foreach (Transform enteredThing in thingsToRemove)
            {
                EnteredThingTransforms.Remove(enteredThing);
                PoolRoomsWaterBehaviour poolRoomsWaterBehaviour = FindGameObjectChildWaterBehaviour(enteredThing.gameObject);
                if (poolRoomsWaterBehaviour != null)
                {
                    poolRoomsWaterBehaviour.WaterMovementSound.Stop();
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

                    poolRoomsWaterBehaviour.LastPosition = component.transform.position;
                    poolRoomsWaterBehaviour.WaterMovementSound.Play();
                    poolRoomsWaterBehaviour.WaterMovementSound.volume = 0;
                }
            }
        }

        public void OnLocalPlayerTeleported(PlayerControllerB player)
        {
            if (player != null)
            {
                EnteredThingTransforms.Remove(player.transform);
                PoolRoomsWaterBehaviour poolRoomsWaterBehaviour = FindGameObjectChildWaterBehaviour(player.gameObject);
                if (poolRoomsWaterBehaviour != null)
                {
                    poolRoomsWaterBehaviour.WaterMovementSound.Stop();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
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
    }
}
