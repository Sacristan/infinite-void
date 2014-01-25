using UnityEngine;
using System.Collections;

public class TextureSize{
	public int width;
	public int height;

	public TextureSize(int pWidth, int pHeight){
		width = pWidth;
		height = pHeight;
	}
}

public class TerrainDeformation: MonoBehaviour{
	public bool randomizeDeformationTextures = true;
	public int explosionTexIdx=1;
	public int [] deformationTextureIDs;
	private Terrain terrain;
	private TextureSize heightmapSize;
	private TextureSize alphamapSize;
	private int numberOfAlphaLayers;
	private const float DEPTH_METER_CONVERT=0.05f;
	private const float TEXTURE_SIZE_MULTIPLIER=1.25f;
	private float [,] heightmapBackup;
	private float [, ,] alphamapBackup;

	void Start(){
		terrain=gameObject.GetComponent<Terrain>();
		heightmapSize = new TextureSize(terrain.terrainData.heightmapWidth,terrain.terrainData.heightmapHeight);
		alphamapSize = new TextureSize(terrain.terrainData.alphamapWidth,terrain.terrainData.alphamapHeight);
		numberOfAlphaLayers = terrain.terrainData.alphamapLayers;
		
		if (Debug.isDebugBuild){
            heightmapBackup = terrain.terrainData.GetHeights(0, 0, heightmapSize.width, heightmapSize.height);
            alphamapBackup = terrain.terrainData.GetAlphamaps(0, 0, alphamapSize.width, alphamapSize.height);   
        }
	}

	public void DeformTerrain(Vector3 position, float craterSizeInMeters, Texture tex = null, float texEdgeMultiplier = 1.5f){
		TerrainDeform(position,craterSizeInMeters);
		TextureDeform(position,craterSizeInMeters*texEdgeMultiplier);
	}

	void TerrainDeform(Vector3 position, float craterSizeInMeters){
		Vector3 terrainPosition = GetRelativeTerrainPositionFromPosition(position, heightmapSize);
		TextureSize heightmapCrater = new TextureSize(
			(int) (craterSizeInMeters * (heightmapSize.width / terrain.terrainData.size.x)),
			(int) (craterSizeInMeters * (heightmapSize.height / terrain.terrainData.size.z)));
		int heightmapStartPositionX = (int) (terrainPosition.x - (heightmapCrater.width/2));
		int heightmapStartPositionZ = (int) (terrainPosition.z - (heightmapCrater.height/2));

		float [,] heights = terrain.terrainData.GetHeights(heightmapStartPositionX, heightmapStartPositionZ, heightmapCrater.width, heightmapCrater.height);
		Vector2 circlePos;
		float distanceFromCenter;
		float depthMultiplier;

		float deformationDepth = (craterSizeInMeters / 3.0f) / terrain.terrainData.size.y;

		for (int i=0; i<heightmapCrater.height; i++){
			for (int j=0; j<heightmapCrater.width;j++){
				circlePos.x = (j - (heightmapCrater.width/2) /  (heightmapSize.width / terrain.terrainData.size.x));
				circlePos.y = (i - (heightmapCrater.height/2) /  (heightmapSize.height / terrain.terrainData.size.z));
				distanceFromCenter = Mathf.Abs(Mathf.Sqrt(circlePos.x * circlePos.x + circlePos.y * circlePos.y));
			
				if (distanceFromCenter < (craterSizeInMeters / 2.0f)){
					depthMultiplier = ((craterSizeInMeters / 2.0f - distanceFromCenter) / craterSizeInMeters / 2.0f) + .1f; 
					depthMultiplier += Random.value * .1f;
					depthMultiplier = Mathf.Clamp(depthMultiplier, 0, 1);
					heights[i,j] = Mathf.Clamp(heights[i,j] - deformationDepth * depthMultiplier, 0, 1);
				}
			}
		}
		terrain.terrainData.SetHeights(heightmapStartPositionX, heightmapStartPositionZ, heights);
	}

	void TextureDeform(Vector3 position, float craterSizeInMeters){
		Vector3 alphamapTerrainPosition = GetRelativeTerrainPositionFromPosition(position, alphamapSize);
		TextureSize alphamapCrater = new TextureSize(
			(int) (craterSizeInMeters * (alphamapSize.width / terrain.terrainData.size.x)),
			(int) (craterSizeInMeters * (alphamapSize.height / terrain.terrainData.size.z)));

		int alphamapStartPosX = (int)(alphamapTerrainPosition.x - (alphamapCrater.width / 2));
        int alphamapStartPosZ = (int)(alphamapTerrainPosition.z - (alphamapCrater.height/2));
		
		float[, ,] alphas = terrain.terrainData.GetAlphamaps(alphamapStartPosX, alphamapStartPosZ, alphamapCrater.width, alphamapCrater.height);
		float circlePosX;
        float circlePosY;
        float distanceFromCenter;

        for (int i=0;i<alphamapCrater.height;i++){
        	for(int j=0;j<alphamapCrater.width;j++){
        		circlePosX = (j - (alphamapCrater.width / 2)) / (alphamapSize.width / terrain.terrainData.size.x);
                circlePosY = (i - (alphamapCrater.height / 2)) / (alphamapSize.height / terrain.terrainData.size.z);
        	
                distanceFromCenter = Mathf.Abs(Mathf.Sqrt(circlePosX * circlePosX + circlePosY * circlePosY));
        	

        	if (distanceFromCenter < (craterSizeInMeters / 2.0f)){
        		for (int layerCounter=0;layerCounter<numberOfAlphaLayers;layerCounter++){
        			//if(layerCounter==deformationTextureIDs[0]){
        			//if(layerCounter==deformationTextureIDs[Random.Range(0, deformationTextureIDs.Length)]){
					if(layerCounter==explosionTexIdx){	
        				alphas[i,j,layerCounter]=1;
        			}
        			else{
        				alphas[i,j,layerCounter]=0;
        			}
        		}
        	}
        }
		}
        terrain.terrainData.SetAlphamaps(alphamapStartPosX, alphamapStartPosZ, alphas);
	}
	Vector3 GetRelativeTerrainPositionFromPosition(Vector3 position, TextureSize map){
		Vector3 tmpCoord = (position - terrain.gameObject.transform.position);
		Vector3 coord;
		
		coord.x = tmpCoord.x / terrain.terrainData.size.x;
		coord.y = tmpCoord.y / terrain.terrainData.size.y;
		coord.z = tmpCoord.z / terrain.terrainData.size.z;

		return new Vector3((coord.x * map.width), 0, (coord.z * map.height));
	}

	void OnApplicationQuit(){
        if (Debug.isDebugBuild){
            terrain.terrainData.SetHeights(0, 0, heightmapBackup);
            terrain.terrainData.SetAlphamaps(0, 0, alphamapBackup);
        }
    }
}