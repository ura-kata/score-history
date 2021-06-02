import * as cdk from '@aws-cdk/core';
import * as path from 'path';
import * as dotenv from 'dotenv';
import { ScoreHistoryBackendScoreDynamoDb } from './ScoreHistoryBackendScoreDynamoDb';
import { ScoreHistoryBackendScoreItemDynamoDb } from './ScoreHistoryBackendScoreItemDynamoDb';
import { ScoreHistoryBackendScoreLargeDataDynamoDb } from './ScoreHistoryBackendScoreLargeDataDynamoDb';
import { ScoreHistoryBackendScoreDataBucket } from './ScoreHistoryBackendScoreDataBucket';
import { ScoreHistoryBackendScoreItemRelationDynamoDb } from './ScoreHistoryBackendScoreItemRelationDynamoDb';
import { OriginAccessIdentity } from '@aws-cdk/aws-cloudfront';
import { ScoreHistoryBackendPrivateItemLambdaEdgeFunction } from './ScoreHistoryBackendPrivateItemLambdaEdgeFunction';
import { ScoreHistoryBackendPrivateItemDistribution } from './ScoreHistoryBackendPrivateItemDistribution';
import { HostedZone } from '@aws-cdk/aws-route53';
import { ScoreHistoryBackendPrivateItemARecord } from './ScoreHistoryBackendPrivateItemARecord';

dotenv.config();

/** 楽譜データを格納する DynamoDB のテーブル */
const SCORE_DYNAMODB_TABLE_NAME = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_SCORE_DYNAMODB_TABLE_NAME as string;

if (!SCORE_DYNAMODB_TABLE_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_DYNAMODB_TABLE_NAME' is not found."
  );
}
/** 楽譜アイテムデータのメタ情報を格納する DynamoDB のテーブル */
const SCORE_ITEM_DYNAMODB_TABLE_NAME = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_SCORE_ITEM_DYNAMODB_TABLE_NAME as string;

if (!SCORE_ITEM_DYNAMODB_TABLE_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_ITEM_DYNAMODB_TABLE_NAME' is not found."
  );
}
/** 楽譜のアイテムの関係を保存する DynamoDB のテーブル */
const SCORE_ITEM_RELATION_DYNAMODB_TABLE_NAME = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_SCORE_ITEM_RELATION_DYNAMODB_TABLE_NAME as string;

if (!SCORE_ITEM_RELATION_DYNAMODB_TABLE_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_ITEM_RELATION_DYNAMODB_TABLE_NAME' is not found."
  );
}
/** 楽譜データの大きいデータを格納する DynamoDB のテーブル */
const SCORE_LARGE_DATA_DYNAMODB_TABLE_NAME = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_SCORE_LARGE_DATA_DYNAMODB_TABLE_NAME as string;

if (!SCORE_LARGE_DATA_DYNAMODB_TABLE_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_LARGE_DATA_DYNAMODB_TABLE_NAME' is not found."
  );
}
/** 楽譜のデータを格納する S3 バケット */
const SCORE_DATA_S3_BUCKET = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_SCORE_DATA_S3_BUCKET as string;

if (!SCORE_DATA_S3_BUCKET) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_DATA_S3_BUCKET' is not found."
  );
}
/** ドメイン名 */
const DOMAIN_NAME = process.env.URA_KATA_APP_DOMAIN_NAME as string;
if (!DOMAIN_NAME) {
  throw new Error("'URA_KATA_APP_DOMAIN_NAME' is not found.");
}
/** 楽譜のアイテムのプライベート CDN のホスト名 */
const HISTORY_BACKEND_PRIVATE_ITEM_HOST_NAME = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_PRIVATE_ITEM_HOST_NAME as string;

if (!HISTORY_BACKEND_PRIVATE_ITEM_HOST_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_PRIVATE_ITEM_HOST_NAME' is not found."
  );
}
/** Certificate Arn */
const URA_KATA_CERTIFICATE_ARN = process.env.URA_KATA_CERTIFICATE_ARN as string;
if (!URA_KATA_CERTIFICATE_ARN) {
  throw new Error("'URA_KATA_CERTIFICATE_ARN' is not found.");
}
/** HOST Zone の ID */
const URA_KATA_PUBLIC_HOSTED_ZONE_ID = process.env
  .URA_KATA_PUBLIC_HOSTED_ZONE_ID as string;
