using UnityEngine;
using UnityEngine.UI;

public class RainMovement : MonoBehaviour
{
    private float speed = 80.0f;
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
        if (Input.GetKey(KeyCode.RightArrow))
            transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
        if (Input.GetKey(KeyCode.LeftArrow))
            transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        if (Input.GetKey(KeyCode.DownArrow))
            transform.Translate(new Vector3(0, 0, speed * Time.deltaTime));
        if (Input.GetKey(KeyCode.UpArrow))
            transform.Translate(new Vector3(0, 0, -speed * Time.deltaTime));
    }

    private void SwitchRain()
    {
        play = !play;
        if (play) rainSys.Play();
        else rainSys.Stop();
    }
}
