import {
  Card,
  CardActionArea,
  CardContent,
  colors,
  createStyles,
  makeStyles,
  Theme,
  Typography,
} from "@material-ui/core";
import { useHistory } from "react-router";
import { ScoreSummary } from "../../../ScoreClientV2";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      height: "100%",
      width: "100%",
    },
    scoreCard: {
      width: "100%",
      height: "100%",
    },
  })
);

export interface ScoreSummaryCardProps {
  scoreSummary: ScoreSummary;
}

/** 楽譜のサマリーデータを表示するカードコンポーネント */
export default function ScoreSummaryCard(props: ScoreSummaryCardProps) {
  const _scoreSummary = props.scoreSummary;

  const classes = useStyles();
  const history = useHistory();
  return (
    <div>
      <Card className={classes.scoreCard}>
        <CardActionArea
          onClick={() => {
            history.push(`scores/${_scoreSummary.id}`);
          }}
        >
          <CardContent>
            <Typography variant="h5">{_scoreSummary?.title}</Typography>
            <Typography variant="subtitle1" gutterBottom>
              {_scoreSummary?.description}
            </Typography>
          </CardContent>
        </CardActionArea>
      </Card>
    </div>
  );
}
