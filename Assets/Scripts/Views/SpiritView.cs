using TMPro;
using UnityEngine;

public class SpiritView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text title;

    [SerializeField]
    private TMP_Text mana;

    [SerializeField]
    private GameObject wrapper;

    public Spirit Spirit { get; private set; }

    public void Setup(Spirit spirit)
    {
        Spirit = spirit;
        title.text = spirit.DisplayName;
        mana.text = spirit.Mana.ToString();
    }
}
