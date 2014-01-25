using UnityEngine;
using System.Collections;

public class Teleporter : MonoBehaviour {
	public Transform destination;
	
	void OnTriggerEnter (Collider other) {
		if (other.CompareTag ("Player")) {
			other.transform.position=destination.transform.position;
	    }
	}
}