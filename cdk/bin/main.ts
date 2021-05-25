#!/usr/bin/env node
import 'source-map-support/register';
import * as cdk from '@aws-cdk/core';
import { ScoreHistoryFrontendStack } from '../lib/ScoreHistoryFrontendStack';
import { ScoreHistoryApiStack } from '../lib/ScoreHistoryApiStack';
import { ScoreHistoryBackendStack } from '../lib/ScoreHistoryBackendStack';

const app = new cdk.App();
new ScoreHistoryFrontendStack(app, 'ScoreHistoryFrontendStack', {
  stackName: 'ura-kata-score-history-frontend-stack',
  env: {
    region: 'ap-northeast-1',
  },
});

new ScoreHistoryApiStack(app, 'ScoreHistoryApiStack', {
  stackName: 'ura-kata-score-history-api-stack',
  env: {
    region: 'ap-northeast-1',
  },
});

new ScoreHistoryBackendStack(app, 'ScoreHistoryBackendStack', {
  stackName: 'ura-kata-score-history-backend-stack',
  env: {
    region: 'ap-northeast-1',
  },
});
