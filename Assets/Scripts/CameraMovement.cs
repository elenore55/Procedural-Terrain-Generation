using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private float speed = 100.0f;
    private const int MIN_CAMERA_HEIGHT = 50;

    void Update()
    {
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            transform.Translate(new Vector3(0, 0, -speed * Time.deltaTime));
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            transform.Translate(new Vector3(0, 0, speed * Time.deltaTime));
        if (Input.GetKey(KeyCode.Space))
            transform.Translate(new Vector3(0, speed * Time.deltaTime), 0);
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            if (transform.position.y > MIN_CAMERA_HEIGHT)
                transform.Translate(new Vector3(0, -speed * Time.deltaTime), 0);
    }
}
