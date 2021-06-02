# バックエンドについて

## やりたいこと

- 自分がアクセスできる楽譜のサマリーを一覧で取得する
- 1つの楽譜の詳細情報を取得する
  - スナップショットを指定してその楽譜の情報を取得する
- スナップショットを作成、削除する
- 公開、限定公開を変更する
- 指定した楽譜の情報を更新する
  - オブジェクトをアップデート、削除する
  - 楽譜の情報を更新する

## 楽譜の情報

- 楽譜のタイトル
- 楽譜の説明
- 楽譜の画像
- 楽譜の注釈
- スナップショット

タイトル、説明についてはサマリーとして一覧で取得できるようにする。

これらの情報は詳細情報を取得するリクエストで一回で取得できるようにしたい。

## データベースの構造

UUID の Base64 エンコードは 24 byte

### Score

- パーティションキー
  - owner_id
    - owner の uuid を base64 したもの
- ソートキー
  - score_id
    - 楽譜の uuid を base64 したもの
    - または
    - 楽譜の uuid と スナップショットの名前の base64 を concat したもの


```json
[
  {
    "owner_id": "uuid の base64",
    "score_id": "uuid の base64",

  }
]

```

#### スナップショット

スナップショットはその時点でのデータ構造を保存することができる。

スナップショットは UTF-8 の 64 文字以内で自由に付けることができる。

スナップショットの名前は DB に保存する際は base64 エンコードする。

#### 例

- owner
  - a5a3cceb-de61-43e4-849c-deafbf750bd7
    - 68yjpWHe5EOEnN6vv3UL1w==
- score
  - 9e97ad6b-c5fe-4a6e-8f25-0b265b59017a
    - a62Xnv7FbkqPJQsmW1kBeg==
- スナップショット
  - サンプル1
    - 44K144Oz44OX44OrMQ==
  - サンプル2
    - 44K144Oz44OX44OrMg==
- アイテム
  - 8f64ed24-7122-42d6-aeb9-c6eddc93b389
    - JO1kjyJx1kKuucbt3JOziQ==


```json
[
  {
    "o": "sc:68yjpWHe5EOEnN6vv3UL1w==",
    "s": "summary",
    "score_count": 1,
    "socres":["a62Xnv7FbkqPJQsmW1kBeg=="]
  },
  {
    "o": "sc:68yjpWHe5EOEnN6vv3UL1w==",
    "s": "main:a62Xnv7FbkqPJQsmW1kBeg==",
    "d_hash": "", // data 部分を JSON にした際のハッシュ値
    "create_at": "",
    "update_at": "",
    "access": "private",
    "s_count": 0, // snapshot count
    "data": {
      "title": "",
      "des_h": "s2hRmHZaGkOC7PDFrXzypw==",
      "v": "1", // data 構造の version
      "p_count": 3, // page count
      "page": [
        {
          "i": 0,
          "it": "JO1kjyJx1kKuucbt3JOziQ==",
          "o": "image.jpg", // object name
          "p": "1"
        },
        {
          "i": 1,
          "it": "JO1kjyJx1kKuucbt3JOziQ==",
          "o": "image.jpg",
          "p": "2"
        },
        {
          "i": 2,
          "it": "JO1kjyJx1kKuucbt3JOziQ==",
          "o": "image.jpg",
          "p": "3"
        }
      ],
      "a_count": 2, // annotation count
      "anno":[
        {
          "i": 0,
          "h": "ouWfeVUe4keu21CyOIZ0jg==" // Content のハッシュ値
        },
        {
          "i": 1,
          "h": "ouWfeVUe4keu21CyOIZ0jg=="
        }
      ]
    }
  },
  {
    "o": "sc:68yjpWHe5EOEnN6vv3UL1w==",
    "s": "snap:a62Xnv7FbkqPJQsmW1kBeg==G83UGGM9UUS4Ky8gsKmxRg==",
    "create_at": "",
    "update_at": "",
    "snapname": "スナップショット1"
  },
  {
    "o": "sc:68yjpWHe5EOEnN6vv3UL1w==",
    "s": "snap:a62Xnv7FbkqPJQsmW1kBeg==HdVwA45SOUacxgvNTADESA==",
    "create_at": "",
    "update_at": "",
    "snapname": "スナップショット2"
  }
]
```

