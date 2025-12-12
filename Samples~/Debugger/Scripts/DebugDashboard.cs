using TMPro;
using UnityEngine;

public class DebugDashboard : MonoBehaviour
{
    private const string Space = "      ";

    [SerializeField] private TMP_Text _debugMessageText;
    [SerializeField] private Transform _textHolder;
    [SerializeField] private float _interval = 0.05f;
    [SerializeField] private DebugElement[] elements;

    private TMP_Text[] _activeTexts;
    private float _elapsedTime = Mathf.Infinity;

    private void Start()
    {
        _activeTexts = new TMP_Text[elements.Length];

        _activeTexts[0] = _debugMessageText;
        for (int i = 1; i < elements.Length; i++)
            _activeTexts[i] = Instantiate(_debugMessageText, _textHolder);
    }

    private void Update()
    {
        if (_interval > 0)
        {
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime < _interval) return;
            _elapsedTime = 0;
        }

        for (int i = 0; i < elements.Length; i++)
            _activeTexts[i].text = elements[i].GetDebugMessage() + Space;
    }
}

public abstract class DebugElement : MonoBehaviour
{
    public abstract string GetDebugMessage();
}
