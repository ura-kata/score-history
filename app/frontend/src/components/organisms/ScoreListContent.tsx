import { colors, createStyles, makeStyles, Theme } from "@material-ui/core";

export interface ScoreListContentProps {}

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      height: "100%",
      width: "100%",
      display: "flex",
      flexWrap: "wrap",
    },
    item: {
      height: "100px",
      width: "100px",
      backgroundColor: colors.green[200],
    },
  })
);

export default function ScoreListContent(props: ScoreListContentProps) {
  const classes = useStyles();

  return (
    <div className={classes.root}>
      {[...new Array(10)].map((_, index) => (
        <div key={index} className={classes.item}>
          aaaaaa
        </div>
      ))}
    </div>
  );
}
