using UnityEngine;
using System.Collections;

public class NetworkInstantiator : MonoBehaviour {

	public Transform prefabToInstantiate;
	public int instanceGroup=0;
	public bool useThisRotation=true;
	public bool addNetworkView=false;
	
	void Start(){
		if(Network.isClient || Network.isServer){
			Quaternion q = (useThisRotation) ? transform.rotation : Quaternion.identity;
			GameObject go = Network.Instantiate(prefabToInstantiate,transform.position,q,instanceGroup) as GameObject;
			if(addNetworkView) go.AddComponent<NetworkView>();
		}
	}
}
