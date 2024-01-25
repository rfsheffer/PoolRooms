using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class OccludeAudio : MonoBehaviour
{
	private AudioLowPassFilter lowPassFilter;

	private AudioReverbFilter reverbFilter;

	public bool useReverb;

	private bool occluded;

	private AudioSource thisAudio;

	private float checkInterval;

	public bool overridingLowPass;

	public float lowPassOverride = 20000f;

	public bool debugLog;
}
