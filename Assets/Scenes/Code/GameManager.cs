using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    //bazen renkler aynı olabiliyor onu değiştircem genel olarak bug yok gibi.
    // 5 parmak birden bastıgında renk farklı geliyor. ama bi parmagı sürekli basılı tut 2. parmagı bas çek bas çek yaparak aynı renk elde edebiliyorum.
    public static GameManager Instance { get; private set; }
    public GameObject fingerPrefab;
    private Dictionary<int, GameObject> fingers = new Dictionary<int, GameObject>();
    private List<int> activeTouches = new List<int>();
    private List<int> finishedTouches = new List<int>();
    private int selectedFingerId = -1;

    [SerializeField] private Color[] fingerColors;

    private Queue<Color> shuffledColors = new Queue<Color>();

    private bool gameEnded = false;
    private bool determiningWinner = false;
    private float allFingersPressedTime = 0f;
    private Camera mainCamera;
    private Color defaultCamColor;


    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        defaultCamColor = mainCamera.backgroundColor;
        
    }

    private void Start()
    { 
        ShuffleColors();
    }
    
    
    private void Update()
    {
        
        if (!gameEnded)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    Vector3 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                    touchPosition.z = 0f;

                    if (!fingers.ContainsKey(touch.fingerId))
                    {
                        var newFinger = Instantiate(fingerPrefab, touchPosition, Quaternion.identity);
                        var newFingerRenderer = newFinger.GetComponent<SpriteRenderer>();
                        newFingerRenderer.color = GetNextRandomColor();
                        fingers.Add(touch.fingerId, newFinger);
                        activeTouches.Add(touch.fingerId);
                    }
                }
                else if ((touch.phase == TouchPhase.Moved) && fingers.ContainsKey(touch.fingerId))
                {
                    if (selectedFingerId == -1 || touch.fingerId == selectedFingerId)
                    {
                        Vector3 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                        touchPosition.z = 0f;

                        fingers[touch.fingerId].transform.position = touchPosition;
                    }
                }
                else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) &&
                         fingers.ContainsKey(touch.fingerId))
                {
                    //fingers.Clear();
                    //activeTouches.Clear();
                    finishedTouches.Add(touch.fingerId);
                }
            }
            
            foreach (var fingerId in finishedTouches)
            {
                if (fingers.ContainsKey(fingerId))
                {
                    Destroy(fingers[fingerId]);
                    fingers.Remove(fingerId);
                }
                activeTouches.Remove(fingerId);
            }
            
            finishedTouches.Clear();
            
            if (activeTouches.Count >= 2 && selectedFingerId == -1 && !determiningWinner)
            {
                allFingersPressedTime += Time.deltaTime;

                if (allFingersPressedTime >= 2f)
                {
                    determiningWinner = true;
                    StartCoroutine(SelectWinnerAfterDelay(.1f));
                }
            }
            else
            {
                allFingersPressedTime = 0f;
            }
        }
        else
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                ResetGame();
            }
        }
    }

    private IEnumerator SelectWinnerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (activeTouches.Count >= 2 && selectedFingerId == -1)
        {
            gameEnded = true;
            
            selectedFingerId = activeTouches[Random.Range(0, activeTouches.Count)];
            Debug.Log("Selected Finger: Finger ID " + selectedFingerId);

            foreach (var fingerId in activeTouches)
            {
                if (fingerId != selectedFingerId)
                {
                    //loser fingers
                    fingers[fingerId].SetActive(false);
                }
                else
                {
                    //winner finger
                    Debug.Log(fingers[fingerId]);
                    mainCamera.backgroundColor = fingers[fingerId].GetComponent<SpriteRenderer>().color;
                    fingers[fingerId].transform.GetChild(0).gameObject.SetActive(true);
                    TriggerVibration();   
                    StartCoroutine(DelayedDestroy(fingers[fingerId]));
                }
            }

            activeTouches.Clear();
            finishedTouches.Clear();
            determiningWinner = false;
        }
    }

    private void ShuffleColors()
    {
        List<Color> tempList = new List<Color>(fingerColors);
        var n = tempList.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Range(0, n + 1);
            Color value = tempList[k];
            tempList[k] = tempList[n];
            tempList[n] = value;
        }
        shuffledColors = new Queue<Color>(tempList);
    }

    private Color GetNextRandomColor()
    {
        if (shuffledColors.Count == 0)
            ShuffleColors();

        return shuffledColors.Dequeue();
    }

    private IEnumerator DelayedDestroy(GameObject obj)
    {
        yield return new WaitForSeconds(2f);
        Destroy(obj);
        mainCamera.backgroundColor = defaultCamColor;

    }
    private void ResetGame()
    {
        foreach (var finger in fingers.Values)
        {
            Destroy(finger);
        }
        fingers.Clear();
        activeTouches.Clear();
        finishedTouches.Clear();
        selectedFingerId = -1;
        gameEnded = false;
        ShuffleColors();
    }
    public void TriggerVibration()
    {
        if (SystemInfo.supportsVibration)
        {
            Handheld.Vibrate();
        }
        Handheld.Vibrate();
    }
    
}