using UnityEngine;
using System.Collections;

public class RemoveTestChar : MonoBehaviour {

	public GameObject testCharToRemove;
	
	void Awake(){
		if(testCharToRemove==null || Network.isClient || Network.isServer){
			Destroy(testCharToRemove);
		}
	}
}
