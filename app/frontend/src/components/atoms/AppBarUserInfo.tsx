import { colors, createStyles, makeStyles, Theme } from "@material-ui/core";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      height: "100%",
      width: "100%",
      display: "flex",
      justifyContent: "center",
      alignItems: "center",
    },
    username: {
      textAlign: "center",
    },
  })
);

export interface AppBarUserInfoProps {
  username?: string;
}

export default function AppBarUserInfo(props: AppBarUserInfoProps) {
  const _username = props.username;
  var classes = useStyles();
  return (
    <div className={classes.root}>
      <p>{_username ?? ""}</p>
    </div>
  );
}
