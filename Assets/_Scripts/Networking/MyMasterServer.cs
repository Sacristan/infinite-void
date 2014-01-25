#pragma warning disable 0414

using UnityEngine;
using System.Collections;

public class MyMasterServer : MonoBehaviour {	
	
	public string gameName = "You must change this";
	public int serverPort = 25002;
	public string sceneToLoad;
		
	float timeoutHostList = 0.0f;
	float lastHostListRequest = -1000.0f;
	float hostListRefreshTimeout = 10.0f;

	ConnectionTesterStatus connectionTestResult = ConnectionTesterStatus.Undetermined;
	bool filterNATHosts = false;
	bool probingPublicIP = false;
	bool doneTesting = false;
	float timer = 0.0f;
	bool useNat = false;
	
	Rect windowRect;
	Rect serverListRect;
	bool hideTest = false;
	string testMessage = "Undetermined NAT capabilities";
	bool canCreateServer=true;
	
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
	
	void MakeWindow (int id){
		//Debug.Log(id.ToString());
		hideTest = GUILayout.Toggle(hideTest, "Hide test info");
	
		if (!hideTest){
			GUILayout.Label(testMessage);
			if (GUILayout.Button ("Retest connection")){
				Debug.Log("Redoing connection test");
				probingPublicIP = false;
				doneTesting = false;
				connectionTestResult = Network.TestConnection(true);
			}
		}
	
		if (Network.peerType == NetworkPeerType.Disconnected){
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			
			if(canCreateServer){
				if (GUILayout.Button ("Start Server")){
					//static function InitializeServer (connections : int, listenPort : int, useNat : boolean) : NetworkConnectionError
					Network.InitializeServer(32, serverPort, useNat);
					//(gameTypeName : String, gameName : String, comment : String = "")
					MasterServer.RegisterHost(gameName, "Server", "");
					
					networkView.RPC( "LoadLevel", RPCMode.AllBuffered);
				}
			}
	
			// Refresh hosts
			if (GUILayout.Button ("Refresh available Servers") || Time.realtimeSinceStartup > lastHostListRequest + hostListRefreshTimeout){
				MasterServer.RequestHostList (gameName);
				lastHostListRequest = Time.realtimeSinceStartup;
			}
			
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
		else{
			if (GUILayout.Button ("Disconnect")){
				Network.Disconnect();
				MasterServer.UnregisterHost();
			}
			GUILayout.FlexibleSpace();
		}
		GUI.DragWindow (new Rect (0,0,1000,1000));
	}
	
	void MakeClientWindow(int id){
		GUILayout.Space(5);
	
		HostData[] data = MasterServer.PollHostList();
		//int count = 0;
		/*if (data.Length>0)
				canCreateServer=false;
		*/
		foreach (HostData element in data){
			GUILayout.BeginHorizontal();
			
			if ( !(filterNATHosts && element.useNat) ){
				string connections = element.connectedPlayers + "/" + element.playerLimit;
				GUILayout.Label(element.gameName);
				GUILayout.Space(5);
				GUILayout.Label(connections);
				GUILayout.Space(5);
				string hostInfo = "";
				
				// Indicate if NAT punchthrough will be performed, omit showing GUID
				if (element.useNat){
					GUILayout.Label("NAT");
					GUILayout.Space(5);
				}
				// Here we display all IP addresses, there can be multiple in cases where
				// internal LAN connections are being attempted. In the GUI we could just display
				// the first one in order not confuse the end user, but internally Unity will
				// do a connection check on all IP addresses in the element.ip list, and connect to the
				// first valid one.
				foreach (string host in element.ip)
					hostInfo = hostInfo + host + ":" + element.port + " ";
				
				//GUILayout.Label("[" + element.ip + ":" + element.port + "]");	
				GUILayout.Label(hostInfo);	
				GUILayout.Space(5);
				GUILayout.Label(element.comment);
				GUILayout.Space(5);
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Connect")){
					Network.Connect(element);
				}
			}
			GUILayout.EndHorizontal();	
		}
	}

	void Awake (){
		DontDestroyOnLoad(gameObject);
		windowRect = new Rect(Screen.width-300,0,300,100);
		serverListRect = new Rect(0, 0, Screen.width - windowRect.width, 100);
		connectionTestResult = Network.TestConnection();
		
		if (Network.HavePublicAddress())
			Debug.Log("This machine has a public IP address");
		else
			Debug.Log("This machine has a private IP address");
	}
	
	void Update(){
		if (!doneTesting)
			TestConnection();
	}
	
	void OnGUI (){
		if(!Network.isClient && !Network.isServer){
			windowRect = GUILayout.Window (0, windowRect, MakeWindow, "Server Controls");
			if (Network.peerType == NetworkPeerType.Disconnected && MasterServer.PollHostList().Length != 0)
				serverListRect = GUILayout.Window(1, serverListRect, MakeClientWindow, "Server List");
		}
		
		/*
		GUILayout.BeginVertical ("box");
			GUI.Label("Me-> "+Network.player.guid+" |IP: "+Network.player.ipAddress+" |exIP:"+ Network.player.externalIP);
			foreach(NetworkPlayer np in Network.connections){
				GUILayout.Label("Player-> "+np.guid+" |IP: "+np.ipAddress+" |exIP:"+ np.externalIP);
			}
		GUILayout.EndVertical();
		*/
	}
	
	void OnFailedToConnectToMasterServer(NetworkConnectionError info) {
		Debug.Log(info);
	}
	
	void OnFailedToConnect(NetworkConnectionError info) {
		Debug.Log(info);
	}
	
	[RPC]
	IEnumerator LoadLevel(){
		//Network.RemoveRPCsInGroup(0);
		//Network.RemoveRPCsInGroup(1);
		Network.SetSendingEnabled(0, false);
		Network.isMessageQueueRunning = false;
		
		Network.SetLevelPrefix(0);
		//Application.LoadLevel(sceneToLoad);
		yield return null;
		yield return null;
		Network.isMessageQueueRunning = true;
		Network.SetSendingEnabled(0, true);
		
		foreach (GameObject go in FindObjectsOfType(typeof(GameObject)))
			go.SendMessage("OnNetworkLoadedLevel", SendMessageOptions.DontRequireReceiver);
	}
	void OnDisconnectedFromServer (){
		Application.LoadLevel("Server");
	}
}
