using Photon.Pun;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using DitzeGames.MobileJoystick;

[RequireComponent(typeof(CharacterController))]
public class PlayerNetworkMover : MonoBehaviourPunCallbacks, IPunObservable {

    [SerializeField]
    private Animator animator;
    [SerializeField]
    private GameObject cameraObject;
    [SerializeField]
    private GameObject gunObject;
    [SerializeField]
    private GameObject playerObject;
    [SerializeField]
    private NameTag nameTag;

    private Vector3 position;
    private Quaternion rotation;
    private float smoothing = 10.0f;

    private CharacterController characterController;
    private DitzeGames.MobileJoystick.Joystick joystick;
    private DitzeGames.MobileJoystick.Button jumpButton;
    private DitzeGames.MobileJoystick.TouchField touchField;

    private Vector3 moveDirection = Vector3.zero;
    private float gravity = 9.8f;
    private float jumpSpeed = 5.0f;
    private bool isJumping = false;

    void MoveToLayer(GameObject gameObject, int layer) {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform) {
            MoveToLayer(child.gameObject, layer);
        }
    }

    void Awake() {
        characterController = GetComponent<CharacterController>();

        if (photonView.IsMine) {
            cameraObject.SetActive(true);
            // Hide and lock the cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        joystick = FindObjectOfType<DitzeGames.MobileJoystick.Joystick>();
        jumpButton = FindObjectOfType<DitzeGames.MobileJoystick.Button>();
        touchField = FindObjectOfType<DitzeGames.MobileJoystick.TouchField>();

        if (joystick == null) {
            Debug.LogError("Joystick not found!");
        }
        if (jumpButton == null) {
            Debug.LogError("Jump Button not found!");
        }
        if (touchField == null) {
            Debug.LogError("Touch Field not found!");
        }
    }

    void Start() {
        if (photonView.IsMine) {
            MoveToLayer(gunObject, LayerMask.NameToLayer("Hidden"));
            MoveToLayer(playerObject, LayerMask.NameToLayer("Hidden"));
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players) {
                player.GetComponentInChildren<NameTag>().target = nameTag.transform;
            }
        } else {
            position = transform.position;
            rotation = transform.rotation;
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players) {
                if (player != gameObject) {
                    nameTag.target = player.GetComponentInChildren<NameTag>().target;
                    break;
                }
            }
        }
    }

    void Update() {
        if (!photonView.IsMine) {
            transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * smoothing);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * smoothing);
        } else {
            // Unlock the cursor if the Escape key is pressed
            if (Input.GetKeyDown(KeyCode.Escape)) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Lock the cursor again if the left mouse button is clicked
            // if (Input.GetMouseButtonDown(0)) {
            //     Cursor.lockState = CursorLockMode.Locked;
            //     Cursor.visible = false;
            // }
        }
    }

    void FixedUpdate() {
        if (photonView.IsMine) {
            if (joystick == null || jumpButton == null || touchField == null) {
                // Skip the update if any required component is missing
                return;
            }

            // Use joystick and keyboard for movement
            float horizontal = joystick.AxisNormalized.x + Input.GetAxis("Horizontal");
            float vertical = joystick.AxisNormalized.y + Input.GetAxis("Vertical");
            Vector3 move = new Vector3(horizontal, 0, vertical);
            move = transform.TransformDirection(move);
            move *= characterController.isGrounded ? 5.0f : 2.5f; // adjust speed accordingly

            if (characterController.isGrounded) {
                if ((jumpButton.Pressed || Input.GetButton("Jump")) && !isJumping) {
                    moveDirection.y = jumpSpeed;
                    isJumping = true;
                } else {
                    moveDirection.y = 0;
                }
            } else {
                isJumping = false;
                moveDirection.y -= gravity * Time.deltaTime;
            }

            characterController.Move((move + moveDirection) * Time.deltaTime);

            // Update animator parameters
            animator.SetFloat("Horizontal", horizontal);
            animator.SetFloat("Vertical", vertical);

            // Handle running
            animator.SetBool("Running", Input.GetKey(KeyCode.LeftShift));

            // Use touch field and mouse for looking around
            float lookHorizontal = touchField.TouchDist.x + Input.GetAxis("Mouse X");
            float lookVertical = touchField.TouchDist.y + Input.GetAxis("Mouse Y");
            transform.Rotate(0, lookHorizontal, 0);
            cameraObject.transform.Rotate(-lookVertical, 0, 0);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        } else {
            position = (Vector3)stream.ReceiveNext();
            rotation = (Quaternion)stream.ReceiveNext();
        }
    }
}
