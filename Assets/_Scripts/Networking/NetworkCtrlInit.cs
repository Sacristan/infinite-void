using UnityEngine;
using System.Collections;

public class NetworkCtrlInit : MonoBehaviour {
	void OnNetworkInstantiate(NetworkMessageInfo info){
		if(networkView.isMine){
			//Camera.main.SendMessage("SetTarget",transform);
			gameObject.GetComponent<NetworkInterpolatedTransform>().enabled=false;
			Messenger<Transform>.Broadcast("network player was instantiated",transform);
			//Camera.mainCamera.SendMessage("FollowThis",transform);
//			_Data.SetMyPlayer(gameObject);
		}
		else{
			gameObject.name += "Remote";
			gameObject.GetComponent<MyPersonController>().enabled=false;
			gameObject.GetComponent<NetworkInterpolatedTransform>().enabled=true;
		}
	}
}
