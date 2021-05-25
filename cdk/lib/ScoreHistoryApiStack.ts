import * as cdk from '@aws-cdk/core';
import * as dotenv from 'dotenv';
import { HostedZone } from '@aws-cdk/aws-route53';
import { Certificate } from '@aws-cdk/aws-certificatemanager';
import { AuthorizationType } from '@aws-cdk/aws-apigateway';
import { ScoreHistoryApiCustomDomainName } from './ScoreHistoryApiCustomDomainName';
import { ScoreHistoryApiARecord } from './ScoreHistoryApiARecord';
import { ScoreHistoryApiFunction } from './ScoreHistoryApiFunction';
import { ScoreHistoryApiRestApi } from './ScoreHistoryApiRestApi';
import { LambdaIntegration } from '@aws-cdk/aws-apigateway';
import { ScoreHistoryApiAuthorizerFunction } from './ScoreHistoryApiAuthorizerFunction';
import { ScoreHistoryApiRequestAuthorizer } from './ScoreHistoryApiRequestAuthorizer';

dotenv.config();

const URA_KATA_APP_DOMAIN_NAME = process.env.URA_KATA_APP_DOMAIN_NAME as string;
const URA_KATA_SCORE_HISTORY_API_CERTIFICATE_ARN = process.env
  .URA_KATA_SCORE_HISTORY_API_CERTIFICATE_ARN as string;
const URA_KATA_PUBLIC_HOSTED_ZONE_ID = process.env
  .URA_KATA_PUBLIC_HOSTED_ZONE_ID as string;

const URA_KATA_SCORE_HISTORY_API_HOST_NAME = process.env
  .URA_KATA_SCORE_HISTORY_API_HOST_NAME as string;
const URA_KATA_SCORE_HISTORY_API_STAGE_NAME = process.env
  .URA_KATA_SCORE_HISTORY_API_STAGE_NAME as string;
const URA_KATA_SCORE_HISTORY_API_CORS_ORIGINS = process.env
  .URA_KATA_SCORE_HISTORY_API_CORS_ORIGINS as string;

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

if (!SCORE_DYNAMODB_TABLE_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_ITEM_DYNAMODB_TABLE_NAME' is not found."
  );
}
/** 楽譜データの大きいデータを格納する DynamoDB のテーブル */
const SCORE_LARGE_DATA_DYNAMODB_TABLE_NAME = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_SCORE_LARGE_DATA_DYNAMODB_TABLE_NAME as string;

if (!SCORE_DYNAMODB_TABLE_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_LARGE_DATA_DYNAMODB_TABLE_NAME' is not found."
  );
}
/** 楽譜のアイテムデータを格納する S3 バケット */
const SCORE_ITEM_S3_BUCKET = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_SCORE_ITEM_S3_BUCKET as string;

if (!SCORE_DYNAMODB_TABLE_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_ITEM_S3_BUCKET' is not found."
  );
}
/** 楽譜のスナップショットデータを格納する S3 バケット */
const SCORE_SNAPSHOT_S3_BUCKET = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_SCORE_SNAPSHOT_S3_BUCKET as string;

if (!SCORE_DYNAMODB_TABLE_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_SNAPSHOT_S3_BUCKET' is not found."
  );
}
/** DynamoDB のリージョン */
const SCORE_DYNAMODB_REGION_SYSTEM_NAME = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_SCORE_DYNAMODB_REGION_SYSTEM_NAME as string;

if (!SCORE_DYNAMODB_TABLE_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_DYNAMODB_REGION_SYSTEM_NAME' is not found."
  );
}
/** S3 のリージョン */
const SCORE_S3_REGION_SYSTEM_NAME = process.env
  .URA_KATA_SCORE_HISTORY_BACKEND_SCORE_S3_REGION_SYSTEM_NAME as string;

if (!SCORE_DYNAMODB_TABLE_NAME) {
  throw new Error(
    "'URA_KATA_SCORE_HISTORY_BACKEND_SCORE_S3_REGION_SYSTEM_NAME' is not found."
  );
}

export interface ScoreHistoryApiStackProps {
  scoreDynamoDbTableArn: string;
  scoreItemDynamoDbTableArn: string;
  scoreLargeDataDynamoDbTableArn: string;
  scoreHistoryBackendScoreDataBucketArn: string;
  scoreHistoryBackendScoreDataSnapshotBucketArn: string;
}

