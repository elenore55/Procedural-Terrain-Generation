using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    float movementSpeed = 85f;
    float rotationSpeed = 2f;
    const int MIN_CAMERA_HEIGHT = 40;
    const int MAX_CAMERA_HEIGHT = 200;
    static bool movementEnabled = true;
    bool rotationEnabled = true;
    public float maxRotationAngle = 80f;
    private Vector2 rotation;

    public static void EnableMovement(bool enabled) { movementEnabled = enabled; }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            rotationEnabled = !rotationEnabled;
        MoveCamera();
        RotateCamera();
    }

    void MoveCamera()
    {
        if (movementEnabled)
        {
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                Vector3 newPos = transform.position + new Vector3(movementSpeed * Time.deltaTime, 0, 0);
                if (newPos.y <= MAX_CAMERA_HEIGHT && newPos.y >= MIN_CAMERA_HEIGHT)
                    transform.Translate(new Vector3(movementSpeed * Time.deltaTime, 0, 0));
            }
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                Vector3 newPos = transform.position + new Vector3(-movementSpeed * Time.deltaTime, 0, 0);
                if (newPos.y <= MAX_CAMERA_HEIGHT && newPos.y >= MIN_CAMERA_HEIGHT)
                    transform.Translate(new Vector3(-movementSpeed * Time.deltaTime, 0, 0));
            }
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                Vector3 newPos = transform.position + new Vector3(0, 0, -movementSpeed * Time.deltaTime);
                if (newPos.y <= MAX_CAMERA_HEIGHT && newPos.y >= MIN_CAMERA_HEIGHT)
                    transform.Translate(new Vector3(0, 0, -movementSpeed * Time.deltaTime));
            }
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                Vector3 newPos = transform.position + new Vector3(0, 0, movementSpeed * Time.deltaTime);
                if (newPos.y <= MAX_CAMERA_HEIGHT && newPos.y >= MIN_CAMERA_HEIGHT)
                    transform.Translate(new Vector3(0, 0, movementSpeed * Time.deltaTime));
            }   
            if (Input.GetKey(KeyCode.Space))
                if (transform.position.y < MAX_CAMERA_HEIGHT)
                    transform.Translate(new Vector3(0, movementSpeed * Time.deltaTime), 0);
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                if (transform.position.y > MIN_CAMERA_HEIGHT)
                    transform.Translate(new Vector3(0, -movementSpeed * Time.deltaTime), 0);
        }
    }

    void RotateCamera()
    {
        if (rotationEnabled)
        {
            rotation.x += Input.GetAxis("Mouse X") * rotationSpeed;
            rotation.y -= Input.GetAxis("Mouse Y") * rotationSpeed;
            rotation.x = Mathf.Repeat(rotation.x, 360);
            rotation.y = Mathf.Clamp(rotation.y, -maxRotationAngle, maxRotationAngle);
            Camera.main.transform.rotation = Quaternion.Euler(rotation.y, rotation.x, 0);
        }
    }
}
