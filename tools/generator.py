import argparse
from faker import Faker
from pprint import pprint
import json
import os

def output_file(contents, dir, filename):
  os.makedirs(dir, exist_ok=True)
  with open(os.path.join(dir, filename), 'w', encoding='utf-8') as f:
    f.write(contents)

def main():
  parser = argparse.ArgumentParser()
  parser.add_argument("--score", action='store_true')
  parser.add_argument("--output", "-o")
  parser.add_argument("--directory", "-d", default="./output/")

  args = parser.parse_args()

  output = args.output
  directory = args.directory

  fake_ja = Faker(locale='ja_JP')
  fake_en = Faker(locale='en_US')

  if args.score:
    score_list = []

    for i in range(10):
      score_list.append({
        "name": fake_en.word(),
        "title": " ".join(fake_ja.words(nb=5)),
        "description":fake_ja.texts()[0],
        "version_meta_urls": []
      })
    j = json.dumps(score_list, ensure_ascii=False, indent=2)
    print(j)

    if output:
      output_file(j, directory, output)

if __name__ == "__main__":
    main()
