using UnityEngine;
using System.Collections;

public class TestProjectileCtrl : MonoBehaviour {

	//private int speed  = 100;
	private Vector3 moveToPos;
	private GameObject fwdGO;
	Vector3 direction;

	void Awake(){
		/*
		if(Network.isClient || Network.isServer){
			foreach(GameObject go in GameObject.FindGameObjectsWithTag("Gun")){
				if(go.transform.root.gameObject.networkView.isMine) fwdGO = go;
			}
		}
		else fwdGO = GameObject.FindWithTag("Player");
		*/
		fwdGO = Camera.main.gameObject;
		direction = fwdGO.transform.forward;
		Destroy(gameObject, 2.0f);
	}
	
	void Update (){
		rigidbody.AddForce(direction*100,ForceMode.Impulse);
	}
	
	void OnCollisionEnter(Collision other){ 
		GameObject go = other.collider.gameObject;
		if(go.tag=="Player"){
			
		}
	}
	void OnNetworkInstantiate(NetworkMessageInfo info) {
		Network.RemoveRPCs(networkView.viewID);
    }
}
