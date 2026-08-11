using UnityEngine;
using UnityEngine.UI;

public class DisclaimerMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;

    [SerializeField] private Button understandButton;

    [SerializeField] private Button declineButton;

    private void Awake()
    {
        if (menuRoot == null)
            menuRoot = gameObject;

        if (understandButton != null)
            understandButton.onClick.AddListener(I_Understand);

        if (declineButton != null)
            declineButton.onClick.AddListener(Decline);
    }

    private void OnDestroy()
    {
        if (understandButton != null)
            understandButton.onClick.RemoveListener(I_Understand);

        if (declineButton != null)
            declineButton.onClick.RemoveListener(Decline);
    }

    public void I_Understand()
    {
        menuRoot.SetActive(false);
    }

    public void Decline()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
