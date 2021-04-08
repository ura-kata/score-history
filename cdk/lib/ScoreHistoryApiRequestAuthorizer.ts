import { RequestAuthorizer, IdentitySource } from '@aws-cdk/aws-apigateway';
import { Construct, Duration } from '@aws-cdk/core';
import { ScoreHistoryApiAuthorizerFunction } from './ScoreHistoryApiAuthorizerFunction';

export class ScoreHistoryApiRequestAuthorizer extends RequestAuthorizer {
  constructor(
    scope: Construct,
    id: string,
    authorizerFunction: ScoreHistoryApiAuthorizerFunction,
    authorizerName: string
  ) {
    super(scope, id, {
      handler: authorizerFunction,
      identitySources: [IdentitySource.header('Cookie')],
      authorizerName: authorizerName,
      resultsCacheTtl: Duration.seconds(0),
    });
  }
}
