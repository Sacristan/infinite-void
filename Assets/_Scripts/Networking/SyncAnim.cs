using UnityEngine;
//using System.Collections;
using System;

//***TODO***

public class SyncAnim : MonoBehaviour {
	/*
	public string currentAnimation;
	public string lastAnimation;
	
	public void SyncAnimation(String animationValue){
		currentAnimation = MyPersonController.fwdState.ToString();	
	}
	
	void Update(){
		gameObject.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
	}
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info){
		char ani;
		if(stream.isWriting){
			ani = (char) currentAnimation;
			stream.Serialize(ref ani);
		}
		else{
			ani = (char)0;
			stream.Serialize(ref ani);
			currentAnimation= MyPersonController.idleState.ToString();
		}
	}
	*/
}
