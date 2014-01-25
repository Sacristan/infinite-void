#pragma warning disable 0414

using UnityEngine;
using System.Collections;

public class NetworkManager : MonoBehaviour {
	public string gameName = "IV_TEST";
	public int serverPort = 25001;
	public string sceneToLoad="SandBox";
	public string serverScene="Lobby";
	public bool initializeSecurity=true;

	float timeoutHostList = 0.0f;
	float lastHostListRequest = -1000.0f;
	float hostListRefreshTimeout = 10.0f;

	ConnectionTesterStatus connectionTestResult = ConnectionTesterStatus.Undetermined;
	bool filterNATHosts = false;
	bool probingPublicIP = false;
	bool doneTesting = false;
	float timer = 0.0f;
	bool useNat = false;

	string testMessage="Undetermined NAT capabilities";
	bool canCreateServer=true;

	void Awake(){
		DontDestroyOnLoad(gameObject);
		connectionTestResult = Network.TestConnection();

		if(Network.HavePublicAddress()) Debug.Log("This machine has a public IP address");
		else Debug.Log("This machine has a private IP address");
	}

	void OnGUI(){
		GUI.skin = Resources.Load("myGUISkin",typeof(GUISkin)) as GUISkin;
		if(!Network.isClient && !Network.isServer){
			GUILayout.BeginHorizontal("box");
			if(canCreateServer){
				if(GUILayout.Button("Start Server")) StartServer();
			}
			if(GUILayout.Button("Refresh Servers")|| Time.realtimeSinceStartup > lastHostListRequest + hostListRefreshTimeout) RefreshServers();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		
			HostData[] hostData = MasterServer.PollHostList();
			if(hostData.Length>0){
				foreach (HostData element in hostData){
					GUILayout.BeginHorizontal();
						string info = "Game Name: "+element.gameName+" "+element.connectedPlayers+"/"+element.playerLimit;
						if(GUILayout.Button(info)) Network.Connect(element);
					GUILayout.EndHorizontal();
				}
			}
		GUI.Label(new Rect(0,Screen.height-25,Screen.width,25),testMessage);	
		}
	}

	void StartServer(){
		if(initializeSecurity) Network.InitializeSecurity();
		Network.InitializeServer(32, serverPort, useNat);
		MasterServer.RegisterHost(gameName, "Server", "");
		networkView.RPC("LoadThisLevel", RPCMode.AllBuffered,sceneToLoad);
	}
	void RefreshServers(){
		MasterServer.RequestHostList (gameName);
		lastHostListRequest = Time.realtimeSinceStartup;
	}
	void Update(){
		if(!doneTesting) TestConnection();
	}

	[RPC]
	IEnumerator LoadThisLevel(string levelName){
		//Network.RemoveRPCsInGroup(0);
		//Network.RemoveRPCsInGroup(1);
		Network.SetSendingEnabled(0, false);
		Network.isMessageQueueRunning = false;
		
		Network.SetLevelPrefix(0);
		
		Application.LoadLevel(levelName);
		yield return null;
		yield return null;
		Network.isMessageQueueRunning = true;
		Network.SetSendingEnabled(0, true);
		
		foreach (GameObject go in FindObjectsOfType(typeof(GameObject)))
			go.SendMessage("OnNetworkLoadedLevel", SendMessageOptions.DontRequireReceiver);
	}
	
	void TestConnection(){
		connectionTestResult = Network.TestConnection();
		
		switch(connectionTestResult){
			
		case ConnectionTesterStatus.Error:
			testMessage="Problem determining NAT capabilities";
			doneTesting=true;
		break;
		
		case ConnectionTesterStatus.Undetermined:
			testMessage = "Undetermined NAT capabilities";
			doneTesting = false;
		break;
			
		case ConnectionTesterStatus.PublicIPIsConnectable:
			testMessage = "Directly connectable public IP address.";
			useNat = false;
			doneTesting = true;
		break;
			
		case ConnectionTesterStatus.PublicIPPortBlocked:
			testMessage = "Non-connectble public IP address (port " + serverPort +" blocked), running a server is impossible.";
			useNat = false;
			
			if (!probingPublicIP){
				Debug.Log("Testing if firewall can be circumvented");
				connectionTestResult = Network.TestConnectionNAT();
				probingPublicIP = true;
				timer = Time.time + 10;
			}
			
			else if (Time.time > timer){
				probingPublicIP = false; 		// reset
				useNat = true;
				doneTesting = true;
			}
		break;
		
		case ConnectionTesterStatus.PublicIPNoServerStarted:
			testMessage = "Public IP address but server not initialized, it must be started to check server accessibility. Restart connection test when ready.";
		break;
			
		case ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted:
			Debug.Log("LimitedNATPunchthroughPortRestricted");
			testMessage = "Limited NAT punchthrough capabilities. Cannot connect to all types of NAT servers.";
			useNat = true;
			doneTesting = true;
		break;
					
		case ConnectionTesterStatus.LimitedNATPunchthroughSymmetric:
			Debug.Log("LimitedNATPunchthroughSymmetric");
			testMessage = "Limited NAT punchthrough capabilities. Cannot connect to all types of NAT servers. Running a server is ill adviced as not everyone can connect.";
			useNat = true;
			doneTesting = true;
		break;
		
		case ConnectionTesterStatus.NATpunchthroughAddressRestrictedCone:
		
		break;
			
		case ConnectionTesterStatus.NATpunchthroughFullCone:
			Debug.Log("NATpunchthroughAddressRestrictedCone || NATpunchthroughFullCone");
			testMessage = "NAT punchthrough capable. Can connect to all servers and receive connections from all clients. Enabling NAT punchthrough functionality.";
			useNat = true;
			doneTesting = true;
			break;

		default: 
			testMessage = "Error in test routine, got " + connectionTestResult;	
			break;
		}
	}

	//messages
	void OnFailedToConnectToMasterServer(NetworkConnectionError info) {
		Debug.Log(info);
	}
	
	void OnFailedToConnect(NetworkConnectionError info) {
		Debug.Log(info);
	}
	void OnDisconnectedFromServer (){
		Application.LoadLevel(serverScene);
	}
}
