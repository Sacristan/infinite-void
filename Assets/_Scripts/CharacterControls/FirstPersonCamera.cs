using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MouseLook))]
public class FirstPersonCamera : MonoBehaviour {
	bool isFPSCam = false;
	GameObject cam;
	Transform defTransform;
	WowCamera wowCam;
	bool camChangeEntered =false;
	public Transform FPStrans;
		
	void Start(){
		if(!networkView.isMine && (Network.isClient || Network.isServer)) this.enabled=false;
		cam = Camera.main.gameObject;
		defTransform = cam.transform;
		wowCam = cam.GetComponent<WowCamera>();
		if(!isFPSCam) EnableFPSLook(false);
	}
	void Update(){
		if(Input.GetKeyDown(KeyCode.C)){
			isFPSCam = !isFPSCam;
			camChangeEntered = true;
		}
		if(camChangeEntered) TriggerFPSCam(isFPSCam);
	}
	void TriggerFPSCam(bool p){
		camChangeEntered = false;
		
		Transform targetTransform;
		if(p){
			targetTransform=FPStrans;
			wowCam.enabled=false;
			cam.transform.parent=FPStrans;
			EnableFPSLook(true);
		}
		else{
			targetTransform=defTransform;
			wowCam.enabled=true;
			cam.transform.parent=null;
			EnableFPSLook(false);
			//FPStrans.rotation = Quaternion.identity; //mmm
		}
		cam.transform.position = targetTransform.position;
		cam.transform.rotation = targetTransform.rotation;
	}
	void EnableFPSLook(bool p){
		cam.GetComponent<MouseLook>().enabled=p;
		gameObject.GetComponent<MouseLook>().enabled=p;
		//FPStrans.gameObject.GetComponent<MouseLook>().enabled=p;
		//FPStrans.gameObject.GetComponent<Camera>().enabled=p;
		Messenger<bool>.Broadcast("fps got enabled",p);
	}
	
	//Listeners and related functions
	void OnEnable(){
	//	Messenger
		Messenger<bool>.AddListener("freeze character completely",FreezeMouseLooks);
	}
	void OnDisable(){
		Messenger<bool>.RemoveListener("freeze character completely",FreezeMouseLooks);
	}
	void FreezeMouseLooks(bool p){
		cam.GetComponent<MouseLook>().enabled=p;
		gameObject.GetComponent<MouseLook>().enabled=p;
	}
}
