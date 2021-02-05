using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

// Text animation types
public enum AnimationType { Wave, Jitter, Pause }

// This class records which text animations have been programmed into the text box
[System.Serializable]
public class TextAnimation
{
    public AnimationType AnimationType;
    public int StartIndex;
    public int EndIndex;
    public float curr_amplitude;
    public float currx_frequency;
    public float curry_frequency;
    public float prev_amplitude;
    public float prevx_frequency;
    public float prevy_frequency;
}

[RequireComponent(typeof(TMP_Parser))]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMProAnimator : MonoBehaviour
{
    // Inspector Assigned
    [Header("Defaults")]
    [SerializeField] private float _defaultAmplitudeMultiplier_CurrCoord;
    [SerializeField] private float _default_X_FrequencyMultiplier_CurrCoord;
    [SerializeField] private float _default_Y_FrequencyMultiplier_CurrCoord;
    [SerializeField] private float _defaultAmplitudeMultiplier_PrevCoord;
    [SerializeField] private float _default_X_FrequencyMultiplier_PrevCoord;
    [SerializeField] private float _default_Y_FrequencyMultiplier_PrevCoord;

    [Header("Text Animations")]
    [SerializeField] private List<TextAnimation> _textAnimations = new List<TextAnimation>();

    [Header("Typewriter Effect")]
    [SerializeField] private bool  _enableTypewriteEffect = true;
    [SerializeField] private float _letterWaitTime        = 0.05f;
    [SerializeField] private float _stopWaitTime          = 0.5f;

    // Private
    private TMP_Parser       _tmpParser      = null;
    private TMP_Text         _textComponent  = null;
    private TMP_TextInfo     _textInfo       = null;
    private string           _untaggedText   = "";
    private List<TaggedText> _processedText  = new List<TaggedText>();
    private List<TaggedText> _parsedText     = new List<TaggedText>();

    // Public Accessors
    public bool  EnableTypewriterEffect { get => _enableTypewriteEffect; set => _enableTypewriteEffect = value; }
    public float LetterWaitTime         { get => _letterWaitTime;        set => _letterWaitTime = value;        }
    public float StopWaitTime           { get => _stopWaitTime;          set => _stopWaitTime = value;          }
    
    // Methods
    private void Clear()
    {
        // Clear all lists and strings
        _textAnimations.Clear();
        _processedText.Clear();
        _parsedText.Clear();
        _untaggedText = "";
    }

    public void DisplaySentence(string text)
    {
        // Clear so that a new line can be displayed
        Clear();

        // Get the text component and set its text, also get the textInfo
        _textComponent = GetComponent<TMP_Text>();
        _textComponent.text = text;
        _textInfo = _textComponent.textInfo;

        // Parse the text to extract the types of commands entered
        _tmpParser = GetComponent<TMP_Parser>();
        _tmpParser.Parse();
        _parsedText = _tmpParser.ParsedText;

        // Just a global property for holding TextAnimation values
        TextAnimation textAnimation = null;

        // Loop over all of the parsed items
        int characterCount = 0;
        foreach (TaggedText taggedText in _parsedText)
        {
            // If it's a text field...
            if (taggedText.Tag == Tag.Text)
            {
                // Fill the text buffer with it and update characterCount
                _untaggedText += taggedText.Text;
                characterCount += taggedText.Text.ToCharArray().Length;

                // Because we handle pauses '.', as in '...', with different timing, we will
                // need to know separate these out and make separate tags for them
                string[] splitString = taggedText.Text.Split('.');

                // If a stop character '.' was actually found...
                if (splitString.Length > 1)
                {
                    // Loop over each of the split strings
                    foreach (string s in splitString)
                    {
                        // For the code segment below, consider the example sentence: "This is a test ... how was it?"
                        // If I call the split('.') function on this, I'll get four substrings:
                        //  (1) "This is a test "
                        //  (2) "" (empty)
                        //  (3) "" (empty)
                        //  (4) " how was it?"
                        // Note that the space between the ellipsis dots causes two empty strings...otherwise, the strings
                        // are non-empty. Thus, I can detect between standard text and pauses by testing if the string is
                        // empty...with that in mind, the code below should be clear
                        if (!string.IsNullOrEmpty(s.Trim()))
                        {
                            // Create a new TaggedText item, tagged as actual Text, and set its text equal to the
                            // substring from the Split('.') operation
                            TaggedText newTaggedText = new TaggedText();
                            newTaggedText = taggedText;
                            newTaggedText.Text = s;
                            _processedText.Add(newTaggedText);
                        }
                        else
                        {
                            // Create a new TaggedText item, tagged as a Pause, and set its text equal to a stop : '.'
                            TaggedText newPauseText = new TaggedText();
                            newPauseText = taggedText;
                            newPauseText.Text = ".";
                            newPauseText.Tag = Tag.Pause;
                            _processedText.Add(newPauseText);
                        }
                    }
                }
                else                
                    _processedText.Add(taggedText); // If no stops, just add the original taggedText
            }

            // If it's a TMPro built-in command, add it to the text buffer with angle brackets (<>) since TMPro itself will
            // parse and interpret it
            if (taggedText.Tag == Tag.TMPCommand && taggedText.Codeword == Codewords.built_in)
            {
                _untaggedText += "<" + taggedText.Text + ">";
                _processedText.Add(taggedText);
            }

            // If it's a TMProAnimator command, then we'll need to interpret what that means...but it won't be added to the
            // text buffer since TMPro will just interpret it as text
            if (taggedText.Tag == Tag.TMPCommand && taggedText.Codeword != Codewords.built_in && taggedText.Codeword != Codewords.none)
            {
                // Setup a TextAnimation based on settings that were parsed earlier by TMP_Parser
                switch (taggedText.Codeword)
                {
                    case Codewords.wave:
                        textAnimation = new TextAnimation();

                        textAnimation.curr_amplitude = taggedText.curr_amplitude != _tmpParser.DefaultValue ? taggedText.curr_amplitude : _defaultAmplitudeMultiplier_CurrCoord;
                        textAnimation.currx_frequency = taggedText.currx_frequency != _tmpParser.DefaultValue ? taggedText.currx_frequency : _default_X_FrequencyMultiplier_CurrCoord;
                        textAnimation.curry_frequency = taggedText.curry_frequency != _tmpParser.DefaultValue ? taggedText.curry_frequency : _default_Y_FrequencyMultiplier_CurrCoord;

                        textAnimation.prev_amplitude = taggedText.prev_amplitude != _tmpParser.DefaultValue ? taggedText.prev_amplitude : _defaultAmplitudeMultiplier_PrevCoord;
                        textAnimation.prevx_frequency = taggedText.prevx_frequency != _tmpParser.DefaultValue ? taggedText.prevx_frequency : _default_X_FrequencyMultiplier_PrevCoord;
                        textAnimation.prevy_frequency = taggedText.prevy_frequency != _tmpParser.DefaultValue ? taggedText.prevy_frequency : _default_Y_FrequencyMultiplier_PrevCoord;

                        textAnimation.AnimationType = AnimationType.Wave;
                        textAnimation.StartIndex = characterCount;

                        break;

                    case Codewords._wave:
                        textAnimation.EndIndex = characterCount;
                        _textAnimations.Add(textAnimation);
                        break;

                    case Codewords.jitter:
                        textAnimation = new TextAnimation();

                        textAnimation.curr_amplitude = taggedText.curr_amplitude != _tmpParser.DefaultValue ? taggedText.curr_amplitude : _defaultAmplitudeMultiplier_CurrCoord;
                        textAnimation.currx_frequency = taggedText.currx_frequency != _tmpParser.DefaultValue ? taggedText.currx_frequency : _default_X_FrequencyMultiplier_CurrCoord;
                        textAnimation.curry_frequency = taggedText.curry_frequency != _tmpParser.DefaultValue ? taggedText.curry_frequency : _default_Y_FrequencyMultiplier_CurrCoord;

                        textAnimation.prev_amplitude = taggedText.prev_amplitude != _tmpParser.DefaultValue ? taggedText.prev_amplitude : _defaultAmplitudeMultiplier_PrevCoord;
                        textAnimation.prevx_frequency = taggedText.prevx_frequency != _tmpParser.DefaultValue ? taggedText.prevx_frequency : _default_X_FrequencyMultiplier_PrevCoord;
                        textAnimation.prevy_frequency = taggedText.prevy_frequency != _tmpParser.DefaultValue ? taggedText.prevy_frequency : _default_Y_FrequencyMultiplier_PrevCoord;


                        textAnimation.AnimationType = AnimationType.Jitter;
                        textAnimation.StartIndex = characterCount;
                        break;

                    case Codewords._jitter:
                        textAnimation.EndIndex = characterCount;
                        _textAnimations.Add(textAnimation);
                        break;
                }
            }
        }

        // Now send the text in the text buffer to the text field of the actual TMPro component
        _textComponent.text = _untaggedText;

        // Handle enabling / disabling of Typewriter effect
        if (_enableTypewriteEffect)
        { 
            // If the typerwriter effect is enabled, set maxVisibleCharacters to zero and start a coroutine to display characters over time
            _textComponent.maxVisibleCharacters = 0;
            StartCoroutine(TypewriterEffect());
        }
        else
        {
            // Otherwise, just show all characters at once
            _textComponent.maxVisibleCharacters = _untaggedText.Length;
        }
    }
    
    private IEnumerator TypewriterEffect()
    {
        // Loop over each tagged text field in our processed text buffer
        foreach (TaggedText taggedText in _processedText)
        {
            int visibleCounter = 0;

            // If we are processing an item tagged as text...
            if (taggedText.Tag == Tag.Text)
            {
                // Loop over all characters in the text field...
                while (visibleCounter < taggedText.Text.Length)
                {
                    // and increment the number of visible characters, simulating a typewriter effect
                    visibleCounter++;
                    _textComponent.maxVisibleCharacters++;

                    // Pause before the next character by _letterWaitTime
                    yield return new WaitForSeconds(_letterWaitTime);
                }

                // Reset counter
                visibleCounter = 0;
            }

            // If the item is tagged as a pause...
            if (taggedText.Tag == Tag.Pause)
            {
                // display the character
                visibleCounter++;
                _textComponent.maxVisibleCharacters++;

                // Pause before the next character by _stopWaitTime
                yield return new WaitForSeconds(_stopWaitTime);
            }

            // If the item is tagged as a TMPro command, just return null
            if (taggedText.Tag == Tag.TMPCommand)
            {
                yield return null;
            }
        }

        yield return null;
    }
        
    private void Update()
    {
        if (_textAnimations == null || _textAnimations.Count == 0 || _textComponent == null)
            return;
        
        // Give control over updating TMPro mesh
        _textComponent.ForceMeshUpdate();
        
        // Loop over all of the text animations added above...
        foreach (TextAnimation textAnimation in _textAnimations)
        {
            // Loop over the character indices corresponding to the TextAnimation
            for (int i = textAnimation.StartIndex; i < textAnimation.EndIndex; ++i)
            {
                TMP_CharacterInfo characterInfo = _textInfo.characterInfo[i];

                // Only need to proceed if the character is actually visible...
                if (!characterInfo.isVisible)
                    continue;

                // Get mesh and vertex info for this character
                TMP_MeshInfo meshInfo = _textInfo.meshInfo[characterInfo.materialReferenceIndex];
                Vector3[] vertices = meshInfo.vertices;

                // Loop over the four vertices that define the mesh that holds the current character.
                // To animate the mesh, we have to animate all four of these vertices.
                for (int j = 0; j < 4; j++)
                {
                    // Get the vertex's starting (before animation) position
                    Vector3 startPosition = vertices[characterInfo.vertexIndex + j];
                    
                    // Apply the appropriate animation
                    switch (textAnimation.AnimationType)
                    {
                        // Wavy text - use a sinusoidal function to animate the characters
                        case AnimationType.Wave:
                            vertices[characterInfo.vertexIndex + j] = textAnimation.prev_amplitude * startPosition +
                                                                      textAnimation.curr_amplitude * new Vector3(Mathf.Sin(Time.time * textAnimation.currx_frequency +
                                                                                               startPosition.x * textAnimation.prevx_frequency), 
                                                                                               Mathf.Sin(Time.time * textAnimation.curry_frequency +
                                                                                               startPosition.y * textAnimation.prevy_frequency), 0);

                            break;

                        // Jitter text - use random offsets to create a jittering effect
                        case AnimationType.Jitter:
                            vertices[characterInfo.vertexIndex + j] = textAnimation.prev_amplitude * startPosition +
                                                                      textAnimation.curr_amplitude * new Vector3(Random.value * 2 * textAnimation.currx_frequency - textAnimation.currx_frequency + 
                                                                                                                 startPosition.x * textAnimation.prevx_frequency, 
                                                                                                                 Random.value * 2 * textAnimation.curry_frequency - textAnimation.curry_frequency + 
                                                                                                                 startPosition.y * textAnimation.prevy_frequency,
                                                                                                                 0);

                            break;
                    }
                }       
            }

            // Now actually update the mesh information on the text component
            for (int i = 0; i < _textInfo.meshInfo.Length; ++i)
            {
                TMP_MeshInfo meshInfo = _textInfo.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
                _textComponent.UpdateGeometry(meshInfo.mesh, i);
            }
        }
    }    
}