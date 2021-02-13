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
import { Link } from "react-router-dom";
import RefreshIcon from "@material-ui/icons/Refresh";
import { ScoreSummary, ScoreSummarySet } from "../../ScoreClient";

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
  onCardClick?: (owner: string, scoreName: string, score: ScoreSummary) => void;
  onRefreshClick?: () => void;
}

const SocreListContent = (props: ScoreListContentProps) => {
  const _scoreSet = props.scoreSet;
  const _onCardClick = props.onCardClick;
  const _onRefreshClick = props.onRefreshClick;

  const handleScoreOnClick = (
    owner: string,
    scoreName: string,
    socre: ScoreSummary
  ) => {
    if (_onCardClick) _onCardClick(owner, scoreName, socre);
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
                <IconButton onClick={_onRefreshClick}>
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
