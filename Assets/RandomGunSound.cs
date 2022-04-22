using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomGunSound : MonoBehaviour
{
	public List<AudioClip> clips = new List<AudioClip>();

	private void Start()
	{
		GetComponent<AudioSource>().clip = clips[Random.Range(0, clips.Count)];
	}
}
