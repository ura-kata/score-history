import { Table, AttributeType, BillingMode } from '@aws-cdk/aws-dynamodb';
import { Construct } from '@aws-cdk/core';

export class ScoreHistoryBackendScoreDynamoDb extends Table {
  constructor(scope: Construct, id: string, tableName: string) {
    super(scope, id, {
      tableName: tableName,
      partitionKey: {
        name: 'owner',
        type: AttributeType.STRING,
      },
      sortKey: {
        name: 'score',
        type: AttributeType.STRING,
      },
      billingMode: BillingMode.PAY_PER_REQUEST,
    });
  }
}
