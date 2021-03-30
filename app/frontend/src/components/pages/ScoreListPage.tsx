import {
  Button,
  ButtonGroup,
  Divider,
  Grid,
  IconButton,
  Typography,
} from "@material-ui/core";
import { Alert } from "@material-ui/lab";
import React, { useEffect, useState } from "react";
import { Link, useHistory } from "react-router-dom";
import { scoreClient } from "../../global";
import PathCreator from "../../PathCreator";
import { ScoreSummary, ScoreSummarySet } from "../../ScoreClient";
import HomeTemplate from "../templates/HomeTemplate";
import RefreshIcon from "@material-ui/icons/Refresh";
import ScoreListView from "../molecules/ScoreListView";

export default function ScoreListPage() {
  const pathCreator = new PathCreator();

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
    history.push(pathCreator.getDetailPath(owner, scoreName));
  };

  const handleOnRefreshClick = async () => {
    loadScoreSet();
  };

  useEffect(() => {
    loadScoreSet();
  }, []);

  return (
    <HomeTemplate>
      <Grid container spacing={2}>
        <Grid item xs={12}>
          <Grid container alignItems="center">
            <Grid item xs>
              <Typography variant="h4">スコア一覧</Typography>
            </Grid>
            <Grid item xs>
              <Grid
                container
                alignItems="center"
                justify="flex-end"
                spacing={1}
              >
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
    </HomeTemplate>
  );
}
