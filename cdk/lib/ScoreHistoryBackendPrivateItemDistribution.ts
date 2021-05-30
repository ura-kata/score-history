import {
  CloudFrontWebDistribution,
  OriginAccessIdentity,
  PriceClass,
  SSLMethod,
  SecurityPolicyProtocol,
  LambdaEdgeEventType,
} from '@aws-cdk/aws-cloudfront';
import { Construct } from '@aws-cdk/core';
import { ScoreHistoryBackendPrivateItemLambdaEdgeFunction } from './ScoreHistoryBackendPrivateItemLambdaEdgeFunction';
import { ScoreHistoryBackendScoreDataBucket } from './ScoreHistoryBackendScoreDataBucket';

export class ScoreHistoryBackendPrivateItemDistribution extends CloudFrontWebDistribution {
  constructor(
    scope: Construct,
    id: string,
    itemBucket: ScoreHistoryBackendScoreDataBucket,
    identity: OriginAccessIdentity,
    fqdn: string,
    acmCertificateArn: string,
    edgeFunction: ScoreHistoryBackendPrivateItemLambdaEdgeFunction
  ) {
    super(scope, id, {
      originConfigs: [
        {
          s3OriginSource: {
            s3BucketSource: itemBucket,
            originAccessIdentity: identity,
          },
          behaviors: [
            {
              isDefaultBehavior: true,
              lambdaFunctionAssociations: [
                {
                  lambdaFunction: edgeFunction.currentVersion,
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
