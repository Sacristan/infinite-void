#pragma warning disable 0414
#pragma warning disable 0219


using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(Rigidbody))]

public class MyPersonController : MonoBehaviour {
	enum CharacterState{
		Idle = 0,
		Walking = 1,
		Running = 2,
		Jumping = 3
	}
	bool fpsMode=false;
	
	CharacterController _controller;
	CharacterMotor _motor;
	Animator _anim;
	Transform _transform;
	
	AnimatorStateInfo currentBaseState;
	AnimatorStateInfo currentSecondaryState;
	CharacterState characterState;
	CapsuleCollider col;
	
	public float animSpeed = 1.0f;
	//MOVEMENT
	
	//mmm
	public float inAirControlAcceleration = 3.0f;
	public float jumpHeight = 0.5f;
	
	public float speedSmoothing = 10.0f;
	public float rotateSpeed = 250.0f;
	public bool useCameraTransform=true;
	public bool canClimb=true;
	public float minClimbHeight=1.5f;
	public float maxClimbHeight=2.51f;
	public float raycastStep=0.2f;

	float jumpRepeatTime = 0.05f;
	float jumpTimeout = 0.15f;
	float groundedTimeout = 0.25f;
	float lockCameraTimer = 0.0f;
	Vector3 moveDirection = Vector3.zero;
	float verticalSpeed = 0.0f;
	float moveSpeed = 0.0f;
	bool climbInitiated=false;
	CollisionFlags collisionFlags;
	bool isJumping=false;
	bool jumpingReachedApex=false;

	bool movingBack=false;
	bool isMoving=false;
	float walkTimeStart=0.0f;
	float lastJumpButtonTime = -10.0f;
	float lastJumpTime=-1.0f;

	float lastJumpStartHeight = 0.0f;
	Vector3 inAirVelocity = Vector3.zero;
	float lastGroundedTime = 0.0f;
	bool isControllable = true;
	
	bool recentlyHitCollider;
	float lastColliderHitTime=0.0f;

	//MECANIM
	public static int idleState = Animator.StringToHash("Base Layer.Idle");
	public static int walkState = Animator.StringToHash("Base Layer.Walk");
	public static int sprintState = Animator.StringToHash("Base Layer.Sprint");
	public static int jumpState = Animator.StringToHash("Base Layer.Jump");
	public static int climbState = Animator.StringToHash("Base Layer.ClimbHigh");
	public static int activateRifleState = Animator.StringToHash("Rifle.WeaponActivate");
	public static int shootRifleState = Animator.StringToHash("Rifle.Shoot");
	Vector3 lastPos;
		
	//MOTION VARIABLES
	float _horizontal;
	float _vertical;
	bool _canJump;
	bool _canSprint;
	bool _canClimb;
	bool _canShoot;
	bool _weaponActivated;
	
	public GameObject gun;
	public Transform spawnTr;
	public Transform climbraycaster;
	
