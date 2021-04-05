import { EdgeFunction } from '@aws-cdk/aws-cloudfront/lib/experimental';
import { Construct } from '@aws-cdk/core';
import { Runtime, Code } from '@aws-cdk/aws-lambda';

export class ScoreHistoryFrontendLambdaEdgeFunction extends EdgeFunction {
  constructor(
    scope: Construct,
    id: string,
    functionName: string,
    srcDirPath: string,
    edgeFunctionStackId: string
  ) {
    super(scope, id, {
      runtime: Runtime.NODEJS_12_X,
      functionName: functionName,
      handler: 'index.handler',
      code: Code.fromAsset(srcDirPath),
      stackId: edgeFunctionStackId,
    });
  }
}
