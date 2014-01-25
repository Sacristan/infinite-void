#pragma warning disable 0219


/*
 * Calling from script ObjectDestruction.FractureAtPoint(Vector3 point,Vector3 force)
 */

using UnityEngine;
using System.Collections;

public class ObjectDestruction : MonoBehaviour {
	
	//public bool useTriggers=false;
	public bool fractureToPoint = false;
	public bool useCollisionDirection = false;
	public bool fractureAtCenter = false;
	public bool smartJoints = false;
	public bool avoidSelfCollision=true;
	//public bool ignoreKinematicRigidbodies=true;
	//public bool avoidCollisionWithOtherFractureObjects=true;
	public int totalMaxFractures = 3;
	public float forcePerDivision = 20.0f;
	public float minBreakingForce = 0.0f;
	public int maxFracturesPerCall = 3;
	public float randomOffset = 0.0f;
	public Vector3 minFractureSize = Vector3.zero;
	public Vector3 grain = Vector3.one;
	public float destroyAllAfterTime = 0.0f;
	public float destroySmallAfterTime = 0.0f;
	public Transform instantiateOnBreak;
	public float totalMassIfStatic = 1.0f;
	private Joint[] joints;
	public string[] tagsToIgnore;
	public float terrainDamage = 0.0f;

	void Start(){
		if(rigidbody){
			ArrayList temp = new ArrayList();
			foreach(Joint j in FindObjectsOfType(typeof(Joint))){
				if(j.connectedBody == rigidbody){
					temp.Add(j);
					joints = (Joint[]) temp.ToArray(typeof(Joint));
				}
			}
		}
	}
	
	
	/*
	void OnTriggerEnter(Collider col){
		if(useTriggers){
			if(tagsToIgnore.Length>0){
				foreach(string t in tagsToIgnore){
					GameObject tmp = collision.gameObject;
					if(tmp.tag==t) return;
				}
			}
			Vector3 point = collision.contacts[0].point;
			Vector3 vec = collision.relativeVelocity*UsedMass(collision);
			FractureAtPoint(point,vec)
		}
	}
	*/

	void OnCollisionEnter(Collision collision){
		if(tagsToIgnore.Length>0){
			foreach(string t in tagsToIgnore){
				GameObject tmp = collision.gameObject;
				if(tmp.tag==t) return;
			}
		}
		
		Vector3 point = collision.contacts[0].point;
		Vector3 vec = collision.relativeVelocity*UsedMass(collision);
		FractureAtPoint(point,vec);
	}
	public void FractureAtPoint(Vector3 hit, Vector3 force){
		if(force.magnitude < Mathf.Max(minBreakingForce,forcePerDivision)) return;
		Vector3 point = transform.worldToLocalMatrix.MultiplyPoint(hit);
		float iterations = Mathf.Min(Mathf.RoundToInt(force.magnitude/forcePerDivision), Mathf.Min(maxFracturesPerCall,totalMaxFractures));
		Fracture(point,force,iterations);
	}