	void Awake () {
		_controller = gameObject.GetComponent<CharacterController>();
		_motor = gameObject.GetComponent<CharacterMotor>();
		_anim = gameObject.GetComponent<Animator>();
		_transform = gameObject.transform;
		col = gameObject.GetComponent<CapsuleCollider>();
		_anim.SetLayerWeight(1,1);
		//moveDirection = transform.TransformDirection(Vector3.forward);
		moveDirection = _transform.TransformDirection(_transform.forward);
		lastPos = _transform.position;
	}
	void UpdateFPSMovement(){
		_motor.enabled=true;
		Vector3 directionVector = new Vector3(_horizontal,0,_vertical);
		if(directionVector!=Vector3.zero){
			float directionLength = directionVector.magnitude;
			directionVector = directionVector / directionLength;
			directionLength = Mathf.Min(1,directionLength);
			directionLength = directionLength * directionLength;
			directionVector = directionVector * directionLength;
		}
		_motor.inputMoveDirection = _transform.rotation*directionVector;
		_motor.inputJump = _canJump;
		
	}
	void UpdateSmootherMovementDirection(){
		_motor.enabled=false;
		
		Transform cameraTransform = Camera.main.transform;
		
		bool grounded = IsGrounded();
		Vector3 forward;
		if(useCameraTransform) forward = Camera.main.transform.TransformDirection(Vector3.forward);
		else  forward = _transform.TransformDirection(Vector3.forward);
		
		forward.y=0;
		forward = forward.normalized;
		Vector3 right = new Vector3(forward.z,0,-forward.x);

		if(_vertical<-0.1) movingBack = true;
		else movingBack = false;

		bool wasMoving = isMoving;
		isMoving = Mathf.Abs(_horizontal) > 0.1f || Mathf.Abs(_vertical) > 0.1f;
 		Vector3 targetDirection = _horizontal * right + _vertical * forward;

 		if(grounded){
 			lockCameraTimer += Time.deltaTime;
 			if(isMoving != wasMoving) lockCameraTimer=0.0f;
 			if(targetDirection!=Vector3.zero){
 				if(moveSpeed < _motor.walkMovement.maxForwardSpeed * 0.9f && grounded)
 					moveDirection = targetDirection.normalized;
 				else{
 					moveDirection = Vector3.RotateTowards(moveDirection,targetDirection,rotateSpeed*Mathf.Deg2Rad*Time.deltaTime,1000);
 					moveDirection = moveDirection.normalized;
 				}
 			}
 			float curSmooth = speedSmoothing * Time.deltaTime;
 			float targetSpeed = Mathf.Min(targetDirection.magnitude, 1.0f);
 			characterState = CharacterState.Idle;

 			if(_canSprint){
 				targetSpeed *= _motor.runMovement.maxForwardSpeed;
 				characterState = CharacterState.Running;
 			}
 			//else if(Time.time ) mmm Trot
 			else{
 				targetSpeed *= _motor.walkMovement.maxForwardSpeed;
 				characterState = CharacterState.Walking;
 			}

 			moveSpeed=Mathf.Lerp(moveSpeed, targetSpeed, curSmooth);
 			if(moveSpeed<_motor.walkMovement.maxForwardSpeed*0.3f) walkTimeStart = Time.time; //mmm
 		}
 		else{
 			if(isJumping) lockCameraTimer = 0.0f;
 			if(isMoving) inAirVelocity += targetDirection.normalized*Time.deltaTime*inAirControlAcceleration;
 		}
 	}
	
	bool TestClimbPossibility(){
		RaycastHit hit;
		
		if(Physics.Raycast(climbraycaster.position,_transform.forward,0.5f) || Physics.Raycast(_transform.position,_transform.forward,0.5f)){
			for(float incr = minClimbHeight; incr <maxClimbHeight;incr+=raycastStep){
				Vector3 origin = new Vector3(_transform.position.x,_transform.position.y+incr,_transform.position.z);
				Vector3 direction = _transform.forward;
				
				if(Physics.Raycast(origin,direction,out hit, 0.5f)){
					print("There is an obstacle in front at height of "+(origin.y - _transform.position.y)+" meters");
					
				}
				else{
					print ("No obstacle in front at height of "+(origin.y - _transform.position.y)+" meters");
					//return (origin.y - _transform.position.y);
					
					if(_canClimb && !climbInitiated){
						StartCoroutine(Climb(origin.y));
						return true;
					}
					else return false;
				}
			}
		}
		
		return false;
	}
	
	IEnumerator Climb(float targetPosition){
		climbInitiated=true;
		Messenger<bool>.Broadcast("freeze character completely",true);
		_motor.canControl=false;
		
		Vector3 entryTrPos = _transform.position;
		
		Vector3 newP1 = new Vector3(_transform.position.x,targetPosition-(_transform.localScale.y*1.7f),_transform.position.z);
		Vector3 newP2 = new Vector3(_transform.position.x,targetPosition,_transform.position.z);
		//Vector3 newP2 = new Vector3(_transform.position.x,targetPosition-(_transform.localScale.y*0.05f),_transform.position.z);
		
		iTween.MoveTo(gameObject,newP1,1.0f);
		yield return new WaitForSeconds(1.0f);
		iTween.MoveTo(gameObject,newP2,3.0f);
		yield return new WaitForSeconds(3.0f);
		
		Messenger<bool>.Broadcast("freeze character completely",false);
		_motor.canControl=true;
		climbInitiated=false;
	}