if (!URA_KATA_PUBLIC_HOSTED_ZONE_ID) {
  throw new Error("'URA_KATA_PUBLIC_HOSTED_ZONE_ID' is not found.");
}

export class ScoreHistoryBackendStack extends cdk.Stack {
  scoreDynamoDbTableArn: string;
  scoreItemDynamoDbTableArn: string;
  scoreLargeDataDynamoDbTableArn: string;
  scoreHistoryBackendScoreDataBucketArn: string;
  scoreItemRelationDynamoDbTableArn: string;

  constructor(scope: cdk.Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const scoreDynamoDbTable = new ScoreHistoryBackendScoreDynamoDb(
      this,
      'ScoreHistoryBackendScoreDynamoDb',
      SCORE_DYNAMODB_TABLE_NAME
    );

    this.scoreDynamoDbTableArn = scoreDynamoDbTable.tableArn;

    const scoreItemDynamoDbTable = new ScoreHistoryBackendScoreItemDynamoDb(
      this,
      'ScoreHistoryBackendScoreItemDynamoDb',
      SCORE_ITEM_DYNAMODB_TABLE_NAME
    );

    this.scoreItemDynamoDbTableArn = scoreItemDynamoDbTable.tableArn;

    const scoreLargeDataDynamoDbTable =
      new ScoreHistoryBackendScoreLargeDataDynamoDb(
        this,
        'ScoreHistoryBackendScoreLargeDataDynamoDb',
        SCORE_LARGE_DATA_DYNAMODB_TABLE_NAME
      );

    this.scoreLargeDataDynamoDbTableArn = scoreLargeDataDynamoDbTable.tableArn;

    const identity = new OriginAccessIdentity(
      this,
      'ScoreHistoryBackendScoreDataBucketOriginAccessIdentity'
    );

    const scoreHistoryBackendScoreDataBucket =
      new ScoreHistoryBackendScoreDataBucket(
        this,
        'ScoreHistoryBackendScoreDataBucket',
        SCORE_DATA_S3_BUCKET,
        identity
      );

    this.scoreHistoryBackendScoreDataBucketArn =
      scoreHistoryBackendScoreDataBucket.bucketArn;

    const scoreHistoryBackendScoreItemRelationDynamoDb =
      new ScoreHistoryBackendScoreItemRelationDynamoDb(
        this,
        'ScoreHistoryBackendScoreItemRelationDynamoDb',
        SCORE_ITEM_RELATION_DYNAMODB_TABLE_NAME
      );

    this.scoreItemRelationDynamoDbTableArn =
      scoreHistoryBackendScoreItemRelationDynamoDb.tableArn;

    const edgeFunction = new ScoreHistoryBackendPrivateItemLambdaEdgeFunction(
      this,
      'ScoreHistoryBackendPrivateItemLambdaEdgeFunction',
      'ura-kata-backend-private-item-verify-token-filter',
      path.join(__dirname, '../../verify-token-filter/build'),
      'ScoreHistoryBackendPrivateItemLambdaEdgeFunctionStack'
    );

    const privateItemCdnFqdn = `${HISTORY_BACKEND_PRIVATE_ITEM_HOST_NAME}.${DOMAIN_NAME}`;

    const distribution = new ScoreHistoryBackendPrivateItemDistribution(
      this,
      'ScoreHistoryBackendPrivateItemDistribution',
      scoreHistoryBackendScoreDataBucket,
      identity,
      privateItemCdnFqdn,
      URA_KATA_CERTIFICATE_ARN,
      edgeFunction
    );

    const hostZone = HostedZone.fromHostedZoneAttributes(
      this,
      'UraKataPublicHostedZone',
      {
        hostedZoneId: URA_KATA_PUBLIC_HOSTED_ZONE_ID,
        zoneName: DOMAIN_NAME,
      }
    );
    new ScoreHistoryBackendPrivateItemARecord(
      this,
      'ScoreHistoryBackendPrivateItemARecord',
      distribution,
      hostZone,
      privateItemCdnFqdn
    );
  }
}
