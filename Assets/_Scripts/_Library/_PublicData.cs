using UnityEngine;

public class _PublicData:MonoBehaviour{
	
	void Awake(){
		_PublicFunctions.LockCursor(true);
		_PublicFunctions.HideCursor(true);
	}
	
	public class _PublicProperties{
		public static bool lockCursor;
		public static bool hideCursor;
		public static GameObject minePlayer;
	}
	public class _PublicFunctions{
		public static System.Collections.IEnumerator PlayOneShot(Animator pAnimator, string paramName){
	        pAnimator.SetBool( paramName, true );
	        yield return null;
	        pAnimator.SetBool( paramName, false );
	    }
		public static void LockCursor(bool p){
			_PublicProperties.lockCursor=p;
			Screen.lockCursor=_PublicProperties.lockCursor;
		}
		public static void HideCursor(bool p){
			_PublicProperties.hideCursor=!p;
			Screen.showCursor=_PublicProperties.hideCursor;	
		}
	}
	
	void OnEnable(){
		Messenger<Transform>.AddListener("network player was instantiated",CheckIfPlayerIsMine);
	}
	void OnDisable(){
		Messenger<Transform>.RemoveListener("network player was instantiated",CheckIfPlayerIsMine);
	}
	void CheckIfPlayerIsMine(Transform t){
		if(minePlayer==null){
			if(t.gameObject.networkView.isMine){
				minePlayer=t.gameObject;
			}
		}
	}
}