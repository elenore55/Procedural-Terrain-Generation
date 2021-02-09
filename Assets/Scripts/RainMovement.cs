using UnityEngine;
using UnityEngine.UI;

public class RainMovement : MonoBehaviour
{
    ParticleSystem rainSystem;
    Button btnRain;
    bool play = false;
    static long numOfRaindrops = 0;

    public static void IncreaseNumOfRaindrops(long newRaindrops)
    {
        numOfRaindrops += newRaindrops;
    }

    private void Awake()
    {
        rainSystem = FindObjectOfType<ParticleSystem>();
        btnRain = FindObjectOfType<Button>();
    }

    private void Start()
    {
        rainSystem.Stop();
        btnRain.onClick.AddListener(SwitchRain);
    }

    private void SwitchRain()
    {
        play = !play;
        if (play)
        {
            float x = Camera.main.transform.position.x;
            float y = Camera.main.transform.position.y;
            float z = Camera.main.transform.position.z;
            rainSystem.transform.position = new Vector3(x, y + 70, z + 30);
            rainSystem.Play();
            CameraMovement.EnableMovement(false);
            btnRain.GetComponentInChildren<Text>().text = "Stop";
            btnRain.GetComponent<Image>().color = Color.red;
            InfiniteTerrain.rains = true;
            // RainCollision.Initialize(InfiniteTerrain.tileSize);
            numOfRaindrops = 0;
        }
        else
        {
            rainSystem.Stop();
            CameraMovement.EnableMovement(true);
            btnRain.GetComponentInChildren<Text>().text = "Rain";
            btnRain.GetComponent<Image>().color = new Color(0.13f, 0.37f, 0.765f);
            InfiniteTerrain.rains = false;
            InfiniteTerrain.startedNow = true;
            Debug.Log("Overall number of raindrops: " + numOfRaindrops);
        }
    }
}
