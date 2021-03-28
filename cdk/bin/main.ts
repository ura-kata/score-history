#!/usr/bin/env node
import 'source-map-support/register';
import * as cdk from '@aws-cdk/core';
import { ScoreHistoryFrontendStack } from '../lib/ScoreHistoryFrontendStack';

const app = new cdk.App();
new ScoreHistoryFrontendStack(app, 'ScoreHistoryFrontendStack', {
  stackName: 'ura-kata-score-history-frontend-stack',
  env: {
    region: 'ap-northeast-1',
  },
});