	void FeedMecanim(){
		//FEED Vars
		float tHorizontal;
		float tVertical;
		bool tCanJump=false;
		bool tCanClimb=false;
		bool tShoot=false;
		bool tRifleActivated = false;
		
		Vector3 velocity = (_transform.position - lastPos)/Time.deltaTime;
   		Vector3 localVelocity = _transform.InverseTransformDirection(velocity);
    	lastPos = _transform.position;
		
    	if(localVelocity.z < 0) tVertical = localVelocity.z/_motor.movement.maxBackwardsSpeed;
		else tVertical = localVelocity.z/_motor.movement.maxForwardSpeed;
		
    	tHorizontal = localVelocity.x/_motor.movement.maxSidewaysSpeed;
		
		tHorizontal = Mathf.Clamp(tHorizontal,-1.0f,1.0f);
		tVertical = Mathf.Clamp(tVertical,-1.0f,1.0f);
		
		_anim.speed = animSpeed;
		currentBaseState = _anim.GetCurrentAnimatorStateInfo(0);
//		currentSecondaryState = _anim.GetCurrentAnimatorStateInfo(1);
		
		if(currentBaseState.nameHash == sprintState || currentBaseState.nameHash == walkState){
			if(_canSprint && _motor.jumping.enabled)
				tCanJump=true;
		}
		else if (currentBaseState.nameHash == jumpState){
			if(!_anim.IsInTransition(0)) tCanJump=false;		
		}
		else{
			if(!_weaponActivated){
				if(canClimb && _canClimb&&TestClimbPossibility()){
					tCanClimb = true;
				}
			}
		}
		
		if (currentBaseState.nameHash==climbState){
			if(!_anim.IsInTransition(0))
				tCanClimb=false;
		}
		
		if(_canShoot)tShoot=true;
		else tShoot=false;
		
		_anim.SetFloat("direction",tHorizontal);
		_anim.SetFloat("speed",tVertical);
		_anim.SetBool("canSprint",_canSprint);
		_anim.SetBool("canJump",tCanJump);
		_anim.SetBool("canClimb",tCanClimb);
		_anim.SetBool("shoot",tShoot);
		_anim.SetBool ("rifleActivated",_weaponActivated);
		//_anim.SetBool("canShoot",tShoot);
		
		//NetworkViewID viewID = Network.AllocateViewID();
		
		
		if(Network.isServer || Network.isClient)		
			networkView.RPC("FeedMyMecanimRemotely",RPCMode.Others,Network.player,tHorizontal,tVertical,_canSprint,tCanJump,tCanClimb,tShoot,_weaponActivated);
		
		if(_canShoot){
			if(Network.isServer || Network.isClient) Network.Instantiate(Resources.Load("Bullet",typeof(Transform)),spawnTr.position,gun.transform.rotation,1);
			else Instantiate(Resources.Load("Bullet",typeof(Transform)),spawnTr.position,gun.transform.rotation);
			_canShoot=false;
		}
	}	
	
