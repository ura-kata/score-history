import {
  colors,
  createStyles,
  IconButton,
  makeStyles,
  Menu,
  MenuItem,
  Theme,
} from "@material-ui/core";
import React, { useState } from "react";
import AppBarUserInfo from "../../atoms/AppBarUserInfo";
import MoreVertIcon from "@material-ui/icons/MoreVert";
import { accessClient } from "../../../global";
import { AppContext } from "../../../AppContext";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      height: "100%",
      width: "100%",
      backgroundColor: colors.lightGreen[200],
      display: "flex",
    },
    appTitle: {
      height: "100%",
      width: "200px",
      textAlign: "center",
    },
    appButtonGroup: {
      height: "100%",
      width: "calc(100% - 200px)",
      display: "flex",
      justifyContent: "flex-end",
    },
    appButtonGroupItem: {
      height: "100%",
      margin: "0 10px",
    },
    title: {
      margin: 0,
      fontWeight: 900,
      fontSize: "x-large",
    },
    subTitle: {
      margin: 0,
      fontWeight: 600,
      fontSize: "small",
    },
  })
);

//-------------------------------------------------------------------------------

export interface MainAppBarProps {}

export default function MainAppBar(props: MainAppBarProps) {
  const classes = useStyles();

  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const menuOpen = Boolean(anchorEl);

  const appContext = React.useContext(AppContext);

  const _userData = appContext.userData;

  const handleOpenMenuButton = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleCloseMenu = () => {
    setAnchorEl(null);
  };

  const handleSignOut = async () => {
    try {
      await accessClient.signout();
      accessClient.gotoSignInPage("");
    } catch (err) {
      console.log(err);
    }
  };

  return (
    <div className={classes.root}>
      <div className={classes.appTitle}>
        <p className={classes.title}>Ura-Kata</p>
        <p className={classes.subTitle}>楽譜共有管理</p>
      </div>
      <div className={classes.appButtonGroup}>
        <div className={classes.appButtonGroupItem}>
          <AppBarUserInfo userData={_userData} />
        </div>
        <div className={classes.appButtonGroupItem}>
          <IconButton onClick={handleOpenMenuButton}>
            <MoreVertIcon />
          </IconButton>
          <Menu open={menuOpen} onClose={handleCloseMenu} anchorEl={anchorEl}>
            <MenuItem onClick={handleSignOut}>SignOut</MenuItem>
          </Menu>
        </div>
      </div>
    </div>
  );
}