Large Data Table

```json
{
  "o": "ld:68yjpWHe5EOEnN6vv3UL1w==", // owner id
  "s": "anno:a62Xnv7FbkqPJQsmW1kBeg==ouWfeVUe4keu21CyOIZ0jg==", // prefix + score id + アノテーションの内容にプレフィックスを付けハッシュ値を計算し base64 にしたもの
  "c": "アノテーションの内容", // content
},
{
  "o": "ld:68yjpWHe5EOEnN6vv3UL1w==", // owner id
  "s": "desc:a62Xnv7FbkqPJQsmW1kBeg==s2hRmHZaGkOC7PDFrXzypw==", // prefix + score id + アノテーションの内容にプレフィックスを付けハッシュ値を計算し base64 にしたもの
  "c": "説明内容", // content
}
```

prefix は以下とする

- anno:
  - アノテーション
- desc:
  - 説明

prefix は 5 文字とする

スナップショットは JSON に変換して S3 に保存する

### アイテム

- パーティションキー
  - owner_id
    - uuid の base64
- ソートキー
  - item_id
    - score_id の uuid の base64 と アイテムの uuid の base64 を concat したもの

#### 例

- owner
  - a5a3cceb-de61-43e4-849c-deafbf750bd7
    - 68yjpWHe5EOEnN6vv3UL1w==
- score
  - 9e97ad6b-c5fe-4a6e-8f25-0b265b59017a
    - a62Xnv7FbkqPJQsmW1kBeg==
- アイテム
  - 8f64ed24-7122-42d6-aeb9-c6eddc93b389
    - JO1kjyJx1kKuucbt3JOziQ==
  - 2b81d88e-9004-41cd-9441-fbe25eba46ee
    - jtiBKwSQzUGUQfviXrpG7g==
  - c56b0b81-d45a-4974-b4a0-395feed1eeef
    - gQtrxVrUdEm0oDlf7tHu7w==

```json
[
  {
    "o": "it:68yjpWHe5EOEnN6vv3UL1w==",
    "s": "summary",
    "size": 123456789 // owner に紐づくアイテムの総量
  },
  {
    "o": "it:68yjpWHe5EOEnN6vv3UL1w==",
    "s": "a62Xnv7FbkqPJQsmW1kBeg==JO1kjyJx1kKuucbt3JOziQ==", // score id + item id
    "obj_name": "", // S3 のオブジェクトの名前
    "size": 12345, // アイテムのバイトサイズ
    "t_size": 24690, // トータルサイズ
    "at": "",
    "type": "image", // アイテムのタイプ
    "org_name": "", // オリジナル名
    "thumbnail": {
      "obj_name": "",
      "size": 12345
    }
  }
]
```

- [DynamoDB Item Sizes and Formats - Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/CapacityUnitCalculations.html)

Snapshot と Item の関連を表すテーブル
Item を削除するときはこのテーブルにアイテムが存在しないことを確認する

```json
[
  {
    "o": "si:68yjpWHe5EOEnN6vv3UL1w==", // owner id
    "s": "JO1kjyJx1kKuucbt3JOziQ==a62Xnv7FbkqPJQsmW1kBeg==" // item relation メインとの関係  item id + score id
  },
  {
    "o": "si:68yjpWHe5EOEnN6vv3UL1w==", // owner id
    "s": "JO1kjyJx1kKuucbt3JOziQ==G83UGGM9UUS4Ky8gsKmxRg==" // item relation スナップショットとの関連  item id + snapshot id
  }
]


```
