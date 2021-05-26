import { colors, createStyles, makeStyles, Theme } from "@material-ui/core";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      minHeight: "100%",
      width: "100%",
    },
    appBar: {
      height: "50px",
      width: "100%",
      backgroundColor: colors.grey[500],
      position: "sticky",
      top: 0,
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
    content: {
      minHeight: "calc(100% - 50px)",
      width: "100%",
      backgroundColor: colors.red[200],
    },
  })
);

export interface MainTemplateProps {
  children: React.ReactNode;
}

export default function MainTemplate(props: MainTemplateProps) {
  var _children = props.children;

  var classes = useStyles();

  return (
    <div className={classes.root}>
      <div className={classes.appBar}>
        <div className={classes.appTitle}>
          <h1>タイトル</h1>
        </div>
        <div className={classes.appButtonGroup}></div>
      </div>
      <div className={classes.content}>{_children}</div>
    </div>
  );
}
