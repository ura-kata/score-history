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
import React from "react";
import { Link, useHistory } from "react-router-dom";
import RefreshIcon from "@material-ui/icons/Refresh";
import { ScoreSummary, ScoreSummarySet } from "../../ScoreClient";
import { scoreClient } from "../../global";
import { PathCreator } from "../pages/HomePage";

// ------------------------------------------------------------------------------------------
interface ScoreListViewProps {
  scoreSet: ScoreSummarySet;
  onClick?: (owner: string, scoreName: string, score: ScoreSummary) => void;
}

const ScoreListView = (props: ScoreListViewProps) => {
  const _scoreSet = props.scoreSet;
  const _onClick = props.onClick;

  const classes = makeStyles((theme: Theme) =>
    createStyles({
      scoreCard: {
        width: "300px",
        margin: theme.spacing(1),
      },
      scoreCardName: {
        color: colors.grey[400],
      },
      scoreCardContainer: {
        margin: theme.spacing(3, 0, 0),
      },
    })
  )();

  return (
    <Grid container className={classes.scoreCardContainer}>
      {Object.entries(_scoreSet).map(([ownerAndScoreName, score], i) => {
        const os = ownerAndScoreName.split("/");
        const owner = os[0];
        const scoreName = os[1];
        const property = score.property;

        return (
          <Card key={i.toString()} className={classes.scoreCard}>
            <CardActionArea
              onClick={() => {
                if (_onClick) _onClick(owner, scoreName, score);
              }}
            >
              <CardContent>
                <Typography variant="h5">{property?.title}</Typography>
                <Typography variant="caption" className={classes.scoreCardName}>
                  {scoreName}
                </Typography>
                <Typography variant="subtitle1" gutterBottom>
                  {property?.description}
                </Typography>
              </CardContent>
            </CardActionArea>
          </Card>
        );
      })}
    </Grid>
  );
};

// ------------------------------------------------------------------------------------------

export interface ScoreListContentProps {
  scoreSet: ScoreSummarySet;
  pathCreator: PathCreator;
  onLoadedScoreSummarySet?: (scoreSummarySet: ScoreSummarySet) => void;
}

const SocreListContent = (props: ScoreListContentProps) => {
  const _scoreSet = props.scoreSet;
  const _pathCreator = props.pathCreator;
  const onLoadedScoreSummarySet = props.onLoadedScoreSummarySet;

  const history = useHistory();

  const handleScoreOnClick = (
    owner: string,
    scoreName: string,
    socre: ScoreSummary
  ) => {
    history.push(_pathCreator.getDetailPath(owner, scoreName));
  };

  const handleOnRefreshClick = async () => {
    if (onLoadedScoreSummarySet) {
      try {
        const scoreSet = await scoreClient.getScores();
        onLoadedScoreSummarySet(scoreSet);
      } catch (err) {
        console.log(err);
      }
    }
  };

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
        <ScoreListView scoreSet={_scoreSet} onClick={handleScoreOnClick} />
      </Grid>
    </Grid>
  );
};

export default SocreListContent;
