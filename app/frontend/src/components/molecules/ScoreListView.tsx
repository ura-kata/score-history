import {
  Card,
  CardActionArea,
  CardContent,
  colors,
  createStyles,
  Grid,
  makeStyles,
  Theme,
  Typography,
} from "@material-ui/core";
import React from "react";
import { ScoreSummary, ScoreSummarySet } from "../../ScoreClient";

const useStyles = makeStyles((theme: Theme) =>
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
);

export interface ScoreListViewProps {
  scoreSet: ScoreSummarySet;
  onClick?: (owner: string, scoreName: string, score: ScoreSummary) => void;
}

export default function ScoreListView(props: ScoreListViewProps) {
  const _scoreSet = props.scoreSet;
  const _onClick = props.onClick;

  const classes = useStyles();

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
}
