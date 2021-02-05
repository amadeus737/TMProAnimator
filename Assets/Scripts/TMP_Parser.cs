using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Enumerations
// Note: Underscore '_' is used to indicate '/' since that character is not allowed in enums
public enum CaptureMode { None, TMPText, TMPCommand_Start, TMPCommand_End }
public enum Tag         { None, TMPCommand, Text, Pause }
public enum Codewords   { none, built_in, wave, _wave, jitter, _jitter }

// Class to record text, classify it by tag and codeword, and assign related parameters
[System.Serializable]
public class TaggedText
{
    public Tag Tag;
    public Codewords Codeword;
    public string Text;
    public float curr_amplitude;
    public float currx_frequency;
    public float curry_frequency;
    public float prev_amplitude;
    public float prevx_frequency;
    public float prevy_frequency;
}

// Helper structure to pass info between functions
public class Command
{
    public Codewords Codeword;
    public float curr_amplitude;
    public float currx_frequency;
    public float curry_frequency;
    public float prev_amplitude;
    public float prevx_frequency;
    public float prevy_frequency;
}

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMP_Parser : MonoBehaviour
{
    // Private
    private List<TaggedText> _parsedText   = new List<TaggedText>();
    private TextMeshProUGUI  _tmpComponent = null;
    private CaptureMode      _captureMode  = CaptureMode.None;
    private string           _currText     = "";
    private string           _currCommand  = "";
    private Codewords        _codeword     = Codewords.none;
    private float            _defaultValue = -999;

    // Public Accessors
    public List<TaggedText> ParsedText   { get => _parsedText;   }
    public float            DefaultValue { get => _defaultValue; }

    private void Clear()
    {
        // Clear lists, strings, and enumerations
        _parsedText.Clear();
        _captureMode = CaptureMode.None;
        _codeword    = Codewords.none;
        _currText    = "";
        _currCommand = "";
    }
    
    public void Parse()
    {
        // Clear so that we can re-parse
        Clear();

        // Get the TMPro component and set the capture mode to Text
        _tmpComponent = GetComponent<TextMeshProUGUI>();
        _captureMode = CaptureMode.TMPText;

        // Just to hold properties
        TaggedText taggedText = null;

        // No point in cotinuing if we don't have a text component
        if (_tmpComponent == null)
            return;
        
        // Loop over all characters in the text property of the text component
        for (int i = 0; i < _tmpComponent.text.Length; ++i)
        {
            // Get the current character
            char currChar = _tmpComponent.text[i];
         
            // Opening angle bracket (<) indicates start of command
            if (currChar == '<')
            {
                if (_captureMode == CaptureMode.TMPCommand_Start)
                    Debug.LogError("Error at character " + i + " : in opening tag - you must close the previous < before opening another tag");

                _captureMode = CaptureMode.TMPCommand_Start;
            }
            else if (currChar == '>') // end of command tag
            {
                if (_captureMode != CaptureMode.TMPCommand_Start)
                    Debug.LogError("Error at character " + i + " : no opening <");

                _captureMode = CaptureMode.TMPCommand_End;
            }
            else // can start recapturing text after command tag end
            {
                if (_captureMode == CaptureMode.TMPCommand_End)
                    _captureMode = CaptureMode.TMPText;
            }


            if (i == _tmpComponent.text.Length - 1 && _captureMode == CaptureMode.TMPText)
            {
                _currText += currChar.ToString();

                taggedText = new TaggedText();
                taggedText.Tag = Tag.Text;
                taggedText.Text = _currText;

                _parsedText.Add(taggedText);

                _currText = "";

                _captureMode = CaptureMode.None;
            }

            // Handle different capture modes
            switch(_captureMode)
            {
                // If at command start, add text after angle-bracket to command buffer
                case CaptureMode.TMPCommand_Start:
                    if (currChar != '<')
                        _currCommand += currChar.ToString();

                    // If there is text in the text buffer (filled before this command tag was opened),
                    // it is time to add that text as a tagged item to our list
                    if (!string.IsNullOrEmpty(_currText))
                    {
                        // Create the tagged text item
                        taggedText = new TaggedText();
                        taggedText.Tag = Tag.Text;
                        taggedText.Text = _currText;
                        taggedText.Codeword = Codewords.none;
                        taggedText.curr_amplitude = _defaultValue;
                        taggedText.prev_amplitude = _defaultValue;
                        taggedText.currx_frequency = _defaultValue;
                        taggedText.curry_frequency = _defaultValue;
                        taggedText.prevx_frequency = _defaultValue;
                        taggedText.prevy_frequency = _defaultValue;

                        // Add to our parsed list
                        _parsedText.Add(taggedText);

                        // Clear text buffer
                        _currText = "";
                    }

                    break;

                // If at command end, add text (not including angle-bracket) to command buffer
                case CaptureMode.TMPCommand_End:
                    if (currChar != '>')
                        _currCommand += currChar.ToString();

                    // Create the tagged command item
                    taggedText = new TaggedText();
                    taggedText.Tag = Tag.TMPCommand;
                    taggedText.Text = _currCommand;
                    
                    // Process the command
                    Command cmd;
                    ProcessCommand(_currCommand, out cmd);
                    
                    taggedText.Codeword = cmd.Codeword;
                    taggedText.curr_amplitude = cmd.curr_amplitude;
                    taggedText.prev_amplitude = cmd.prev_amplitude;
                    taggedText.currx_frequency = cmd.currx_frequency;
                    taggedText.curry_frequency = cmd.curry_frequency;
                    taggedText.prevx_frequency = cmd.prevx_frequency;
                    taggedText.prevy_frequency = cmd.prevy_frequency;

                    // Add to our parsed list
                    _parsedText.Add(taggedText);

                    // Clear command buffer
                    _currCommand = "";

                    break;

                // If we're capturing text, add it to the text buffer
                case CaptureMode.TMPText:
                    if (i != _tmpComponent.text.Length - 1)
                        _currText += currChar.ToString();

                    break;
            }
        }
    }

    private void ProcessCommand(string command, out Command cmd)
    {
        cmd = new Command();

        // In TMProAnimator commands, ':' is used to specify animation parameters, so we 
        // want to separate those out from the commands
        string[] splitStrings = command.Split(':');

        // Trim whitespace in all substrings
        foreach (string s in splitStrings)
        {
            s.Trim();
        }

        // See if there is a codeword match for this
        int i = FindCodewordMatch(splitStrings[0].Trim());
        
        // If we didn't find a codeword (i == -1), that means that it is a
        // TMPro built-in code
        if (i != -1)
            _codeword = (Codewords)i;
        else
            _codeword = Codewords.built_in;
        
        // Either a built-in TMP command or no parameters are defined
        if (splitStrings.Length == 1)
        {
            // In here, it just means we don't have a ':' in the command block.
            // Just record the extracted codeword as the command since there are
            // no additional parameters to process.
            cmd.Codeword = _codeword;
        }
        else // definitely a TMPAnim command
        {   
            // Split out parameters by commas
            string[] coordArgs = splitStrings[1].Split(',');

            // Trim out whitespace
            foreach (string s in coordArgs)
            {
                s.Trim();
            }

            // Extract parameters
            float a1 = _defaultValue;
            float a2 = _defaultValue;
            float fx1 = _defaultValue;
            float fx2 = _defaultValue;
            float fy1 = _defaultValue;
            float fy2 = _defaultValue;
            if (coordArgs.Length > 1)
            {
                a1 = float.Parse(coordArgs[0]);
                fx1 = float.Parse(coordArgs[1]);

                if (coordArgs.Length >= 3)
                    fy1 = float.Parse(coordArgs[2]);

                if (coordArgs.Length >=4)
                    a2 = float.Parse(coordArgs[3]);

                if (coordArgs.Length >= 5)
                    fx2 = float.Parse(coordArgs[4]);
                
                if (coordArgs.Length >= 6)
                    fy2 = float.Parse(coordArgs[5]);

                if (coordArgs.Length >= 7)
                    Debug.LogError("TOO MANY INPUT ARGS!");
            }

            // Save codeword
            cmd.Codeword = _codeword;

            // Record command parameters
            cmd.curr_amplitude = a1;
            cmd.currx_frequency = fx1;
            cmd.curry_frequency = fy1;
            cmd.prev_amplitude = a2;
            cmd.prevx_frequency = fx2;
            cmd.prevy_frequency = fy2;
        }
    }

    private int FindCodewordMatch(string codeword)
    {
        // Get number of elements in the enum
        int codewordEnumLength = System.Enum.GetNames(typeof(Codewords)).Length;

        // Loop over all elements in the enum
        for (int i = 0; i < codewordEnumLength; ++i)
        {
            string currCodeword = ((Codewords)i).ToString();
            currCodeword = currCodeword.Replace('_', '/');

            // If the passed-in codeword matches an element in the enum,
            // return its index
            if (codeword == currCodeword)
            {
                return i;
            }
        }

        // -1 indicates that the codeword was not found in the enum
        return -1;
    }
}
