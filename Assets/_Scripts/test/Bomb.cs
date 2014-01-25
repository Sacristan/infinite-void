using UnityEngine;
using System.Collections;

public class Bomb : MonoBehaviour
{
    public float additionalDownwardForce = 1000.0f;
    public GameObject explosion;

    public void Awake(){
        gameObject.rigidbody.AddForce(Vector3.down * additionalDownwardForce);
    }

    public void OnTriggerEnter(Collider other){
        if (explosion){
            Instantiate(explosion, gameObject.transform.position, gameObject.transform.rotation);
        }

        if (other.gameObject.GetComponent<TerrainDeformation>() != null)
        {
				print ( "D");
           other.gameObject.GetComponent<TerrainDeformation>().DeformTerrain(gameObject.transform.position,10);
        }
        Destroy(this.gameObject);
    }
}