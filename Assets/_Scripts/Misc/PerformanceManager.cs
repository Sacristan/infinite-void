using UnityEngine;
using System.Collections;

public class PerformanceManager : MonoBehaviour {
	
	float fps;
	int currentQuality;
	void Update () {
		QualitySettings.GetQualityLevel();
		fps = 1.0f / Time.smoothDeltaTime;
		if(fps>50.0f) QualitySettings.IncreaseLevel(false);
		else if (fps<15.0f) QualitySettings.DecreaseLevel(false);
	}
	
	void OnGUI() {
		GUI.Label(new Rect(Screen.width - 50,0,50,20),fps.ToString());
	}
	

}
