import { OriginAccessIdentity } from '@aws-cdk/aws-cloudfront';
import * as cdk from '@aws-cdk/core';
import { ScoreHistoryFrontendBucket } from './ScoreHistoryFrontendBucket';
import { ScoreHistoryFrontendDistribution } from './ScoreHistoryFrontendDistribution';
import { BucketDeployment, Source } from '@aws-cdk/aws-s3-deployment';
import * as path from 'path';
import * as dotenv from 'dotenv';
import { HostedZone } from '@aws-cdk/aws-route53';
import { ScoreHistoryFrontendARecord } from './ScoreHistoryFrontendARecord';

dotenv.config();

const URA_KATA_APP_DOMAIN_NAME = process.env.URA_KATA_APP_DOMAIN_NAME as string;
const URA_KATA_APP_HOST_NAME = process.env.URA_KATA_APP_HOST_NAME as string;
const URA_KATA_CERTIFICATE_ARN = process.env.URA_KATA_CERTIFICATE_ARN as string;
const URA_KATA_PUBLIC_HOSTED_ZONE_ID = process.env
  .URA_KATA_PUBLIC_HOSTED_ZONE_ID as string;

export class ScoreHistoryFrontendStack extends cdk.Stack {
  constructor(scope: cdk.Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const identity = new OriginAccessIdentity(
      this,
      'ScoreHistoryFrontendOriginAccessIdentity'
    );

    const bucket = new ScoreHistoryFrontendBucket(
      this,
      'ScoreHistoryFrontendBucket',
      identity
    );

    const signinFqdn = `${URA_KATA_APP_HOST_NAME}.${URA_KATA_APP_DOMAIN_NAME}`;

    const distribution = new ScoreHistoryFrontendDistribution(
      this,
      'ScoreHistoryFrontendDistribution',
      bucket,
      identity,
      signinFqdn,
      URA_KATA_CERTIFICATE_ARN
    );

    const hostZone = HostedZone.fromHostedZoneAttributes(
      this,
      'UraKataPublicHostedZone',
      {
        hostedZoneId: URA_KATA_PUBLIC_HOSTED_ZONE_ID,
        zoneName: URA_KATA_APP_DOMAIN_NAME,
      }
    );

    new ScoreHistoryFrontendARecord(
      this,
      'ScoreHistoryFrontendARecord',
      distribution,
      hostZone,
      signinFqdn
    );

    new BucketDeployment(this, 'ScoreHistoryFrontendBucketDeployment', {
      sources: [Source.asset(path.join(__dirname, '../../app/frontend/build'))],
      destinationBucket: bucket,
      distribution: distribution,
      distributionPaths: ['/*'],
    });
  }
}
