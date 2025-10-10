using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Mystery_ShellGameBindings : MonoBehaviour
{
    public Button stake25Btn;
    public Button stake50Btn;
    public Button stake100Btn;

    public Button[] targetButtons = new Button[3];
    public Transform[] targetTransforms = new Transform[3];

    public TMP_Text infoText;
    public TMP_Text stakeText;

    public static Mystery_ShellGameBindings Instance { get; private set; }
    private void Awake() => Instance = this;
}
