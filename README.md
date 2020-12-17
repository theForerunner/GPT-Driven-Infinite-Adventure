# GPT-Driven Infinite Adventure
## Overview
There are many narratives in traditional RPGs including plot and conversations. To offer an immersive story which will react to players' choices, it will be a huge workload. However, players may still find that they are not offered the choice they want or their choice have little impact on the development of the story. Also, fixed story line and repetitive conversations often make these games not replayable.

To address this issue, we have made a game that use GPT-2 to generate the plot and conversations. Player can decide their own actions and lines and the system will respond according to players' choices.
## Installation
* Python environment: python 3.7
* Python libraries:
  * numpy
  * tensorflow-gpu == 1.14 or 1.15
  * difflib
  * textblob
  * gpt-2-simple
  * stanza
 * GPT-2 model: 
   * download the model [torrent](docs/gpt2_model.torrent)
   * put it under GPT-Driven-Infinite-Adventure/GPT/generator/models folder
 * Unity Project:
   * Install Unity Hub and add /GPT-Adventure-Unity directory as project
   * Install and configure project with editor version 2020.2.0b1
## Execution
### GPT-2 Server
 * Make sure no firewall rules or other restrictions limit communication on localhost port 8800
 * Enter GPT-Driven-Infinite-Adventure/GPT folder
 * excute the following line in command line
   ```
   python gpt2_module.py
   ```
 * Open Unity project in editor and press play while python model is running.
 
