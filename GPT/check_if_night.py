from textblob import TextBlob
from textblob.classifiers import NaiveBayesClassifier


def check_if_night(text, classifier=False, classifier_data_dir=''):
    if classifier:
        with open(classifier_data_dir, 'r', encoding='utf-8') as fp:
            classifier = NaiveBayesClassifier(fp, format='json')
            return classifier.classify(text) == 'pos'
    else:
        blob = TextBlob(text)
        # print(blob.tags)

        night_noun_list = ['moon', 'star', 'dark', 'night', 'midnight', 'darkness', 'blackness', 'serenade', 'sunset', 'glow']
        night_adj_list = ['dark']

        for i, (word, tag) in enumerate(blob.tags):
            word = word.lower()
            if tag == 'NN':
                if word in night_noun_list:
                    return True
            elif tag == 'NNS':
                if blob.words[i].singularize() in night_noun_list:
                    return True
            elif tag == 'JJ':
                if word in night_adj_list:
                    return True
        return False

if __name__ == '__main__':
    text = "You are on a quest to defeat the evil dragon of Larion. You've heard he lives up at the north of the kingdom. You set on the path to defeat him and walk into a dark forest."
    print(text)
    print(check_if_night(text, classifier=True, classifier_data_dir='GPT/data.json'))
    print(check_if_night(text))

# with open('night_desc_dataset.txt', 'r', encoding='utf-8') as fp:
#     desc_list = fp.readlines()
# sent_list = []
# for text in desc_list:
#     sent_list += re.split(r'[.!?]', text)
# sent_list = ["\"" + sent.strip() + "\"" for sent in list(filter(lambda x: x.strip(), sent_list))]
# print(sent_list[:5])


# with open('night.txt', 'r', encoding='utf-8') as fp:
#     pos = fp.readlines()

# with open('daytime.txt', 'r', encoding='utf-8') as fp:
#     neg = fp.readlines()

# dataset = []

# for sent in pos:
#     dataset.append({"text": sent, "label": "pos"})

# for sent in neg:
#     dataset.append({"text": sent, "label": "neg"})

# random.shuffle(dataset)

# dataset_json = json.dumps(dataset)

# with open('data.json', 'w', encoding='utf-8') as fw:
#     fw.write(dataset_json)

# with open('data.json', 'r', encoding='utf-8') as fp:
#     classifier = NaiveBayesClassifier(fp, format='json')

# print(classifier.classify(text))