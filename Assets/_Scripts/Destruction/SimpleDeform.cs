using UnityEngine;
using System.Collections;

[RequireComponent (typeof (MeshFilter))]
[RequireComponent (typeof (MeshCollider))]


public class SimpleDeform : MonoBehaviour {
	public bool onCollision = true;
	public bool onCall = true;
	public bool updateCollider = false;
	public bool updateColliderOnBounce = false;

	public float minForce = 1.0f;
	public float multiplier = 0.1f;
	public float deformRadius = 1.0f;
	public float maxDeform = 0.0f;
	public float bounceBackSpeed =0.0f;
	public float bounceBackSleepCap = 0.001f;

	private Mesh mesh;
	private MeshFilter thisMeshFilter;
	private Vector3[] permaVerts;
	private bool sleep = true;

	void Start(){
		thisMeshFilter = GetComponent<MeshFilter>();
		mesh = thisMeshFilter.mesh;
		permaVerts = thisMeshFilter.mesh.vertices;
	}
	void OnCollisionEnter(Collision collision){
		Vector3[] vertices = mesh.vertices;
		if(onCollision && collision.relativeVelocity.magnitude >= minForce){
			sleep = false;
			Matrix4x4 tf = transform.worldToLocalMatrix;
			for(int i=0;i<vertices.Length;i++){
				foreach(ContactPoint contact in collision.contacts){
					Vector3 point = tf.MultiplyPoint(contact.point);
					Vector3 vec = tf.MultiplyVector(collision.relativeVelocity*UsedMass(collision));
					if((point-vertices[i]).magnitude < deformRadius){
						vertices[i] += vec*(deformRadius-(point-vertices[i]).magnitude)/deformRadius*multiplier;
						if(maxDeform>0 && (vertices[i]-permaVerts[i]).magnitude > maxDeform){
							vertices[i] = permaVerts[i] + (vertices[i]-permaVerts[i]).normalized*maxDeform;
						}
					}
				}
				
			}
			UpdateMesh(mesh,vertices,updateCollider);
		}
		
	}
	void Deform(Vector3 point, Vector3 direction){
		if(onCall && direction.magnitude >= minForce){
			sleep = false;
			Vector3[] vertices = mesh.vertices;
			Matrix4x4 tf = transform.worldToLocalMatrix;
			Vector3 thisPoint = tf.MultiplyPoint(point);
			Vector3 vec = tf.MultiplyVector(direction);
			for(int i=0;i<vertices.Length;i++){
				if((thisPoint-vertices[i]).magnitude <= deformRadius){
					vertices[i] += vec*(deformRadius-(thisPoint-vertices[i]).magnitude)/deformRadius*multiplier;
				}
			}
			UpdateMesh(mesh,vertices,updateCollider);
		}
		

		
	}
	void Update(){
		if(!sleep && bounceBackSpeed > 0){
			sleep=true;
			Vector3[] vertices = mesh.vertices;
			for(int i=0;i<vertices.Length;i++){
				vertices[i]+=(permaVerts[i] - vertices[i])*(Time.deltaTime*bounceBackSpeed);
				if((permaVerts[i]-vertices[i]).magnitude >= bounceBackSleepCap) sleep=false;
			}
			UpdateMesh(mesh,vertices,updateColliderOnBounce);
		}
	}

	void UpdateMesh(Mesh mesh, Vector3[] newVertices,bool updateCol){
		mesh.vertices = newVertices;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		
		if(updateCol) GetComponent<MeshCollider>().sharedMesh=mesh;
	}
	float UsedMass (Collision collision){
		if(collision.rigidbody){
			if(rigidbody){
				if(collision.rigidbody.mass < rigidbody.mass)
					return  (float)collision.rigidbody.mass;
				else return (float)rigidbody.mass;
			}
			else return (float)collision.rigidbody.mass;
		}
		else if(rigidbody) return (float)collision.rigidbody.mass;
		else return 1.0f;
	}
}
