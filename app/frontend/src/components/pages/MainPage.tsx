import { colors, createStyles, makeStyles, Theme } from "@material-ui/core";
import ScoreListContent from "../organisms/ScoreListContent";
import MainTemplate from "../templates/MainTemplate";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      height: "100%",
      width: "100%",
      display: "flex",
      flexWrap: "wrap",
    },
    navi: {
      height: "100px",
      width: "100%",
    },
    content: {},
  })
);

export interface MainPageProps {}

export default function MainPage(props: MainPageProps) {
  const classes = useStyles();
  return (
    <MainTemplate>
      <div className={classes.root}>
        <ScoreListContent />
      </div>
    </MainTemplate>
  );
}
