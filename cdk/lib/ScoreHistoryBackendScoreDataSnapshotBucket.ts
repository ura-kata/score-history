import { ArnPrincipal, Effect, PolicyStatement } from '@aws-cdk/aws-iam';
import { Bucket } from '@aws-cdk/aws-s3';
import { Construct } from '@aws-cdk/core';

export class ScoreHistoryBackendScoreDataSnapshotBucket extends Bucket {
  constructor(scope: Construct, id: string, bucketName: string) {
    super(scope, id, {
      bucketName: bucketName,
    });

    const policy = new PolicyStatement({
      actions: ['s3:GetObject'],
      effect: Effect.ALLOW,
      principals: [new ArnPrincipal('*')],
      resources: [`${this.bucketArn}/*`],
    });

    this.addToResourcePolicy(policy);
  }
}
