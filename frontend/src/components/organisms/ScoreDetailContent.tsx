import { Button, Divider, Grid, Paper, Typography } from "@material-ui/core";
import {
  Timeline,
  TimelineConnector,
  TimelineContent,
  TimelineDot,
  TimelineItem,
  TimelineSeparator,
} from "@material-ui/lab";
import React from "react";
import { Link } from "react-router-dom";
import {
  ScoreV2Latest,
  ScoreV2VersionSet,
} from "../../PracticeManagerApiClient";

interface ScoreDetailContentProps {
  owner?: string;
  scoreName?: string;
  score?: ScoreV2Latest;
  versionSet?: ScoreV2VersionSet;
}

const ScoreDetailContent = (props: ScoreDetailContentProps) => {
  const _owner = props.owner;
  const _scoreName = props.scoreName;
  const _score = props.score;

  const property = _score?.head.property;
  const _versionSet = props.versionSet ? props.versionSet : {};

  const versions = Object.entries(_versionSet).map(([key, value]) => key);
  const timelineItems = !_score
    ? []
    : [...versions].reverse().map((version, index) => {
        return (
          <TimelineItem key={index}>
            <TimelineSeparator>
              <TimelineDot>
                {/* Todo チェックしたかどうかをアイコンで表示する */}
              </TimelineDot>
              {index !== versions.length - 1 ? <TimelineConnector /> : <></>}
            </TimelineSeparator>
            <TimelineContent>
              <Button
                component={Link}
                to={`/home/${_owner}/${_scoreName}/version/${version}`}
              >
                <Paper elevation={3} style={{ padding: "6px 16px" }}>
                  <Typography>version {version}</Typography>
                </Paper>
              </Button>
            </TimelineContent>
          </TimelineItem>
        );
      });

  return (
    <>
      <Grid container>
        <Grid item xs>
          <Typography variant="h4">{property?.title}</Typography>
        </Grid>
      </Grid>

      <Divider />

      <Grid container spacing={3}>
        <Grid item xs>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Typography variant="h5">説明</Typography>
            </Grid>
            <Grid item xs={12}>
              {property?.description?.split("\n").map((t, index) => (
                <Typography key={index}>{t}</Typography>
              ))}
            </Grid>
          </Grid>
        </Grid>
        <Grid item xs={5}>
          <Grid container justify="center" spacing={3}>
            <Grid item xs={12}>
              <Typography variant="h5" style={{ textAlign: "center" }}>
                バージョン
              </Typography>
            </Grid>
            <Grid item xs={12}>
              {/* Todo バージョンは長くなることが良そうされるのでスクロールできるようにする */}
              <Timeline align="left">{timelineItems}</Timeline>
            </Grid>
          </Grid>
        </Grid>
      </Grid>
    </>
  );
};

export default ScoreDetailContent;
