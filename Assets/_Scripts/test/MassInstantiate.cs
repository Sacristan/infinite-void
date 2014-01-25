using UnityEngine;
using System.Collections;

public class MassInstantiate : MonoBehaviour {
	public Transform toInstantiate;
	public float time = .5f;
	int counter=0;
	
	void Instantiator(){
		Instantiate(toInstantiate,transform.position,Quaternion.identity);
	
		counter++;
//		Debug.Log(counter.ToString());
		Vector3 tmp = new Vector3(1,0,1)*Random.Range(-5,5);
		transform.position+=tmp;
	}
	
	IEnumerator Wait(){
		yield return new WaitForSeconds(30.0f);
		gameObject.SetActive(false);
	}
	
	void Start(){
		InvokeRepeating("Instantiator",0.0f,time);
		Wait();
	}
}
