import gpt_2_simple as gpt2
from GPT.utils.utils import cut_trailing_sentence

class GPT2GeneratorSimple:
    def __init__(self, generate_len=60, temperature=0.4, top_k=40, top_p=0.9) -> None:
        super().__init__()
        self.sess = gpt2.start_tf_sess()
        gpt2.load_gpt2(self.sess, checkpoint_dir='./generator/models/checkpoint')

        self.generate_len = generate_len
        self.temp = temperature
        self.top_k = top_k
        self.top_p = top_p

        self.if_first_run = True #for the first response (generating the initial setting) we want to impose less strict restrictions on output

    def prompt_replace(self, prompt):
        if len(prompt) > 0 and prompt[-1] == " ":
            prompt = prompt[:-1]
        return prompt

    def result_replace(self, result):
        result = cut_trailing_sentence(result)
        if len(result) == 0:
            return ""
        first_letter_capitalized = result[0].isupper()
        result = result.replace('."', '".')
        result = result.replace("#", "")
        result = result.replace("*", "")
        result = result.replace("\n\n", "\n")
        # result = first_to_second_person(result)

        if not first_letter_capitalized:
            result = result[0].lower() + result[1:]

    def generate_raw(self, prompt):
        result = gpt2.generate(self.sess, temperature=self.temp, prefix=prompt, length=self.generate_len, top_k=self.top_k, top_p=self.top_p, checkpoint_dir='./generator/models/checkpoint')
        return result if result else ""

    def generate(self, prompt):
        prompt = self.prompt_replace(prompt)
        result = self.generate_raw(prompt)
        result = self.result_replace(result)
        if len(result) == 0:
            return self.generate(prompt)
        return result