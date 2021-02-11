import { Button, Divider, Grid, Paper, Typography } from "@material-ui/core";
import {
  Timeline,
  TimelineConnector,
  TimelineContent,
  TimelineDot,
  TimelineItem,
  TimelineSeparator,
} from "@material-ui/lab";
import React, { useEffect } from "react";
import { Link } from "react-router-dom";
import { Score } from "../../PracticeManagerApiClient";

interface ScoreDetailContentProps {
  score?: Score;
}

const ScoreDetailContent = (props: ScoreDetailContentProps) => {
  const _socre = props.score;

  useEffect(() => {
    const f = async () => {
      if (!_socre) return;
    };
    f();
  }, [_socre]);

  const timelineItems = !_socre
    ? []
    : [..._socre.versions].reverse().map((version, index) => {
        return (
          <TimelineItem key={index}>
            <TimelineSeparator>
              <TimelineDot>
                {/* Todo チェックしたかどうかをアイコンで表示する */}
              </TimelineDot>
              {index !== _socre.versions.length - 1 ? (
                <TimelineConnector />
              ) : (
                <></>
              )}
            </TimelineSeparator>
            <TimelineContent>
              <Button
                component={Link}
                to={`/home/${_socre?.name}/${version.version}`}
              >
                <Paper elevation={3} style={{ padding: "6px 16px" }}>
                  <Typography>version {version.version}</Typography>
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
          <Typography variant="h4">{_socre?.title}</Typography>
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
              {_socre?.description?.split("\n").map((t, index) => (
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
