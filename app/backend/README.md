# バックエンドについて

## データベースの構造

UUID の Base64 エンコードは 24 byte

### Score

- パーティションキー
  - owner_id
    - uuid の base64
- ソートキー
  - score_id
    - uuid の base64
```json
[
  {
    "owner_id": "uuid の base64",
    "score_id": "uuid の base64",

  }
]


```
