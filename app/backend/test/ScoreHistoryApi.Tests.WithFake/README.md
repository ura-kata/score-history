## テスト

```powershell
docker run -p 18000:8000 -d amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb
# docker run -p 18000:8000 -d amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb -inMemory

$env:DYNAMO_ENDPOINT="http://localhost:18000"
dynamodb-admin
```


```powershell

docker run --name test_minio -p 19000:9000 -e "MINIO_ACCESS_KEY=minio_test" -e "MINIO_SECRET_KEY=minio_test_pass"  minio/minio server /data

```

http://localhost:19000/minio/


テスト用のバケットに以下を作成する。

- ura-kata-test-bucket

```powershell

# テスト用のプロファイルを作成しておく
aws configure --profile ura-kata-minio-test

aws --endpoint-url http://localhost:19000 --profile ura-kata-minio-test s3 mb s3://ura-kata-test-bucket

```
