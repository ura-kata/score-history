import { colors, createStyles, makeStyles, Theme } from "@material-ui/core";
import { BrowserRouter as Router, Route, Switch } from "react-router-dom";
import ScoreDetail from "../organisms/ScoreDetailContent";
import ScoreListContent from "../organisms/ScoreListContent";
import ScoreNew from "../organisms/ScoreNew";
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
        <Router>
          <Switch>
            <Route path="/scores/new" component={ScoreNew} />
            <Route path="/scores/:scoreId" component={ScoreDetail} />
            <Route path="/scores" component={ScoreListContent} />
            <Route path="/" component={ScoreListContent} />
          </Switch>
        </Router>
      </div>
    </MainTemplate>
  );
}
