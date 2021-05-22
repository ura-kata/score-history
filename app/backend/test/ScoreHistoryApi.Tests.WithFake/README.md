## テスト

### Local DynamoDB

```powershell
docker run -p 18000:8000 -d amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb
# docker run -p 18000:8000 -d amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb -inMemory

$env:DYNAMO_ENDPOINT="http://localhost:18000"
dynamodb-admin
```


```powershell

docker run --name test_minio -p 19000:9000 -e "MINIO_ACCESS_KEY=minio_test" -e "MINIO_SECRET_KEY=minio_test_pass"  minio/minio server /data

```

#### create-table

```powershell
aws --endpoint-url http://localhost:18000 dynamodb create-table --table-name ura-kata-score-history --attribute-definitions AttributeName=owner,AttributeType=S AttributeName=score,AttributeType=S --key-schema AttributeName=owner,KeyType=HASH AttributeName=score,KeyType=RANGE --provisioned-throughput ReadCapacityUnits=1,WriteCapacityUnits=1

```

#### query

```powershell
aws --endpoint-url http://localhost:18000 dynamodb query --table-name ura-kata-score-history --key-condition-expression '#owner = :owner' --expression-attribute-values '{\":owner\":{\"S\":\"FQwk8i0PzkGUHWsXO66UwA==\"}}' --expression-attribute-names '{\"#owner\":\"owner\"}' --projection-expression '#owner'
```

#### delete-item

```powershell
aws --endpoint-url http://localhost:18000 dynamodb delete-item --table-name ura-kata-score-history --key '{\"owner\":{\"S\":\"FQwk8i0PzkGUHWsXO66UwA==\"},\"score\":{\"S\":\"\"}}'
```

### MinIO

http://localhost:19000/minio/


テスト用のバケットに以下を作成する。

- ura-kata-test-bucket

```powershell

# テスト用のプロファイルを作成しておく
aws configure --profile ura-kata-minio-test

aws --endpoint-url http://localhost:19000 --profile ura-kata-minio-test s3 mb s3://ura-kata-test-bucket

```
