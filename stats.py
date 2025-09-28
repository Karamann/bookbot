def get_num_words(text):
    return len(text.split())

def get_char_count(text):
    char_count = {}
    for char in text.lower():
        if char in char_count:
            char_count[char] += 1
        else:
            char_count[char] = 1
    return char_count

def sort_char_counts(char_count):
    items=[]
    for ch, count in char_count.items():
        items.append({"char":ch, "num":count})
    def sort_on(d):
        return d["num"]
    items.sort(key=sort_on, reverse=True)
    return items