from GPT.generator.gpt2_generator import GPT2Generator
from GPT.generator.gpt2_generator_simple import GPT2GeneratorSimple
from GPT.utils.story_manager import StoryManager
from GPT.utils.utils import get_similarity, first_to_second_person


class GameManager:
    def __init__(self, generator='simple'):
        if generator == 'simple':
            self.generator =  GPT2GeneratorSimple()
        else:
            self.generator = GPT2Generator()
        self.story_manager = StoryManager(self.generator)
        self.prompt = "You are on a quest to defeat the evil dragon of Larion. You've heard he lives up at the north of the kingdom. You set on the path to defeat him and walk into a dark forest. You get into forest."
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
    
    def set_context(self, context):
        self.context = context

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
    gm = GameManager()
    opening = gm.start()
    print(opening)
    attack = "You kill dragon"
    words = '"Hi, dragon, I just come to be your friend!"'
    # result = gm.act(attack)
    result = gm.talk(words)
    print("> ", "You say " + words)
    print("result: |", result)


