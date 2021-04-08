import { ARecord, RecordTarget, IHostedZone } from '@aws-cdk/aws-route53';
import { ApiGatewayDomain } from '@aws-cdk/aws-route53-targets';
import { Construct } from '@aws-cdk/core';
import { ScoreHistoryApiCustomDomainName } from './ScoreHistoryApiCustomDomainName';

export class ScoreHistoryApiARecord extends ARecord {
  constructor(
    scope: Construct,
    id: string,
    customDomainName: ScoreHistoryApiCustomDomainName,
    hostedZone: IHostedZone,
    recordName: string
  ) {
    super(scope, id, {
      target: RecordTarget.fromAlias(new ApiGatewayDomain(customDomainName)),
      recordName: recordName,
      zone: hostedZone,
    });
  }
}
