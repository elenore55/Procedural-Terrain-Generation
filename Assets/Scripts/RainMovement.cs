using UnityEngine;
using UnityEngine.UI;

public class RainMovement : MonoBehaviour
{
    // private float speed = 80.0f;
    private ParticleSystem rainSys;
    private Button btnRain;
    private bool play = false;

    private void Start()
    {
        rainSys = FindObjectOfType<ParticleSystem>();
        btnRain = FindObjectOfType<Button>();
        rainSys.Stop();
        btnRain.onClick.AddListener(SwitchRain);
    }

    void Update()
    {
        /*
        if (Input.GetKey(KeyCode.RightArrow))
            transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
        if (Input.GetKey(KeyCode.LeftArrow))
            transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        if (Input.GetKey(KeyCode.DownArrow))
            transform.Translate(new Vector3(0, 0, speed * Time.deltaTime));
        if (Input.GetKey(KeyCode.UpArrow))
            transform.Translate(new Vector3(0, 0, -speed * Time.deltaTime)); */
    }

    private void SwitchRain()
    {
        play = !play;
        if (play)
        {
            // ParticleSystem.ShapeModule editableShape = rainSys.shape;
            float x = Camera.main.transform.position.x;
            float y = Camera.main.transform.position.y;
            float z = Camera.main.transform.position.z;
            rainSys.transform.position = new Vector3(x, y + 70, z);
            // editableShape.position = new Vector3(x, y + 150, z - 30);
            // Debug.Log(x);
            // Debug.Log(y);
            // Debug.Log(rainSys.transform.position.z + "    " + Camera.main.transform.position.z);

            rainSys.Play();
            CameraMovement.movementEnabled = false;
            btnRain.GetComponentInChildren<Text>().text = "Stop";
            btnRain.GetComponent<Image>().color = Color.red;
        }
        else
        {
            rainSys.Stop();
            CameraMovement.movementEnabled = true;
            btnRain.GetComponentInChildren<Text>().text = "Rain";
            btnRain.GetComponent<Image>().color = new Color(0.13f, 0.37f, 0.765f);
        }
    }
}
