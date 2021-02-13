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
import { ScoreProperty } from "../../ScoreClient";
import { PathCreator } from "../pages/HomePage";

interface ScoreDetailContentProps {
  owner: string;
  scoreName: string;
  property: ScoreProperty;
  versions?: string[];
  pathCreator: PathCreator;
}

const ScoreDetailContent = (props: ScoreDetailContentProps) => {
  const _owner = props.owner;
  const _scoreName = props.scoreName;
  const _property = props.property;
  const _pathCreator = props.pathCreator;

  const history = useHistory();

  const _versions = props.versions ?? [];

  const versions = _versions.reverse();

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

  const handleOnEditProperty = () => {
    history.push(_pathCreator.getEditPropertyPath(_owner, _scoreName));
  };

  const handleOnInitializePages = () => {
    history.push(_pathCreator.getEditPropertyPath(_owner, _scoreName));
  };

  const InitialVersionButton = () => (
    <Grid container direction="column" alignItems="center">
      <Grid item>
        <Button variant="outlined" onClick={handleOnInitializePages}>
          新しくページを登録する
        </Button>
      </Grid>
    </Grid>
  );

  // TODO 最新のバージョンを表示することにする
  // version が選択されていない場合は最新のバージョンを表示することにする

  return (
    <Grid container spacing={2}>
      <Grid item xs={12}>
        <Grid container alignItems="center">
          <Grid item xs>
            <Typography variant="h4">
              {_property.title ?? _scoreName}
            </Typography>
          </Grid>
          <Grid item xs>
            <Grid container alignItems="center" justify="flex-end" spacing={1}>
              <Grid item>
                <ButtonGroup color="primary">
                  <Button onClick={handleOnEditProperty}>編集</Button>
                </ButtonGroup>
              </Grid>
            </Grid>
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
                {_property.description?.split("\n").map((t, index) => (
                  <Typography key={index}>{t}</Typography>
                ))}
              </Grid>
            </Grid>
          </Grid>
          <Grid item xs={5}>
            {0 < versions.length ? (
              <VersionTimeLine />
            ) : (
              <InitialVersionButton />
            )}
          </Grid>
        </Grid>
      </Grid>
    </Grid>
  );
};

export default ScoreDetailContent;
