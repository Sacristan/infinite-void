using UnityEngine;
using System.Collections;

public class RPCs : MonoBehaviour {

	[RPC]
	void FeedMyMecanimRemotely(NetworkPlayer np, float h, float v, bool s, bool j, bool c, bool sh,bool r){
		foreach(NetworkPlayer npp in Network.connections){
			if(npp.ToString() == np.ToString()){
				Animator _anim = GetComponent<Animator>();	
				_anim.SetFloat("speed",v);
				_anim.SetFloat("direction",h);
				_anim.SetBool("canSprint",s);
				_anim.SetBool("canJump",j);
				_anim.SetBool("canClimb",c);
				_anim.SetBool("shoot",sh);
				_anim.SetBool("rifleActivated",r);
				break;
			}
		}
	}
	[RPC] 
	void SyncMyPosition(NetworkPlayer np, Vector3 position){
		foreach(NetworkPlayer npp in Network.connections){
			if(npp.ToString() == np.ToString()){
				transform.position = position;
				break;
			}
		}
	}
	[RPC]
	GameObject CreateCopyLocally(Transform t){
		GameObject go = Instantiate(t,t.position,t.rotation);
	}
}
