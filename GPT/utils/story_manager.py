from GPT.utils.utils import cut_trailing_sentence

class StoryManager:
    def __init__(self, generator):
        self.generator = generator
        # self.story = None
        self.context = ''
        self.actions = []
        self.results = []
        self.choices = []
        self.memory = 20

    def start_new_story(self, story_prompt, context=""):
        # result = self.generator.generate(context + story_prompt)
        # result = cut_trailing_sentence(result)
        self.story_start = context + story_prompt
        self.context = context
        self.actions = []
        self.results = []
        self.choices = []
        return self.get_story()

    def story_context(self):
        mem_ind = self.memory
        if len(self.results) < 2:
            latest_result = self.story_start
        else:
            latest_result = self.context
        while mem_ind > 0:
            if len(self.results) >= mem_ind:
                latest_result += self.actions[-mem_ind] + self.results[-mem_ind]
            mem_ind -= 1
        return latest_result

    def get_story(self):
        story_list = [self.story_start]
        for i in range(len(self.results)):
            story_list.append("\n" + self.actions[i] + "\n")
            story_list.append("\n" + self.results[i])
        return "".join(story_list)

    def act(self, action_choice):
        result = self.generate_result(action_choice)
        self.actions.append(action_choice)
        self.results.append(result)
        return result

    def generate_result(self, action):
        result = self.generator.generate(self.story_context() + action)
        return result