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

## dotnet lambda を使用したデプロイ

以下のコマンドでデプロイを実施する

```bash
cd ./backend/PracticeManagerApi/src/PracticeManagerApi
dotnet lambda deploy-serverless
```

この時クラウドフォーメーションのスタック名とクラウドビルドしたバイナリなどを保管する S3 Bucket の名前を聞かれるので入力して実行する。

## Lambda のテスト

Lambda のテストを実行する場合は入力に API Gateway のテンプレートを使用する。
また、 Header で `{"Content-Type": "application/json"}` を指定する必要がある。


## 認証とトークンの保存について

```plantuml
@startuml
actor User

User -> Lambda@Edge: GET https://HOST/
Lambda@Edge --> User: response 401

User -> Cognito: GET https://SUB.auth.us-uast-1.amazoncognito.com/oauth2/authorize\n?response_type=code\n&client_id=xxxx\n&redirect_uri=https://HOST/callback\n&state=STATE\n&scope=openid+email\n&code_challenge_method=S256\n&code_challenge=CODE_CHALLENGE
Cognito --> User: Login Page

User -> Cognito: user and password
Cognito --> User: response 302 Location: https://HOST/callback?code=AUTHORIZATION_CODE

User -> Cognito: POST https://SUB.auth.us-east-1.amazoncognito.com/oauth2/token\nContent-Type='application/x-www-form-urlencoded'&\n\ngrant_type=authorization_code&\nclient_id=xxxx\ncode=AUTHORIZATION_CODE&\ncode_verifier=CODE_VERIFIER&\nredirect_uri=https://HOST/callback
Cognito --> User: response 200\n\n{\n    "access_token": "abcd",\n    "refresh_token": "efg",\n...\n}

User -> Lambda: POST https://auth.HOST/\n\n{\n    "access_token": "abcd",\n    "refresh_token": "efg",\n...\n}
Lambda --> User: response 200\n\nset-cookie: access_token=abcd; Domain=HOST; HttpOnly; Secure\nset-cookie: refresh_token=efg; Domain=HOST; HttpOnly; Secure

User -> Lambda@Edge: GET https://HOST/\ncookie: access_token=abcd; refresh_token=efg
Lambda@Edge -> CloudFront
CloudFront --> User: response 200
@enduml
```
