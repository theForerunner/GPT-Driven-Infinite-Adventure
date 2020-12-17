from GPT.generator.gpt2_generator import GPT2Generator
from GPT.generator.gpt2_generator_simple import GPT2GeneratorSimple
from GPT.utils.story_manager import StoryManager
from GPT.utils.utils import get_similarity, first_to_second_person

from http.server import HTTPServer, BaseHTTPRequestHandler
import json

import regex as re
import textblob as tb
from textblob import wordnet

def detect_agents(text_response):
    
    agents = {
        'count': 0,
        'human': 0,
        'agentName': "other",
    }
    
    resTB = tb.TextBlob(text_response)
    
    for word, part in resTB.tags:
        if not "NN" in part:
            continue
        descs = tb.Word(word).get_synsets(pos = wordnet.NOUN)
        
        if len(descs) == 0:
            continue
            
        desc = descs[0].definition()
        
        print("%s: %s" % (word, desc))
        
        for pattern_list, is_human in [
            (["creature", "imaginary", "beast", "mammal", "living"], 0),
            (["person", "people", "band", "citizen", "man"], 1),
        ]:
            for pattern in pattern_list:
                if pattern in desc.split() or word == pattern:
                    if part == "NNS":
                        agents['count'] = 3
                    else:
                        agents['count'] = 1
                    agents['human'] = is_human
                    print("\tIs a %s!" % pattern)
                    
    return agents
    

def action_raw(request, response):
    response['textResponse'] = gm.generator.generate_raw(request['prompt'])
    
def action_observe_scene(request, response):
    location = "scenery"
    default_observation = "I look around at the %s." % location
    prompt = request['prompt'] if request['prompt'] else default_observation
    response['textResponse'] = gm.act(prompt)
    
    agents = detect_agents(response['textResponse'])
    response['key1'] = 'agentCount'
    response['val1'] = agents['count']
    response['key2'] = 'isHuman'
    response['val2'] = agents['human']
    
    
def action_switch_scenes(request, response):
    response['textResponse'] = gm.talk(request['prompt'])
    
def action_act(request, response):
    response['textResponse'] = gm.act(request['prompt'])
    
def action_talk(request, response):
    response['textResponse'] = gm.talk(request['prompt'])
    
def action_start(request, response):
    gm.set_context(request['context'])
    gm.set_prompt(request['prompt'])
    response['textResponse'] = gm.start()
    
class RequestHandler(BaseHTTPRequestHandler):
    def _set_headers(self, respCode = 200):
        self.send_response(respCode)
        self.send_header("Content-type", "application/json")
        self.end_headers()
        
    def do_GET(self):
        self.wfile.write(b"NO")

    def do_HEAD(self):
        self._set_headers()

    def do_POST(self):
        content_length = int(self.headers['Content-Length'])
        post_data = self.rfile.read(content_length).decode('utf-8')
        
        response = {
            'error':            "",
            'textResponse':     "",
            'key1':             "",
            'val1':             0.0,
            'key2':             "",
            'val2':             0.0,
            'key3':             "",
            'val3':             0.0,
        }
        
        print("Received request: %s" % post_data)
        
        try:
            message = json.loads(post_data)
        except Exception as err:
            response['error'] = "invalid JSON: %s" % err
            self._set_headers(500)
            self.wfile.write(json.dumps(response).encode('utf-8'))
            return
        
        
        action_map = {
            'start': {
                'requires': ['prompt', 'context'],
                'handler': action_start,
            },
            'observe_scene': {
                'handler': action_observe_scene,
            },
            'switch_scenes': {
                'handler': action_switch_scenes,
            },
            'act': {
                'requires': ['prompt'],
                'handler': action_act,
            },
            'talk': {
                'requires': ['prompt'],
                'handler': action_talk,
            },
            'raw': {
                'requires': ['prompt'],
                'handler': action_raw,
            },
        }
        
        
        if not message['action'] or not message['action'] in action_map:
            response['error'] = "missing or invalid action"
            self._set_headers(500)
            self.wfile.write(json.dumps(response).encode('utf-8'))
            return
        
        if 'requires' in action_map[message['action']]:
            for requirement in action_map[message['action']]['requires']:
                if not requirement in message or not type(message[requirement]) == str:
                    response['error'] = "%s requires %s" % (message['action'], requirement)
                    self._set_headers(500)
                    self.wfile.write(json.dumps(response).encode('utf-8'))
        
        action_map[message['action']]['handler'](message, response)
        
        self._set_headers()
        self.wfile.write(json.dumps(response).encode('utf-8'))
        


def run(server_class = HTTPServer, handler_class = RequestHandler, addr = "localhost", port = 8000):
    server_address = (addr, port)
    httpd = server_class(server_address, handler_class)

    print(f"Starting httpd server on {addr}:{port}")
    httpd.serve_forever()


def main():
    global gm
    gm = GameManager('complex')
    run(addr = '127.0.0.1', port = 8800)
    
    
class GameManager:
    def __init__(self, generator='simple'):
        if generator == 'simple':
            self.generator =  GPT2GeneratorSimple()
        else:
            self.generator = GPT2Generator()
        self.story_manager = StoryManager(self.generator)
        self.prompt = "You are on a quest to defeat the evil dragon of Larion. You've heard he lives up at the north of the kingdom. You set on the path to defeat him and walk into a dark forest. You get into the forest."
        self.context = "You are a knight living in the kingdom of Larion."

    def generate_result(self, action):
        result = self.story_manager.act(action)
        if len(self.story_manager.results) >= 2:
            similarity = get_similarity(
                self.story_manager.results[-1], self.story_manager.results[-2]
            )
            if similarity > 0.9:
                self.story_manager.actions = self.story_manager.actions[:-1]
                self.story_manager.results = self.story_manager.results[:-1]
                return "Woops that action caused the model to start looping. Try a different action to prevent that."
        return result

    def set_prompt(self, prompt):
        self.prompt = prompt
        return "OK"
    
    def set_context(self, context):
        self.context = context
        return "OK"

    def start(self):
        return self.story_manager.start_new_story(self.prompt, self.context)

    def talk(self, words):
        action = "You say " + words
        return self.generate_result(action)

    def act(self, action):
        action = action.strip()
        if "you" not in action[:6].lower() and "I" not in action[:6]:
            action = action[0].lower() + action[1:]
            action = "You " + action
        if action[-1] not in [".", "?", "!"]:
            action = action + "."
        action = first_to_second_person(action)
        action = "\n> " + action + "\n"
        return self.generate_result(action)

if __name__ == '__main__':
    main()


