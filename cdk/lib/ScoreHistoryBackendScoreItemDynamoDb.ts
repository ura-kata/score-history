import { Table, AttributeType, BillingMode } from '@aws-cdk/aws-dynamodb';
import { Construct } from '@aws-cdk/core';

export class ScoreHistoryBackendScoreItemDynamoDb extends Table {
  constructor(scope: Construct, id: string, tableName: string) {
    super(scope, id, {
      tableName: tableName,
      partitionKey: {
        name: 'owner',
        type: AttributeType.STRING,
      },
      sortKey: {
        name: 'item',
        type: AttributeType.STRING,
      },
      billingMode: BillingMode.PAY_PER_REQUEST,
    });
  }
}
