### MIT License
### 
### Copyright (c) 2019 OpenAI
### 
### Permission is hereby granted, free of charge, to any person obtaining a copy
### of this software and associated documentation files (the "Software"), to deal
### in the Software without restriction, including without limitation the rights
### to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
### copies of the Software, and to permit persons to whom the Software is
### furnished to do so, subject to the following conditions:
### 
### The above copyright notice and this permission notice shall be included in all
### copies or substantial portions of the Software.
### 
### THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
### IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
### FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
### AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
### LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
### OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
### SOFTWARE.

# Project for IPC with Unity. Thanks!
# https://github.com/off99555/Unity3D-Python-Communication/
### Copyright (c) 2019 Nick Walton
### 
### Permission is hereby granted, free of charge, to any person obtaining a copy
### of this software and associated documentation files (the "Software"), to deal
### in the Software without restriction, including without limitation the rights
### to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
### copies of the Software, and to permit persons to whom the Software is
### furnished to do so, subject to the following conditions:
### 
### The above copyright notice and this permission notice shall be included in all
### copies or substantial portions of the Software.
### 
### THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
### IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
### FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
### AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
### LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
### OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
### SOFTWARE.

# Model used from AI Dungeon. Thanks!
# https://github.com/latitudegames/AIDungeon/
### MIT License
### 
### Copyright (c) 2018 Chanchana Sornsoontorn
### 
### Permission is hereby granted, free of charge, to any person obtaining a copy
### of this software and associated documentation files (the "Software"), to deal
### in the Software without restriction, including without limitation the rights
### to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
### copies of the Software, and to permit persons to whom the Software is
### furnished to do so, subject to the following conditions:
### 
### The above copyright notice and this permission notice shall be included in all
### copies or substantial portions of the Software.
### 
### THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
### IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
### FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
### AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
### LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
### OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
### SOFTWARE.

import time
import zmq

import json
import os
import warnings

import numpy as np

import tensorflow as tf
from src import encoder, model, sample
from utils import *

warnings.filterwarnings("ignore")

tf.compat.v1.logging.set_verbosity(tf.compat.v1.logging.ERROR)

def main():
    context = zmq.Context()
    socket = context.socket(zmq.REP)
    socket.bind("tcp://*:5555")
    
    generator = GPT2Generator()
    
    while True:
        message = socket.recv()
        message_data = json.load(message)
        
        print("Received request: %s" % message)
        
        model_text = generator.generate_raw(message_data["prompt"]);
        
        socket.send(model_text.encode('utf-8'))
    

class GPT2Generator:
    def __init__(self, generate_num=60, temperature=0.4, top_k=40, top_p=0.9, censor=True):
        self.generate_num = generate_num
        self.temp = temperature
        self.top_k = top_k
        self.top_p = top_p
        self.censor = censor

        self.model_name = "model_v5"
        self.model_dir = "."
        self.checkpoint_path = os.path.join(self.model_dir, self.model_name)

        models_dir = os.path.expanduser(os.path.expandvars(self.model_dir))
        self.batch_size = 1
        self.samples = 1

        self.enc = encoder.get_encoder(self.model_name, models_dir)
        hparams = model.default_hparams()
        with open(os.path.join(models_dir, self.model_name, "hparams.json")) as f:
            hparams.override_from_dict(json.load(f))
        seed = np.random.randint(0, 100000)

        config = tf.compat.v1.ConfigProto()
        config.gpu_options.allow_growth = True
        self.sess = tf.compat.v1.Session(config=config)

        self.context = tf.placeholder(tf.int32, [self.batch_size, None])
        # np.random.seed(seed)
        # tf.set_random_seed(seed)
        self.output = sample.sample_sequence(
            hparams=hparams,
            length=self.generate_num,
            context=self.context,
            batch_size=self.batch_size,
            temperature=temperature,
            top_k=top_k,
            top_p=top_p,
        )

        saver = tf.train.Saver()
        ckpt = tf.train.latest_checkpoint(os.path.join(models_dir, self.model_name))
        saver.restore(self.sess, ckpt)

    def prompt_replace(self, prompt):
        # print("\n\nBEFORE PROMPT_REPLACE:")
        # print(repr(prompt))
        if len(prompt) > 0 and prompt[-1] == " ":
            prompt = prompt[:-1]

        # prompt = second_to_first_person(prompt)

        # print("\n\nAFTER PROMPT_REPLACE")
        # print(repr(prompt))
        return prompt

    def result_replace(self, result):
        # print("\n\nBEFORE RESULT_REPLACE:")
        # print(repr(result))

        result = cut_trailing_sentence(result)
        if len(result) == 0:
            return ""
        first_letter_capitalized = result[0].isupper()
        result = result.replace('."', '".')
        result = result.replace("#", "")
        result = result.replace("*", "")
        result = result.replace("\n\n", "\n")
        # result = first_to_second_person(result)
        if self.censor:
            result = remove_profanity(result)

        if not first_letter_capitalized:
            result = result[0].lower() + result[1:]

        #
        # print("\n\nAFTER RESULT_REPLACE:")
        # print(repr(result))

        return result

    def generate_raw(self, prompt):
        context_tokens = self.enc.encode(prompt)
        generated = 0
        for _ in range(self.samples // self.batch_size):
            out = self.sess.run(
                self.output,
                feed_dict={
                    self.context: [context_tokens for _ in range(self.batch_size)]
                },
            )[:, len(context_tokens) :]
            for i in range(self.batch_size):
                generated += 1
                text = self.enc.decode(out[i])
        return text

    def generate(self, prompt, options=None, seed=1):

        debug_print = False
        prompt = self.prompt_replace(prompt)

        if debug_print:
            print("******DEBUG******")
            print("Prompt is: ", repr(prompt))

        text = self.generate_raw(prompt)

        if debug_print:
            print("Generated result is: ", repr(text))
            print("******END DEBUG******")

        result = text
        result = self.result_replace(result)
        if len(result) == 0:
            return self.generate(prompt)

        return result

if __name__ == "__main__":
    main()