## テスト

```powershell
docker run -p 18000:8000 -d amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb
# docker run -p 18000:8000 -d amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb -inMemory

$env:DYNAMO_ENDPOINT="http://localhost:18000"
dynamodb-admin
```
