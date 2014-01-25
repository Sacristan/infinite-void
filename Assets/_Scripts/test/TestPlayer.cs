using UnityEngine;
using System.Collections;

public class TestPlayer : MonoBehaviour {
	
	public Transform ball;
	public Transform holder;
	public float throwSpeed =100f;
	
	void Start(){
		Physics.IgnoreCollision(collider,ball.collider);	
	}
	
	void Update () {
		if(Input.GetKeyDown(KeyCode.F)){
			if(ball.parent!=null){
				ball.parent=null;
				ball.rigidbody.isKinematic=false;
				ball.rigidbody.AddForce(Camera.main.gameObject.transform.forward * throwSpeed,ForceMode.Impulse);
			}
			else{
				ball.rigidbody.isKinematic=true;
				ball.parent = holder;
				ball.position = holder.position;
			}
		}
	}
	void OnGUI(){
		GUI.Label(new Rect(Screen.width-200,0,200,20),"F to throw/Space to Jump");
	}
}
