import { Effect, PolicyStatement } from '@aws-cdk/aws-iam';
import { Function, Runtime, Code } from '@aws-cdk/aws-lambda';
import { Construct, Duration } from '@aws-cdk/core';
import * as path from 'path';

export interface ScoreHistoryApiFunctionProps {
  scoreDynamoDbTableArn: string;
  scoreHistoryBackendScoreDataBucketArn: string;
}

export class ScoreHistoryApiFunction extends Function {
  constructor(
    scope: Construct,
    id: string,
    functionName: string,
    environment: { [key: string]: string },
    props: ScoreHistoryApiFunctionProps
  ) {
    super(scope, id, {
      functionName: functionName,
      environment: environment,
      runtime: Runtime.DOTNET_CORE_3_1,
      handler:
        'ScoreHistoryApi::ScoreHistoryApi.LambdaEntryPoint::FunctionHandlerAsync',
      code: Code.fromAsset(path.join(__dirname, '../../app/backend/build')),
      memorySize: 512,
      timeout: Duration.seconds(60),
    });

    this.addToRolePolicy(
      new PolicyStatement({
        effect: Effect.ALLOW,
        resources: [props.scoreDynamoDbTableArn],
        actions: ['dynamodb:*'],
      })
    );

    this.addToRolePolicy(
      new PolicyStatement({
        effect: Effect.ALLOW,
        resources: [
          props.scoreHistoryBackendScoreDataBucketArn,
          props.scoreHistoryBackendScoreDataBucketArn + '/*',
        ],
        actions: ['s3:*'],
      })
    );
  }
}
