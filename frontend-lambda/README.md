# Frontend の Lambda@Edge

## Build

`.env` を作成してください。

| 環境変数名                 | 必須 | 説明                                         | 例                                                                                                                                                             |
| -------------------------- | ---- | -------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| COGNITO_REGION             | ✔    | Cognito のリージョン                         | us-east-1                                                                                                                                                      |
| COGNITO_USER_POOL_ID       | ✔    | Cognito のユーザープール ID                  | us-east-1_xxxxxxxxx                                                                                                                                            |
| JWKS                       | ✔    | Token の検証に必要な値                       | {"keys":[{"alg":"RS256","e":"AQAB","kid":"abcd=","kty":"RSA","n":"abc","use":"sig"},{"alg":"RS256","e":"AQAB","kid":"abc","kty":"RSA","n":"abc","use":"sig"}]} |
| REDIRECT_URL_ILLEGAL_TOKEN | ✔    | Token の検証に失敗した場合のリダイレクト URL | <https://example.com/auth/>                                                                                                                                      |
| ENVIRONMENT                |      | 環境                                         | Production                                                                                                                                                     |

`build.ps1` を実行してください。

```powershell
pwsh build.ps1
```