	 void Fracture(Vector3 point, Vector3 force, float iterations){
		MeshFilter thisMeshFilter = gameObject.GetComponent<MeshFilter>();

		if(instantiateOnBreak && force.magnitude >= Mathf.Max(minBreakingForce, forcePerDivision)){
			if(Network.isServer || Network.isClient) Network.Instantiate(instantiateOnBreak, transform.position, transform.rotation,0);
			else Instantiate(instantiateOnBreak, transform.position, transform.rotation);
			instantiateOnBreak=null;
		}
		while (iterations > 0){
			if(totalMaxFractures == 0 || Vector3.Min(thisMeshFilter.mesh.bounds.size, minFractureSize) != minFractureSize){
				if(destroySmallAfterTime >= 1.0f)
					Destroy(gameObject, destroySmallAfterTime);

				totalMaxFractures = 0;
				return;
			}
			totalMaxFractures--;
			iterations--;

			if(fractureAtCenter) point = thisMeshFilter.mesh.bounds.center;
			Vector3 vec = Vector3.Scale(grain,Random.insideUnitSphere).normalized;
			Vector3 sub = transform.worldToLocalMatrix.MultiplyVector(force.normalized)*(useCollisionDirection ? 1 : 0)*Vector3.Dot(transform.worldToLocalMatrix.MultiplyVector(force.normalized),vec);
			//Vector3 sub = transform.worldToLocalMatrix.MultiplyVector(force.normalized)*useCollisionDirection*Vector3.Dot(transform.worldToLocalMatrix.MultiplyVector(force.normalized),vec);
			Plane plane = new Plane(vec-sub,Vector3.Scale(Random.insideUnitSphere, thisMeshFilter.mesh.bounds.size)*randomOffset+point);
			GameObject newObject;
			if(Network.isServer || Network.isClient){
				newObject = _PublicData._PublicProperties.minePlayer.networkView.RPC("CreateCopyLocally",RPCMode.OthersBuffered,gameObject.transform);
				//newObject = Network.Instantiate(gameObject.transform, transform.position, transform.rotation,0) as GameObject;
			}
			else newObject = Instantiate(gameObject.transform, transform.position, transform.rotation) as GameObject;
			MeshFilter newObjectMeshFilter = newObject.GetComponent<MeshFilter>();
			if(avoidSelfCollision) IgnoreCollision(newObject.collider,gameObject.collider);
			
			if(rigidbody) newObject.rigidbody.velocity = rigidbody.velocity;
			Vector3[] vertsA = thisMeshFilter.mesh.vertices;
			Vector3[] vertsB = newObjectMeshFilter.mesh.vertices;
			
			Vector3 average = Vector3.zero;
			foreach(Vector3 i in vertsA) average+=i;
			
			average /= thisMeshFilter.mesh.vertexCount;
			average -= plane.GetDistanceToPoint(average)*plane.normal;

			int broken = 0;

			if(fractureToPoint){
				for(int i=0;i<thisMeshFilter.mesh.vertexCount;i++){
					if(plane.GetSide(vertsA[i])){
						vertsA[i] = average;
						broken++;
					}
					else vertsB[i] = average;
				}
			}
			else{
				for(int ii=0;ii<thisMeshFilter.mesh.vertexCount;ii++){
					if(plane.GetSide(vertsA[ii])){
						vertsA[ii] -= plane.GetDistanceToPoint(vertsA[ii])*plane.normal;
						broken++;
					}
					else vertsB[ii] -= plane.GetDistanceToPoint(vertsB[ii])*plane.normal;
				}
			}

			if(broken == 0 || broken == thisMeshFilter.mesh.vertexCount){
				totalMaxFractures++;
				iterations++;
				Destroy(newObject);
				//yield return null; //mmm
			}
			else{
				thisMeshFilter.mesh.vertices = vertsA;
				newObjectMeshFilter.mesh.vertices = vertsB;
				
				thisMeshFilter.mesh.RecalculateNormals();
				newObjectMeshFilter.mesh.RecalculateNormals();

				thisMeshFilter.mesh.RecalculateBounds();
				newObjectMeshFilter.mesh.RecalculateBounds();

				MeshCollider thisMeshCol = gameObject.GetComponent<MeshCollider>();
				if(thisMeshCol){
					thisMeshCol.sharedMesh = thisMeshFilter.mesh;
					newObject.GetComponent<MeshCollider>().sharedMesh = newObjectMeshFilter.mesh; //mmm
				}
				else{
					Destroy(collider);
					Destroy(gameObject, 1.0f);
				}
			}
			if(smartJoints){
				Joint[] jointsb = GetComponents<Joint>();
				if(jointsb.Length>0){
					for(int iii=0;iii<jointsb.Length;iii++){
						if(jointsb[iii].connectedBody!=null && plane.GetSide(transform.worldToLocalMatrix.MultiplyPoint(jointsb[iii].connectedBody.transform.position))){
							Joint[] tmpJoints = jointsb[iii].gameObject.GetComponent<ObjectDestruction>().joints;
							if(tmpJoints.Length>0){
								foreach(Joint j in tmpJoints){
									//if(j == jointsb[iii]) j=newObject.GetComponents<Joint>()[iii]; //mmm
								}
							}
							Destroy(jointsb[iii]);
						}
						else{
							Destroy(newObject.GetComponents<Joint>()[iii]);
						}
					}
				}
				if(joints!=null){
					ArrayList temp;
					for(int iiii=0;iiii<joints.Length;iiii++){
						if(joints[iiii] && plane.GetSide(transform.worldToLocalMatrix.MultiplyPoint(joints[iiii].transform.position))){
							joints[iiii].connectedBody=newObject.rigidbody;
							temp = new ArrayList(joints);
							temp.RemoveAt(iiii);
							joints = (Joint[]) temp.ToArray(typeof(Joint));
						}
						else{
							temp = new ArrayList(joints);
							temp.RemoveAt(iiii);
							newObject.GetComponent<ObjectDestruction>().joints = (Joint[]) temp.ToArray(typeof(Joint));
						}
					}
				}
			}
			else{
				if(GetComponent<Joint>()){
					Joint[] jnts = GetComponents<Joint>();
					Joint[] newjnts = GetComponents<Joint>();
					for(int i_i=0;i_i<jnts.Length;i_i++){
						Destroy(jnts[i_i]);
						Destroy(newjnts[i_i]);
					}
				}
				if(joints != null){
					foreach(Joint jnt in joints){
						Destroy(jnt);
					}
					joints=null;
				}
			}
			if(!rigidbody){
				gameObject.AddComponent<Rigidbody>();
				newObject.AddComponent<Rigidbody>();
				rigidbody.mass = totalMassIfStatic;
				newObject.rigidbody.mass = totalMassIfStatic;
			}
			gameObject.rigidbody.mass *= .5f;
			newObject.rigidbody.mass *= .5f;
			gameObject.rigidbody.centerOfMass = transform.worldToLocalMatrix.MultiplyPoint3x4(gameObject.collider.bounds.center);
			newObject.rigidbody.centerOfMass = transform.worldToLocalMatrix.MultiplyPoint3x4(newObject.collider.bounds.center);
			
			newObject.GetComponent<ObjectDestruction>().Fracture(point,force,iterations);
		
			if(destroyAllAfterTime>=1){
				Destroy(newObject.GetComponent<MeshCollider>(), destroyAllAfterTime-1);
				Destroy(gameObject.GetComponent<MeshCollider>(), destroyAllAfterTime-1);
				Destroy(newObject, destroyAllAfterTime);
				Destroy(gameObject, destroyAllAfterTime);
			}
			//yield return null;//hmmm
		}
		if(totalMaxFractures == 0 || Vector3.Min(thisMeshFilter.mesh.bounds.size,minFractureSize)!=minFractureSize){
			if(destroySmallAfterTime>=1){
				Destroy(GetComponent<MeshCollider>(), destroySmallAfterTime-1);
				Destroy(gameObject, destroySmallAfterTime);
			}
			totalMaxFractures = 0;
		}
	}

	float UsedMass (Collision collision){
		if(collision.rigidbody!=null){
			if(gameObject.rigidbody!=null){
				if(collision.rigidbody.mass < gameObject.rigidbody.mass)
					return (float) collision.rigidbody.mass;
				else return (float) gameObject.rigidbody.mass;
			}
			else return (float) collision.rigidbody.mass;
		}
		else if(gameObject.rigidbody!=null) return (float)gameObject.rigidbody.mass;
		else return (float) 1.0f;
	}
	public void IgnoreCollision(Collider p1, Collider p2){
		Physics.IgnoreCollision(p1,p2);
	}
}
