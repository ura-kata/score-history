import { colors, createStyles, makeStyles, Theme } from "@material-ui/core";
import MainTemplate from "../templates/MainTemplate";

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

export interface MainPageProps {}

export default function MainPage(props: MainPageProps) {
  const classes = useStyles();
  return (
    <MainTemplate>
      <div className={classes.root}>
        {[...new Array(100)].map((_, index) => (
          <div key={index} className={classes.item}>
            aaaaaa
          </div>
        ))}
      </div>
    </MainTemplate>
  );
}
