# TMProAnimator
Extends TextMeshPro by adding in text animations. This package works by accessing the character meshes that TextMeshPro is built on. These meshes are then animated in a number of different ways, and a simple typewriter effect is also included. Tagged commands entered into a TextMeshPro text component are automatically parsed and interpreted into text animations.

<b>Usage</b>:

On the TMP_Testing component in the Inspector, modify the sentence that you'd like displayed. Standard text and supported TextMeshPro tags are supported. You may also enter custom commands using the following template:

"<command : a0, f0x, f0y, a1, f1x, f1y> text <\/command>"

where,
- command = the command tag itself. Two commands are currently supported -> "wave" and "jitter"
- :       = the separator between the command tag and parameter fields
- a0      = the amplitude applied to the updated function evaluation
- f0x     = the frequency applied to the x-component of the updated function evaluation
- f0y     = the frequency applied to the y-component of the updated function evaluation
- a1      = the amplitude applied to the previous function evaluation
- f1x     = the frequency applied to the x-component of the previous function evaluation
- f1y     = the frequency applied to the y-component of the previous function evaluation
