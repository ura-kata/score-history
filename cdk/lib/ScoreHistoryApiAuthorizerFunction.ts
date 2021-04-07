import { Function, Code } from '@aws-cdk/aws-lambda';
import { Construct } from '@aws-cdk/core';
import { Runtime } from '@aws-cdk/aws-lambda';
import * as path from 'path';

export class ScoreHistoryApiAuthorizerFunction extends Function {
  constructor(
    scope: Construct,
    id: string,
    functionName: string,
    environment?: { [key: string]: string }
  ) {
    super(scope, id, {
      functionName: functionName,
      runtime: Runtime.NODEJS_14_X,
      code: Code.fromAsset(
        path.join(__dirname, '../../api-verify-token-filter/build')
      ),
      handler: 'index.handler',
      environment: environment,
    });
  }
}
