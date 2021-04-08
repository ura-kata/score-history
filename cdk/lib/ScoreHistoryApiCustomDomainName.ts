import {
  DomainName,
  EndpointType,
  SecurityPolicy,
} from '@aws-cdk/aws-apigateway';
import { Construct } from '@aws-cdk/core';
import { ICertificate } from '@aws-cdk/aws-certificatemanager';

export class ScoreHistoryApiCustomDomainName extends DomainName {
  constructor(
    scope: Construct,
    id: string,
    domainName: string,
    certificate: ICertificate
  ) {
    super(scope, id, {
      domainName: domainName,
      certificate: certificate,
      securityPolicy: SecurityPolicy.TLS_1_2,
      endpointType: EndpointType.REGIONAL,
    });
  }
}
