#pragma warning disable 0414

using UnityEngine;
using System.Collections;
//using System.Collections.Generic;

public class Inventory : MonoBehaviour {
	[System.Serializable]
	class InventoryItem{
		[HideInInspector]
		public string name;
		[HideInInspector]
		public Texture itemTexture;
	}
	
	[System.Serializable]
	class InventorySlot{
		[HideInInspector]	
		public string name;
		[HideInInspector]
		public Texture icon;
		[HideInInspector]
		public Rect slotRect;
	}
	
	static Inventory thisInventory;
	
	InventoryItem[] inventory;
	InventoryItem[] equipped;
	
	//LAYOUT
	public int inventoryWidth=20;
	public int inventoryLength=20;
	public int iconWidthHeight;
	public int spacing;
	public Vector2 offset;
	
	public Texture emptySlot;
	bool isInventoryWinOpen=false;
	
	Rect inventoryWindow;
	public Texture testTexture;
	
	void Awake(){
		thisInventory = this;
		
		inventoryWindow = new Rect(270,40,180,195);
		//lootWin
		//charWin
		
		inventory = new InventoryItem[inventoryLength];
		for (int i = 0;i<inventory.Length;i++) inventory[i]=null;
		
		equipped = new InventoryItem[12];//mmm
		for (int j=0;j<equipped.Length;j++) equipped[j]=null;
		
		//equiptmentslot iteration / declaration
		//inventory[0].itemTexture=testTexture;
	}
	
	//VISUALISATION
	void OnGUI(){
		if(isInventoryWinOpen){
			GUI.Window(1,inventoryWindow,DrawInventoryWindow,"Inventory");
		}
	}
	
	void Update(){
		if(Input.GetKeyUp(KeyCode.I)){
			isInventoryWinOpen = !isInventoryWinOpen;
			Screen.lockCursor=!isInventoryWinOpen;
		}
		
	}
	
	//WINDOWS ARE DRAWED HERE (Called from OnGUI)
	void DrawInventoryWindow(int windowID){
		if(GUI.Button(new Rect(5,5,10,10),"")){
			isInventoryWinOpen=false;
		}
		int j;
		int k;
		
		InventoryItem currInventoryItem;
		Rect currRect;
		for(int i =0;i<inventory.Length;i++){
			j=i/inventoryWidth;
			k=i%inventoryWidth;
			currInventoryItem = inventory[i];
			currRect = (new Rect (offset.x + k * (iconWidthHeight + spacing), offset.y + j * (iconWidthHeight + spacing), iconWidthHeight, iconWidthHeight));
			if(currInventoryItem == null) GUI.DrawTexture(currRect,emptySlot);
			else GUI.DrawTexture(currRect,currInventoryItem.itemTexture);
			
			//mmmm
			/*
			if(currInventoryItem!=null && GUI.Button(currRect,"",GUIStyle("label"))){
				if(Input.GetMouseButtonUp(0)){
					//openLootWindow = false;
					//openCharWin=false;
				}
			}
			*/
		}
	}
	void ResizeInventory(int newInventoryLength){
		InventoryItem[] oldInventory = inventory;
		
		inventory = new InventoryItem[newInventoryLength];
		for(int i=0;i<oldInventory.Length;i++) inventory[i]=oldInventory[i];
		for(int j=oldInventory.Length;j<inventory.Length;j++) inventory[j]=null;
	}
	//FUNCTIONALITY
	/*
	void EquipItem(InventoryItem item){
		for(int i =0;i<equipped.Length;i++){
		}
	}
	*/
}
