import {
  Badge,
  Button,
  createStyles,
  IconButton,
  makeStyles,
  Theme
} from "@material-ui/core";
import React from "react";

import MailIcon from '@material-ui/icons/Mail';
import NotificationsIcon from '@material-ui/icons/Notifications';
import MoreIcon from '@material-ui/icons/MoreVert';
import AccountCircle from '@material-ui/icons/AccountCircle';
import { UserMe } from "../../PracticeManagerApiClient";

const useStyles = makeStyles((theme: Theme) =>
createStyles({
  grow: {
    flexGrow: 1,
  },
  sectionDesktop: {
    display: 'none',
    [theme.breakpoints.up('md')]: {
      display: 'flex',
    },
  },
  sectionMobile: {
    display: 'flex',
    [theme.breakpoints.up('md')]: {
      display: 'none',
    },
  },
}));

export interface AppBarIconsProps{
  userMe?: UserMe;
}

const AppBarIcons = (props: AppBarIconsProps)=>{

  const classes = useStyles();

  const menuId = 'primary-search-account-menu';
  const mobileMenuId = 'primary-search-account-menu-mobile';

  const _userMe = props.userMe;

  return (
    <>
      <div className={classes.sectionDesktop}>
        <IconButton aria-label="show 17 new notifications" color="inherit">
          <Badge badgeContent={0} color="secondary">
            <NotificationsIcon />
          </Badge>
        </IconButton>
        <Button
          aria-label="account of current user"
          aria-controls={menuId}
          aria-haspopup="true"
          // onClick={handleProfileMenuOpen}
          color="inherit"
          startIcon={<AccountCircle />}
        >
          {_userMe?.name}
        </Button>
      </div>
      <div className={classes.sectionMobile}>
        <IconButton
          aria-label="show more"
          aria-controls={mobileMenuId}
          aria-haspopup="true"
          // onClick={handleMobileMenuOpen}
          color="inherit"
        >
          <MoreIcon />
        </IconButton>
      </div>
    </>
  )
}

export default AppBarIcons;
