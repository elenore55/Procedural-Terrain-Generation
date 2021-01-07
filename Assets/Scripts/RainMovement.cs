using UnityEngine;
using UnityEngine.UI;

public class RainMovement : MonoBehaviour
{
    private ParticleSystem rainSys;
    private Button btnRain;
    private bool play = false;
    private Slider gravitySlider;

    private void Start()
    {
        rainSys = FindObjectOfType<ParticleSystem>();
        btnRain = FindObjectOfType<Button>();
        rainSys.Stop();
        btnRain.onClick.AddListener(SwitchRain);
        gravitySlider = GameObject.Find("Gravity").GetComponent<Slider>();
        gravitySlider.onValueChanged.AddListener(delegate { UpdateGravity(); });
    }

    private void SwitchRain()
    {
        play = !play;
        if (play)
        {
            float x = Camera.main.transform.position.x;
            float y = Camera.main.transform.position.y;
            float z = Camera.main.transform.position.z;
            rainSys.transform.position = new Vector3(x, y + 70, z);
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

    void UpdateGravity()
    {
        ParticleSystem.MainModule psMain;
        psMain = rainSys.main;
        psMain.gravityModifierMultiplier = gravitySlider.value;
    }
}
