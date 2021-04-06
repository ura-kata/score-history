import {
  CloudFrontWebDistribution,
  OriginAccessIdentity,
  PriceClass,
  SSLMethod,
  SecurityPolicyProtocol,
  LambdaEdgeEventType,
} from '@aws-cdk/aws-cloudfront';
import { Construct } from '@aws-cdk/core';
import { ScoreHistoryFrontendBucket } from './ScoreHistoryFrontendBucket';
import { ScoreHistoryFrontendLambdaEdgeFunction } from './ScoreHistoryFrontendLambdaEdgeFunction';

export class ScoreHistoryFrontendDistribution extends CloudFrontWebDistribution {
  constructor(
    scope: Construct,
    id: string,
    appBucket: ScoreHistoryFrontendBucket,
    identity: OriginAccessIdentity,
    fqdn: string,
    acmCertificateArn: string,
    scoreHistoryFrontendLambdaEdgeFunction: ScoreHistoryFrontendLambdaEdgeFunction
  ) {
    super(scope, id, {
      errorConfigurations: [
        {
          errorCachingMinTtl: 300,
          errorCode: 403,
          responseCode: 200,
          responsePagePath: '/index.html',
        },
        {
          errorCachingMinTtl: 300,
          errorCode: 404,
          responseCode: 200,
          responsePagePath: '/index.html',
        },
      ],
      originConfigs: [
        {
          s3OriginSource: {
            s3BucketSource: appBucket,
            originAccessIdentity: identity,
          },
          behaviors: [
            {
              isDefaultBehavior: true,
              lambdaFunctionAssociations: [
                {
                  lambdaFunction:
                    scoreHistoryFrontendLambdaEdgeFunction.currentVersion,
                  eventType: LambdaEdgeEventType.VIEWER_REQUEST,
                  includeBody: false,
                },
              ],
            },
          ],
        },
      ],
      priceClass: PriceClass.PRICE_CLASS_200,
      viewerCertificate: {
        aliases: [fqdn],
        props: {
          acmCertificateArn: acmCertificateArn,
          minimumProtocolVersion: SecurityPolicyProtocol.TLS_V1_2_2019,
          sslSupportMethod: SSLMethod.SNI,
        },
      },
    });
  }
}
