import { Table, AttributeType, BillingMode } from '@aws-cdk/aws-dynamodb';
import { Construct } from '@aws-cdk/core';

export class ScoreHistoryBackendScoreItemRelationDynamoDb extends Table {
  constructor(scope: Construct, id: string, tableName: string) {
    super(scope, id, {
      tableName: tableName,
      partitionKey: {
        name: 'o',
        type: AttributeType.STRING,
      },
      sortKey: {
        name: 'i',
        type: AttributeType.STRING,
      },
      billingMode: BillingMode.PAY_PER_REQUEST,
    });
  }
}
