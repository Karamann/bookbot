from stats import get_num_words, get_char_count
from stats import sort_char_counts
import sys


def main():
    if len(sys.argv) != 2:
        print("Usage: python3 main.py <path_to_book>") 
        sys.exit(1)
    book_path =  sys.argv[1]
    text = get_book_text(book_path)
    num_words = get_num_words(text)
    print(f"Found {num_words} total words")
    
    char_count = get_char_count(text)
    sorted_chars= sort_char_counts(char_count)

    for item in sorted_chars:
        if item["char"].isalpha():
            print(f"{item['char']}: {item['num']}")


def get_book_text(path):
    with open(path) as f:
        return f.read()




main()
