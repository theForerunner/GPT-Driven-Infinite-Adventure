import stanza
from stanza.server import CoreNLPClient

class NLP:
    def __init__(self, end_point='http://localhost:9999', text=''):
        self.end_point = end_point
        try:
            with CoreNLPClient(
                endpoint=self.end_point,
                annotators=['tokenize','ssplit','pos','lemma','ner', 'parse', 'depparse','coref'],
                timeout=30000,
                memory='16G') as self.client:
                pass
        except:
            pass

    def parse(self, text):

        def tree2dict(parse_tree, dict_root):
            dict_root['value'] = parse_tree.value
            child_num = len(parse_tree.child)
            if child_num < 2 and parse_tree.value not in ['S', 'ADJP', 'NP']:
                dict_root['token'] = [parse_tree.child[0].value]
            else:
                dict_root['child'] = []
                dict_root['token'] = []
                for index, child in enumerate(parse_tree.child):
                    dict_root['child'].append({})
                    dict_root['token'] += tree2dict(child, dict_root['child'][index])
            return dict_root['token']

        sent_parse_list = []
        with CoreNLPClient(endpoint=self.end_point, start_server=stanza.server.StartServer.DONT_START) as client:
            ann = client.annotate(text)
        for sent in ann.sentence:
            constituency_parse = sent.parseTree
            parse_dict = {}
            tree2dict(constituency_parse.child[0], parse_dict)
            sent_parse_list.append(parse_dict)
        
        return sent_parse_list

    def get_location(self, sent):
        
        def check_if_tag_in_child(root, tag):
            for child in root['child']:
                if child['value'] == tag:
                    return True
            return False

        def get_child_with_tag(root, tag):
            result = []
            for child in root['child']:
                if child['value'] == tag:
                    result += child['token']
            return ' '.join(result)

        def get_loc(root, flag=False):
            loc = ''
            for child in root['child']:
                if child['value'] in ['NP'] and flag:
                    if check_if_tag_in_child(child, 'NP'):
                        loc = get_loc(child, True)
                    else:
                        loc = get_child_with_tag(child, 'NN')
                    break
                elif child['value'] in ['VP']:
                    loc = get_loc(child, True)
                    break
                elif child['value'] in ['PP']:
                    loc =  get_loc(child, True)
                    break
            return loc
            
        return get_loc(sent, False)


if __name__ == '__main__':
    nlp = NLP()
    text = "You are on a quest to defeat the evil dragon of Larion. You've heard he lives up at the north of the kingdom. You set on the path to defeat him and walk into a dark forest. You get into forest."
    d = nlp.parse(text)[1]
    print(d)
    loc = nlp.get_location(d)
    print(loc)