import { Table, AttributeType, BillingMode } from '@aws-cdk/aws-dynamodb';
import { Construct } from '@aws-cdk/core';

export class ScoreHistoryBackendScoreDynamoDb extends Table {
  constructor(scope: Construct, id: string, tableName: string) {
    super(scope, id, {
      tableName: tableName,
      partitionKey: {
        name: 'o',
        type: AttributeType.STRING,
      },
      sortKey: {
        name: 's',
        type: AttributeType.STRING,
      },
      //billingMode: BillingMode.PAY_PER_REQUEST,
      readCapacity: 10,
      writeCapacity: 10,
    });
  }
}
