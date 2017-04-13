# UnityPCSS
Nvidia's PCSS soft shadow algorithm implemented in Unity3D.

# Description
"PCSS" or "Percentage Closer Soft Shadows" is a shadow sampling algorithm invented by Nvidia in 2005 ([Original Whitepaper](http://developer.download.nvidia.com/shaderlibrary/docs/shadow_PCSS.pdf)). The intent is to simulate a more realistic falloff where the shadows get progressively softer the further the receiver is from the caster.

This effect is easy to spot in real life, such as when looking at tree shadows:
![alt text](http://www.pictorem.com/collection/900_455535.jpg "Photo of a real tree shadow on snow")

I thought this photo was really cool, so I did a quick recreation as a test:
![alt text](https://pbs.twimg.com/media/C9R9LQ3V0AAXqBo.jpg "I think it looks fairly close for like 5 minutes of work haha")


# Current Limitations
Currently only works with "Directional" light sources, as it's primarily an override of the "Screen Space Shadows" shader in the graphics settings, but I'm looking into the possibility of directly overriding the actual "UnityShadowLibrary.cginc" to affect "Spot" and "Point" lights if possible.

# Version Compatibility
The shadows themselves do work with **BOTH 5.5 and 5.6** (no modifications necessary, it automatically detects and adapts to either version), but as this is a **Unity 5.6 project**, the materials in the demo scene do not import correctly in 5.5. There are only 3 or so materials that need their albedo/metallic/specular textures assigned from the nearby "Textures" folder, so it's not that bad, but if anybody has any ideas to fix this, please let me know! :)

# Future Development
This asset and any improvements will remain free (both as in beer and speech), but if you wish to support me spending extra time on this, you can donate to me through PayPal: paypal.me/TheMasonX. NOTE: Please do not feel pressured to donate it's just an option if anyone wishes to support me spending more time on this, and less on my game, which I can't even use it for.

I obviously didn't do this to make money though; instead, I'd prefer support in the form of you guys contributing back any fixes/improvements, so that we can all benefit :) I made it Open Source rather than just a free asset because I believe that if we work together, we can create something truly amazing. Thanks for the interest, and I hope you guys get some good use out of it! Let me know if you have any issues or ideas, and I'll respond to pull requests ASAP.