export class ScoreHistoryApiStack extends cdk.Stack {
  constructor(
    scope: cdk.Construct,
    id: string,
    props: cdk.StackProps,
    stackProps: ScoreHistoryApiStackProps
  ) {
    super(scope, id, props);

    const zoneName = URA_KATA_APP_DOMAIN_NAME;
    const publicHostedZoneId = URA_KATA_PUBLIC_HOSTED_ZONE_ID;

    const hostZone = HostedZone.fromHostedZoneAttributes(
      this,
      'UraKataPublicHostedZone',
      {
        hostedZoneId: publicHostedZoneId,
        zoneName: zoneName,
      }
    );

    const certificateArn = URA_KATA_SCORE_HISTORY_API_CERTIFICATE_ARN;
    const certificate = Certificate.fromCertificateArn(
      this,
      'UraKataCertificate',
      certificateArn
    );

    const scoreHistoryApiFqdn = `${URA_KATA_SCORE_HISTORY_API_HOST_NAME}.${URA_KATA_APP_DOMAIN_NAME}`;
    const customDomainName = new ScoreHistoryApiCustomDomainName(
      this,
      'ScoreHistoryApiCustomDomainName',
      scoreHistoryApiFqdn,
      certificate
    );

    new ScoreHistoryApiARecord(
      this,
      'ScoreHistoryApiARecord',
      customDomainName,
      hostZone,
      scoreHistoryApiFqdn
    );

    const lambdaFunction = new ScoreHistoryApiFunction(
      this,
      'ScoreHistoryApiFunction',
      'ura-kata-score-history-api',
      {
        URA_KATA_CorsOrigins: URA_KATA_SCORE_HISTORY_API_CORS_ORIGINS,
        URA_KATA_CorsHeaders:
          'Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,Cookie',
        URA_KATA_CorsMethods: 'GET,POST,DELETE,PATCH,OPTIONS',
        URA_KATA_CorsCredentials: 'true',
        URA_KATA_ApiVersion: '1.0.0',
        URA_KATA_ScoreDynamoDbTableName: SCORE_DYNAMODB_TABLE_NAME,
        URA_KATA_ScoreItemDynamoDbTableName: SCORE_ITEM_DYNAMODB_TABLE_NAME,
        URA_KATA_ScoreLargeDataDynamoDbTableName:
          SCORE_LARGE_DATA_DYNAMODB_TABLE_NAME,
        URA_KATA_ScoreItemS3Bucket: SCORE_ITEM_S3_BUCKET,
        URA_KATA_ScoreDataSnapshotS3Bucket: SCORE_SNAPSHOT_S3_BUCKET,
        URA_KATA_ScoreDynamoDbRegionSystemName:
          SCORE_DYNAMODB_REGION_SYSTEM_NAME,
        URA_KATA_ScoreS3RegionSystemName: SCORE_S3_REGION_SYSTEM_NAME,
      },
      stackProps
    );

    const apiStageName = URA_KATA_SCORE_HISTORY_API_STAGE_NAME;
    const restApi = new ScoreHistoryApiRestApi(
      this,
      'ScoreHistoryApiRestApi',
      'ura-kata-score-history-api',
      apiStageName
    );

    customDomainName.addBasePathMapping(restApi, {
      basePath: apiStageName,
    });

    const authorizerFunction = new ScoreHistoryApiAuthorizerFunction(
      this,
      'ScoreHistoryApiAuthorizerFunction',
      'ura-kata-socre-history-api-authorizer'
    );

    const requestAuthorizer = new ScoreHistoryApiRequestAuthorizer(
      this,
      'ScoreHistoryApiRequestAuthorizer',
      authorizerFunction,
      'score-history-api-authorizer'
    );

    const integration = new LambdaIntegration(lambdaFunction);

    const authorizationMethods = ['GET', 'POST', 'DELETE', 'PATCH'];
    restApi.root.addMethod('OPTIONS', integration);
    authorizationMethods.forEach((method) => {
      restApi.root.addMethod(method, integration, {
        authorizer: requestAuthorizer,
        authorizationType: AuthorizationType.CUSTOM,
      });
    });

    const proxyResource = restApi.root.addResource('{proxy+}');
    proxyResource.addMethod('OPTIONS', integration);
    authorizationMethods.forEach((method) => {
      proxyResource.addMethod(method, integration, {
        authorizer: requestAuthorizer,
        authorizationType: AuthorizationType.CUSTOM,
      });
    });
  }
}
