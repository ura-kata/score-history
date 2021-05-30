import { OriginAccessIdentity } from '@aws-cdk/aws-cloudfront';
import { Effect, PolicyStatement } from '@aws-cdk/aws-iam';
import { Bucket } from '@aws-cdk/aws-s3';
import { Construct } from '@aws-cdk/core';

export class ScoreHistoryBackendScoreDataBucket extends Bucket {
  constructor(
    scope: Construct,
    id: string,
    bucketName: string,
    identity: OriginAccessIdentity
  ) {
    super(scope, id, {
      bucketName: bucketName,
    });

    const policy = new PolicyStatement({
      actions: ['s3:GetObject'],
      effect: Effect.ALLOW,
      principals: [identity.grantPrincipal],
      resources: [`${this.bucketArn}/*`],
    });

    this.addToResourcePolicy(policy);
  }
}
