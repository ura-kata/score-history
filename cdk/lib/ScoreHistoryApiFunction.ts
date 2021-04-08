import { Function, Runtime, Code } from '@aws-cdk/aws-lambda';
import { Construct } from '@aws-cdk/core';
import * as path from 'path';

export class ScoreHistoryApiFunction extends Function {
  constructor(
    scope: Construct,
    id: string,
    functionName: string,
    environment?: { [key: string]: string }
  ) {
    super(scope, id, {
      functionName: functionName,
      environment: environment,
      runtime: Runtime.DOTNET_CORE_3_1,
      handler:
        'PracticeManagerApi::PracticeManagerApi.LambdaEntryPoint::FunctionHandlerAsync',
      code: Code.fromAsset(
        path.join(
          __dirname,
          '../../app/backend/PracticeManagerApi/src/PracticeManagerApi/bin/Release/netcoreapp3.1'
        )
      ),
    });
  }
}
