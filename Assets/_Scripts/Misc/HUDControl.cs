using UnityEngine;
using System.Collections;

public class HUDControl : MonoBehaviour {
	public Color HUDColor = Color.blue;
	public Texture screenHUDGrid;
	public GameObject[] hudElements;
	private bool isGridActive=true;
	private bool isMine=false;
	

	void OnEnable(){
		Messenger<bool>.AddListener("fps got enabled",FPSControl);
		Messenger<Transform>.AddListener("network player was instantiated",CheckIfHUDIsMine);
	}
	void OnDisable(){
		Messenger<bool>.RemoveListener("fps got enabled",FPSControl);
		Messenger<Transform>.RemoveListener("network player was instantiated",CheckIfHUDIsMine);
	}	
		
	void FPSControl(bool param){
		isGridActive = param;
		if(param&&isMine){
			foreach(GameObject go in hudElements){
				iTween.FadeTo(go,1.0f,2f);
				iTween.ShakeScale(go,new Vector3(0.1f,0f,0.1f),2f);
			}
		}
		else{
			foreach(GameObject go in hudElements){
				iTween.FadeTo(go,0.0f,0.0f);
				//iTween.ShakeScale(go,new Vector3(0.1f,0f,0.1f),2f);
			}
		}
	}
	void Awake(){
		if(!Network.isClient && !Network.isServer) {
			isMine = true;
			this.camera.enabled=true;
		}
		else{
			this.camera.enabled=false;
		}
	}
	void CheckIfHUDIsMine(Transform t){
		if(t.gameObject.networkView.isMine){
			isMine=true;
			this.camera.enabled=isMine;
		}
	}
	void OnGUI(){
		if(isGridActive&&isMine){
			GUI.color=HUDColor;
			GUI.depth=-1;
			GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),screenHUDGrid);
		}
	}
}