	void UpdateMotionVariables(){
		_horizontal = Input.GetAxis("Horizontal");
		_vertical = Input.GetAxis("Vertical");
		_canSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		_canJump = Input.GetKeyDown(KeyCode.Space);
		_canClimb = Input.GetKeyDown(KeyCode.E);
		_canShoot = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		if(Input.GetKeyDown(KeyCode.F)) _weaponActivated = !_weaponActivated;
	}
	void FixedUpdate () {
		UpdateMotionVariables();
		if(_motor.canControl){
	 		if(!fpsMode){
				_motor.enabled=false;
				if(!isControllable) Input.ResetInputAxes();
		 		if(Input.GetButton("Jump")) lastJumpButtonTime = Time.time;
		 		UpdateSmootherMovementDirection();
		 		ApplyGravity();
		 		ApplyJumping();
				//mmmm fps goes here
		 		Vector3 movement = moveDirection * moveSpeed + new Vector3(0, verticalSpeed, 0) + inAirVelocity;
		 		movement*=Time.deltaTime;
		 		collisionFlags = _controller.Move(movement);
	
		 		//mmm ANIMATIONPARTHERE
	
		 		if(IsGrounded()) _transform.rotation = Quaternion.LookRotation(moveDirection);
		 		else{
		 			Vector3 xzMove = movement;
		 			xzMove.y=0;
		 			if(xzMove.sqrMagnitude>0.001f) _transform.rotation = Quaternion.LookRotation(xzMove);
		 		}
		 		if(IsGrounded()){
		 			lastGroundedTime=Time.time;
		 			inAirVelocity=Vector3.zero;
		 			if(isJumping){
		 				isJumping=false;
		 				SendMessage("DidLand", SendMessageOptions.DontRequireReceiver);
		 			}
		 		}
		 	}
		 	else UpdateFPSMovement();
		}
		//networkView.RPC("SyncMyPosition",RPCMode.Others,Network.player,_transform.position);
		FeedMecanim();
		
	}
 	void ApplyJumping(){
 		if(lastJumpTime+jumpRepeatTime > Time.time) return;
 		if(IsGrounded()){
 			if(_motor.jumping.enabled&&Time.time<lastJumpTime+jumpTimeout){
 				verticalSpeed=CalculateJumpVerticalSpeed(jumpHeight);
 				SendMessage("DidJump", SendMessageOptions.DontRequireReceiver);
 			}
 		}
 	}
 	void ApplyGravity(){
 		if(isControllable){
 			bool jumpButton = Input.GetButton("Jump");
 			if(isJumping&&!jumpingReachedApex&&verticalSpeed<=0.0f){
 				jumpingReachedApex=true;
 				SendMessage("DidJumpReachApex", SendMessageOptions.DontRequireReceiver);
 			}
 			if(IsGrounded()) verticalSpeed=0.0f;
 			else verticalSpeed-=_motor.movement.gravity*Time.deltaTime;
 		}
 	}
 	float CalculateJumpVerticalSpeed(float targetJumpHeight){
 		return Mathf.Sqrt(2*targetJumpHeight*_motor.movement.gravity);
 	}
 	void DidJump(){
 		isJumping = true;
 		jumpingReachedApex=false;
 		lastJumpTime=Time.time;
 		lastJumpStartHeight=_transform.position.y;
 		lastJumpButtonTime=-10;
 		characterState=CharacterState.Jumping;
 	}
 	
 	void OnControllerColliderHit(ControllerColliderHit hit){
		recentlyHitCollider = true;
		lastColliderHitTime=Time.time;
 		if(hit.moveDirection.y>0.01f) return;
 	}
 	float GetSpeed(){
 		return moveSpeed;
 	}
 	bool IsJumping(){
 		return isJumping;
 	}
 	bool IsGrounded(){
 		return (collisionFlags & CollisionFlags.CollidedBelow) !=0;
 	}
 	Vector3 GetDirection(){
 		return moveDirection;
 	}
 	bool IsMovingBackwards(){
 		return movingBack;
 	}
	float GetLockCamerTimer(){
		return lockCameraTimer;
	}
	bool IsMoving(){
		return Mathf.Abs(Input.GetAxisRaw("Vertical")) + Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.5f;	
	}
	bool HasJumpReachedApex(){
		return jumpingReachedApex;
	}
	bool IsGroundedWithTimeout(){
		return lastGroundedTime + groundedTimeout > Time.time;
	}
	void Reset(){
		gameObject.tag="Player";
	}
	
	void FreezeMovement(bool p){
		_motor.canControl=p;
	}

	//Listeners and related functions
	void OnEnable(){
		Messenger<bool>.AddListener("fps got enabled",SetFPSMode);
		//Messenger<bool>.AddListener("freeze character completely",FreezeMovement);
	}
	void OnDisable(){
		Messenger<bool>.RemoveListener("fps got enabled",SetFPSMode);
		//Messenger<bool>.AddListener("freeze character completely",FreezeMovement);
	}
	void SetFPSMode(bool p){
		fpsMode=p;
	}
	
	
}