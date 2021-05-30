import { ARecord, RecordTarget, IHostedZone } from '@aws-cdk/aws-route53';
import { Construct } from '@aws-cdk/core';
import { CloudFrontTarget } from '@aws-cdk/aws-route53-targets';
import { ScoreHistoryBackendPrivateItemDistribution } from './ScoreHistoryBackendPrivateItemDistribution';

export class ScoreHistoryBackendPrivateItemARecord extends ARecord {
  constructor(
    scope: Construct,
    id: string,
    distribution: ScoreHistoryBackendPrivateItemDistribution,
    hostedZone: IHostedZone,
    recordName: string
  ) {
    super(scope, id, {
      target: RecordTarget.fromAlias(new CloudFrontTarget(distribution)),
      recordName: recordName,
      zone: hostedZone,
    });
  }
}
