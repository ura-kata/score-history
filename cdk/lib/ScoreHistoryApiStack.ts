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

export class ScoreHistoryApiStack extends cdk.Stack {
  constructor(scope: cdk.Construct, id: string, props?: cdk.StackProps) {
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
        CorsOrigins: URA_KATA_SCORE_HISTORY_API_CORS_ORIGINS,
        CorsHeaders:
          'Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,Cookie',
        CorsMethods: 'GET,POST,DELETE,PATCH,OPTIONS',
        CorsCredentials: 'true',
      }
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

    restApi.root.addMethod('ANY', integration, {
      authorizer: requestAuthorizer,
      authorizationType: AuthorizationType.CUSTOM,
    });

    const proxyResource = restApi.root.addResource('{proxy+}');

    proxyResource.addMethod('ANY', integration, {
      authorizer: requestAuthorizer,
      authorizationType: AuthorizationType.CUSTOM,
    });
  }
}
