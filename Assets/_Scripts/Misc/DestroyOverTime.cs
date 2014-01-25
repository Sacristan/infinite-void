using UnityEngine;
using System.Collections;

public class DestroyOverTime : MonoBehaviour {
	void Awake(){
		Destroy(gameObject,30.0f);
	}
}
