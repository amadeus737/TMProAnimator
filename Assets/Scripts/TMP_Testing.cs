using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TMP_Testing : MonoBehaviour
{
    // Inspector Assigned
    [SerializeField] private TMProAnimator _tmpAnimator         = null;   
    [SerializeField] private bool          _initiateOnStart     = false;
    [SerializeField] private bool          _useTypewriterEffect = true;
    [TextArea(4, 10)]
    [SerializeField] private string _sentence = "You just made me <wave : 5, 0, -5, 1, 0, .01><b><color=orange><size=200%>look</b><color=white><size=100%></wave> " +
                                                "over there <size=200%>...<size=100%> and I saw a <jitter : 1, 3, 2, 1, 0, 0><b><color=yellow><size=150%>SUPER "     +
                                                "<color=blue>SCARY</b><color=white></jitter>ghost!";
    // Private
    private bool   _test                      = false;
    private bool   _testInitiated             = false;
    private string _lastSentence              = null;
    private bool   _lastTypewriterEffectValue = false;
   
    // Public Accessors
    public string Sentence { get => _sentence; set => _sentence = value; }
    public bool   Test     { get => _test;     set => _test = value;     }

    // Methods
    private void Start()
    {
        // Keep track of last typed sentence so that we know when to enable / disable test button
        _lastSentence              = _sentence;
        _lastTypewriterEffectValue = _useTypewriterEffect;

        if (_initiateOnStart)
            InitiateTest();
    }

    private void Update()
    {
        if (_test && _tmpAnimator != null)
        {
            InitiateTest();
        }

        // If we have changed the text, then, we can re-enable the test button
        if (_lastSentence != _sentence || _lastTypewriterEffectValue != _useTypewriterEffect)
        {
            _test = false;
            _testInitiated = false;
        }

        // Update last values for next iteration
        _lastSentence              = _sentence;
        _lastTypewriterEffectValue = _useTypewriterEffect;
    }

    private void InitiateTest()
    {
        // Only want to execute if we haven't already requested this line to be displayed
        if (_testInitiated)
            return;

        // Set whether or not we want the typewrite effect
        _tmpAnimator.EnableTypewriterEffect = _useTypewriterEffect;

        // Tell the TMProAnimator component to display the sentence
        _tmpAnimator.DisplaySentence(_sentence);

        _testInitiated = true;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TMP_Testing))]
class TMP_TestingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Get the target object
        TMP_Testing tmpTesting = (TMP_Testing)target;

        if (tmpTesting == null)
            return;

        // Draw default inspector
        DrawDefaultInspector();
               
        // Show button when allowed
        if (!tmpTesting.Test)
        {
            if (GUILayout.Button("Perform Test"))
            {
                tmpTesting.Test = true;
            }
        }
    }
}
#endif