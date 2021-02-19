import {
  Button,
  ButtonGroup,
  Card,
  CardActionArea,
  CardContent,
  colors,
  createStyles,
  Divider,
  Grid,
  IconButton,
  makeStyles,
  Theme,
  Typography,
} from "@material-ui/core";
import React, { useEffect, useState } from "react";
import { Link, useHistory } from "react-router-dom";
import RefreshIcon from "@material-ui/icons/Refresh";
import { ScoreSummary, ScoreSummarySet } from "../../ScoreClient";
import { scoreClient } from "../../global";
import { Alert } from "@material-ui/lab";
import PathCreator from "../../PathCreator";
import ScoreListView from "../molecules/ScoreListView";

// ------------------------------------------------------------------------------------------

// ------------------------------------------------------------------------------------------

export interface ScoreListContentProps {
  pathCreator: PathCreator;
}

const SocreListContent = (props: ScoreListContentProps) => {
  const _pathCreator = props.pathCreator;

  const [loadScoreSetError, setLoadScoreSetError] = useState<string>();
  const [scoreSet, setScoreSet] = useState<ScoreSummarySet>({});

  const history = useHistory();

  const loadScoreSet = async () => {
    try {
      const scoreSummarys = await scoreClient.getScores();
      setScoreSet(scoreSummarys);
      setLoadScoreSetError(undefined);
    } catch (err) {
      setLoadScoreSetError(`楽譜の一覧取得に失敗しました`);
      console.log(err);
    }
  };

  const handleScoreOnClick = (
    owner: string,
    scoreName: string,
    socre: ScoreSummary
  ) => {
    history.push(_pathCreator.getDetailPath(owner, scoreName));
  };

  const handleOnRefreshClick = async () => {
    loadScoreSet();
  };

  useEffect(() => {
    loadScoreSet();
  }, []);

  return (
    <Grid container spacing={2}>
      <Grid item xs={12}>
        <Grid container alignItems="center">
          <Grid item xs>
            <Typography variant="h4">スコア一覧</Typography>
          </Grid>
          <Grid item xs>
            <Grid container alignItems="center" justify="flex-end" spacing={1}>
              <Grid item>
                <IconButton onClick={handleOnRefreshClick}>
                  <RefreshIcon />
                </IconButton>
              </Grid>
              <Grid item>
                <ButtonGroup color="primary" style={{ float: "right" }}>
                  <Button component={Link} to="/new">
                    新規
                  </Button>
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
        {loadScoreSetError ? (
          <Alert severity="error">{loadScoreSetError}</Alert>
        ) : (
          <></>
        )}
      </Grid>
      <Grid item xs={12}>
        <ScoreListView scoreSet={scoreSet} onClick={handleScoreOnClick} />
      </Grid>
    </Grid>
  );
};

export default SocreListContent;
