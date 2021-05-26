import { Bucket } from '@aws-cdk/aws-s3';
import { Construct } from '@aws-cdk/core';

export class ScoreHistoryBackendScoreDataSnapshotBucket extends Bucket {
  constructor(scope: Construct, id: string, bucketName: string) {
    super(scope, id, {
      bucketName: bucketName,
    });
  }
}
