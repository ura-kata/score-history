import { colors, createStyles, makeStyles, Theme } from "@material-ui/core";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      height: "100%",
      width: "100%",
      backgroundColor: colors.grey[500],
      display: "flex",
    },
    appTitle: {
      height: "100%",
      width: "200px",
      backgroundColor: colors.blue[200],
    },
    appButtonGroup: {
      height: "100%",
      width: "calc(100% - 200px)",
      backgroundColor: colors.amber[200],
    },
  })
);

export interface MainAppBarProps {}

export default function MainAppBar(props: MainAppBarProps) {
  const classes = useStyles();
  return (
    <div className={classes.root}>
      <div className={classes.appTitle}>
        <h1>タイトル</h1>
      </div>
      <div className={classes.appButtonGroup}></div>
    </div>
  );
}
