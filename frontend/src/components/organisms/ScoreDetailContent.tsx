import {
  Button,
  ButtonGroup,
  Divider,
  Grid,
  Paper,
  Typography,
} from "@material-ui/core";
import {
  Timeline,
  TimelineConnector,
  TimelineContent,
  TimelineDot,
  TimelineItem,
  TimelineSeparator,
} from "@material-ui/lab";
import React from "react";
import { Link, useHistory } from "react-router-dom";
import {
  ScoreV2Latest,
  ScoreV2VersionSet,
} from "../../PracticeManagerApiClient";
import { PathCreator } from "../pages/HomePage";

interface ScoreDetailContentProps {
  owner: string;
  scoreName: string;
  score?: ScoreV2Latest;
  versionSet?: ScoreV2VersionSet;
  pathCreator: PathCreator;
}

const ScoreDetailContent = (props: ScoreDetailContentProps) => {
  const _owner = props.owner;
  const _scoreName = props.scoreName;
  const _score = props.score;
  const _pathCreator = props.pathCreator;

  const history = useHistory();

  const property = _score?.head.property ?? {
    title: "",
    description: "",
  };
  const _versionSet = props.versionSet ?? {};

  const versions = Object.entries(_versionSet).map(([key, value]) => key);

  const VersionTimeLine = () => (
    <Grid
      container
      direction="column"
      alignItems="center"
      justify="center"
      spacing={3}
    >
      <Grid item xs={12}>
        <Typography variant="h5">バージョン</Typography>
      </Grid>
      <Grid item xs={12}>
        {/* Todo バージョンは長くなることが良そうされるのでスクロールできるようにする */}
        <Timeline align="left">
          {versions.reverse().map((version, index) => {
            return (
              <TimelineItem key={index}>
                <TimelineSeparator>
                  <TimelineDot>
                    {/* Todo チェックしたかどうかをアイコンで表示する */}
                  </TimelineDot>
                  {index !== versions.length - 1 ? (
                    <TimelineConnector />
                  ) : (
                    <></>
                  )}
                </TimelineSeparator>
                <TimelineContent>
                  <Button
                    component={Link}
                    to={_pathCreator.getVersionPath(
                      _owner,
                      _scoreName,
                      version
                    )}
                  >
                    <Paper elevation={3} style={{ padding: "6px 16px" }}>
                      <Typography>version {version}</Typography>
                    </Paper>
                  </Button>
                </TimelineContent>
              </TimelineItem>
            );
          })}
        </Timeline>
      </Grid>
    </Grid>
  );

  const handleOnScoreUpdate = () => {
    history.push(_pathCreator.getUpdatePath(_owner, _scoreName));
  };

  return (
    <Grid container spacing={2}>
      <Grid item xs={12}>
        <Grid container alignItems="center">
          <Grid item xs>
            <Typography variant="h4">{property.title ?? _scoreName}</Typography>
          </Grid>
          <Grid item>
            <ButtonGroup>
              <Button onClick={handleOnScoreUpdate}>楽譜更新</Button>
            </ButtonGroup>
          </Grid>
        </Grid>
      </Grid>
      <Grid item xs={12}>
        <Divider />
      </Grid>
      <Grid item xs={12}>
        <Grid container spacing={3}>
          <Grid item xs>
            <Grid container spacing={3}>
              <Grid item xs={12}>
                <Typography variant="h5">説明</Typography>
              </Grid>
              <Grid item xs={12}>
                {property.description?.split("\n").map((t, index) => (
                  <Typography key={index}>{t}</Typography>
                ))}
              </Grid>
            </Grid>
          </Grid>
          <Grid item xs={5}>
            <VersionTimeLine />
          </Grid>
        </Grid>
      </Grid>
    </Grid>
  );
};

export default ScoreDetailContent;
