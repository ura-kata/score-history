# Practice Manager

演奏会の練習をサポートするためのサービス

## 機能

- 楽譜の書き込みのバージョン管理
- 楽譜の書き込みの共有
- 楽譜への簡易書き込み

## アーキテクチャ

![architecture.d.svg](./designs/architecture.d.svg)

## S3 の構造

- ${bucket_name}
  - ${score_name}
    - meta.json
    - versions
      - ${version_number(/\d{5}/)}
        - ${yyyyMMddHHmmssfff}
          - version.json
          - comments
            - ${page_number}
              - ${yyyyMMddHHmmssfff}
                - comment.json
    - images
      - ${UUID}.${ext}
