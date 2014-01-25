using UnityEngine;
using System.Collections;

public class NetworkSpawnPlayerPrefab : MonoBehaviour {
	public Transform playerPrefab;
	
	void OnNetworkLoadedLevel(){
		Network.Instantiate(playerPrefab, 
			new Vector3(transform.position.x+Random.Range(-1,1),transform.position.y,transform.position.z+Random.Range(-1,1)), 
			transform.rotation, 0);
	}
	
	void OnPlayerDisconnected(NetworkPlayer player){
		Debug.Log("Server destroying player");
		Network.RemoveRPCs(player, 0);
		Network.DestroyPlayerObjects(player);
	}
}
