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
  makeStyles,
  Theme,
  Typography,
} from "@material-ui/core";
import React from "react";
import { Link, useHistory } from "react-router-dom";
import { Score } from "../../PracticeManagerApiClient";

// ------------------------------------------------------------------------------------------
interface ScoreListViewProps {
  scores: { [name: string]: Score };
  onClick?: (key: string, score: Score) => void;
}

const ScoreListView = (props: ScoreListViewProps) => {
  const _scores = props.scores;
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
      {Object.entries(_scores).map(([key, score], i) => (
        <Card key={i.toString()} className={classes.scoreCard}>
          <CardActionArea
            onClick={() => {
              if (_onClick) _onClick(key, score);
            }}
          >
            <CardContent>
              <Typography variant="h5">{score.title}</Typography>
              <Typography variant="caption" className={classes.scoreCardName}>
                {score.name}
              </Typography>
              <Typography variant="subtitle1" gutterBottom>
                {score.description}
              </Typography>
            </CardContent>
          </CardActionArea>
        </Card>
      ))}
    </Grid>
  );
};

// ------------------------------------------------------------------------------------------

export interface ScoreListContentProps {
  scores: { [name: string]: Score };
}

const SocreListContent = (props: ScoreListContentProps) => {
  const _scores = props.scores;
  const history = useHistory();

  const handleScoreOnClick = (key: string, socre: Score) => {
    history.push(`/home/${key}/`);
  };

  return (
    <>
      <Grid container>
        <Grid item xs>
          <Typography variant="h4">スコア一覧</Typography>
        </Grid>
        <Grid item xs>
          <ButtonGroup color="primary" style={{ float: "right" }}>
            <Button component={Link} to="/new">
              新規
            </Button>
          </ButtonGroup>
        </Grid>
      </Grid>

      <Divider />

      <ScoreListView scores={_scores} onClick={handleScoreOnClick} />
    </>
  );
};

export default SocreListContent;
