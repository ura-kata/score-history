import { colors, createStyles, makeStyles, Theme } from "@material-ui/core";
import { UserData } from "../../UserClient";

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
  userData?: UserData;
}

export default function AppBarUserInfo(props: AppBarUserInfoProps) {
  const _userData = props.userData;
  var classes = useStyles();
  return (
    <div className={classes.root}>
      <p>{_userData?.email ?? ""}</p>
    </div>
  );
}
